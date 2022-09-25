using System.Linq.Expressions;
using System.Threading;

namespace Benutomo.DataBindings
{
    public sealed partial class Binding<SourceT, DestinationT> : IBinding
    {
        private const int INVOKE_STATE_INIT = 0;
        private const int INVOKE_STATE_BEGIN_SET_VALUE = 1;
        private const int INVOKE_STATE_END_SET_VALUE = 2;
        private const int INVOKE_STATE_FINISHED = 3;

        public event Action<Exception>? ForwardSyncError;

        public event Action<Exception>? BackwardSyncError;

        public event Action<IBinding>? Disposing;

        public event Action? Disposed;

        [EnableNotificationSupport]
        public BindingDirection BindingDirection
        {
            get => _BindingDirection();
            set
            {
                if (_BindingDirection(value))
                {
                    SyncValueCore(ignoreIfSameObject: true);
                }
            }
        }

        [EnableNotificationSupport]
        public Func<SourceT?, DestinationT> ForwardConverter
        {
            get => _ForwardConverter();
            set
            {
                if (_ForwardConverter(value))
                {
                    OnSourceValueChanged();
                }
            }
        }

        [EnableNotificationSupport]
        public Func<DestinationT?, SourceT> BackwardConverter
        {
            get => _BackwardConverter();
            set
            {
                if (_BackwardConverter(value))
                {
                    OnDestinationValueChanged();
                }
            }
        }

        [EnableNotificationSupport]
        public Action<Action>? ForwardSynchronizationHandler
        {
            get => _ForwardSynchronizationHandler();
            set
            {
                if (_ForwardSynchronizationHandler(value))
                {
                    OnSourceValueChanged();
                }
            }
        }

        [EnableNotificationSupport]
        public Action<Action>? BackwardSynchronizationHandler
        {
            get => _BackwardSynchronizationHandler();
            set
            {
                if (_BackwardSynchronizationHandler(value))
                {
                    OnDestinationValueChanged();
                }
            }
        }

        private IValueAccesser<SourceT> _sourceValueAccesser;

        private IValueAccesser<DestinationT> _destinationValueAccesser;

        private int _pushValueNestingCounter;

        private static Func<SourceT?, DestinationT> s_defaultConverter;

        static Binding()
        {
            var arg = Expression.Parameter(typeof(SourceT), "arg");

            try
            {
                var defaultConverterExpression = Expression.Lambda<Func<SourceT?, DestinationT>>(
                    Expression.Convert(arg, typeof(DestinationT)),
                    arg);

                s_defaultConverter = defaultConverterExpression.Compile();
            }
            catch (Exception)
            {
                var defaultConverterExpression = Expression.Lambda<Func<SourceT?, DestinationT>>(
                    Expression.Block(
                        Expression.Throw(Expression.New(typeof(InvalidCastException).GetConstructor(Array.Empty<Type>())!)),
                        Expression.Constant(default(DestinationT), typeof(DestinationT))
                    ),
                    arg
                );

                s_defaultConverter = defaultConverterExpression.Compile();
            }
        }

        internal Binding(IValueAccesser<SourceT> sourceValueAccesser, IValueAccesser<DestinationT> destinationValueAccesser, BindingDirection bindingDirection)
        {
            _sourceValueAccesser = sourceValueAccesser ?? throw new ArgumentNullException(nameof(sourceValueAccesser));
            _destinationValueAccesser = destinationValueAccesser ?? throw new ArgumentNullException(nameof(destinationValueAccesser));

            __bindingDirection = bindingDirection;

            _sourceValueAccesser.ValueChanged += OnSourceValueChanged;
            _destinationValueAccesser.ValueChanged += OnDestinationValueChanged;

            __forwardConverter = s_defaultConverter;
            __backwardConverter = Binding<DestinationT, SourceT>.s_defaultConverter;

            SyncValueCore(ignoreIfSameObject: true);
        }

        public void PerformSyncValue()
        {
            SyncValueCore(ignoreIfSameObject: false);
        }

        private void SyncValueCore(bool ignoreIfSameObject)
        {
            if (BindingDirection == BindingDirection.TwoWay || BindingDirection == BindingDirection.OneWay)
            {
                PushValueForward(ignoreIfSameObject);
            }
            else if (BindingDirection == BindingDirection.OneWayBack)
            {
                PushValueBackward(ignoreIfSameObject);
            }
        }

        private void OnSourceValueChanged()
        {
            if (BindingDirection == BindingDirection.TwoWay || BindingDirection == BindingDirection.OneWay)
            {
                PushValueForward(ignoreIfSameObject: true);
            }
        }

        private void OnDestinationValueChanged()
        {
            if (BindingDirection == BindingDirection.TwoWay || BindingDirection == BindingDirection.OneWayBack)
            {
                PushValueBackward(ignoreIfSameObject: true);
            }
        }

        private void PushValueForward(bool ignoreIfSameObject)
        {
            var nestingCount = Interlocked.Increment(ref _pushValueNestingCounter);
            try
            {
                if (nestingCount > 1)
                {
                    // 無限ループ(再起)を回避するために最初の１回の設定が完了までの呼び出しはすべて無視
                    return;
                }

                if (ForwardSynchronizationHandler is null)
                {
                    _destinationValueAccesser.SetValue(ForwardConverter(_sourceValueAccesser.Value), ignoreIfSameObject);
                }
                else
                {
                    int invokeState = INVOKE_STATE_INIT;

                    ForwardSynchronizationHandler(() =>
                    {
                        Interlocked.Increment(ref _pushValueNestingCounter);
                        try
                        {
                            var prevInvokeState = Interlocked.CompareExchange(ref invokeState, INVOKE_STATE_BEGIN_SET_VALUE, INVOKE_STATE_INIT);

                            if (prevInvokeState != INVOKE_STATE_INIT)
                            {
                                Debug.Fail(null);
                                return;
                            }

                            _destinationValueAccesser.SetValue(ForwardConverter(_sourceValueAccesser.Value), ignoreIfSameObject);

                            prevInvokeState = Interlocked.CompareExchange(ref invokeState, INVOKE_STATE_END_SET_VALUE, INVOKE_STATE_BEGIN_SET_VALUE);

                            if (prevInvokeState != INVOKE_STATE_BEGIN_SET_VALUE)
                            {
                                Debug.Fail(null);
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            ForwardSyncError?.Invoke(ex);
                        }
                        finally
                        {
                            Interlocked.Decrement(ref _pushValueNestingCounter);
                        }
                    });

                    var prevInvokeState = Interlocked.Exchange(ref invokeState, INVOKE_STATE_FINISHED);

                    switch (prevInvokeState)
                    {
                        case INVOKE_STATE_INIT:
                            throw new InvalidOperationException($"{nameof(ForwardSynchronizationHandler)}を介した処理が同期的に実行開始されませんでした。");
                        case INVOKE_STATE_BEGIN_SET_VALUE:
                            throw new InvalidOperationException($"{nameof(ForwardSynchronizationHandler)}を介した処理が同期的に実行完了しませんでした。");
                        case INVOKE_STATE_END_SET_VALUE:
                            break;
                        default:
                            throw new InvalidOperationException($"{nameof(ForwardSynchronizationHandler)}を介した処理が不明な状態です。");
                    }
                }
            }
            catch (Exception ex)
            {
                ForwardSyncError?.Invoke(ex);
            }
            finally
            {
                Interlocked.Decrement(ref _pushValueNestingCounter);
            }
        }

        private void PushValueBackward(bool ignoreIfSameObject)
        {
            var nestingCount = Interlocked.Increment(ref _pushValueNestingCounter);
            try
            {
                if (nestingCount > 1)
                {
                    // 無限ループ(再起)を回避するために最初の１回の設定が完了までの呼び出しはすべて無視
                    return;
                }


                if (BackwardSynchronizationHandler is null)
                {
                    _sourceValueAccesser.SetValue(BackwardConverter(_destinationValueAccesser.Value), ignoreIfSameObject);
                }
                else
                {
                    int invokeState = INVOKE_STATE_INIT;

                    BackwardSynchronizationHandler(() =>
                    {
                        Interlocked.Increment(ref _pushValueNestingCounter);
                        try
                        {
                            var prevInvokeState = Interlocked.CompareExchange(ref invokeState, INVOKE_STATE_BEGIN_SET_VALUE, INVOKE_STATE_INIT);

                            if (prevInvokeState != INVOKE_STATE_INIT)
                            {
                                Debug.Fail(null);
                                return;
                            }

                            _sourceValueAccesser.SetValue(BackwardConverter(_destinationValueAccesser.Value), ignoreIfSameObject);

                            prevInvokeState = Interlocked.CompareExchange(ref invokeState, INVOKE_STATE_END_SET_VALUE, INVOKE_STATE_BEGIN_SET_VALUE);

                            if (prevInvokeState != INVOKE_STATE_BEGIN_SET_VALUE)
                            {
                                Debug.Fail(null);
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            BackwardSyncError?.Invoke(ex);
                        }
                        finally
                        {
                            Interlocked.Decrement(ref _pushValueNestingCounter);
                        }
                    });

                    var prevInvokeState = Interlocked.CompareExchange(ref invokeState, INVOKE_STATE_FINISHED, INVOKE_STATE_END_SET_VALUE);

                    switch (prevInvokeState)
                    {
                        case INVOKE_STATE_INIT:
                            throw new InvalidOperationException($"{nameof(BackwardSynchronizationHandler)}を介した処理が同期的に実行開始されませんでした。");
                        case INVOKE_STATE_BEGIN_SET_VALUE:
                            throw new InvalidOperationException($"{nameof(BackwardSynchronizationHandler)}を介した処理が同期的に実行完了しませんでした。");
                        case INVOKE_STATE_END_SET_VALUE:
                            break;
                        default:
                            throw new InvalidOperationException($"{nameof(BackwardSynchronizationHandler)}を介した処理が不明な状態です。");
                    }
                }
            }
            catch (Exception ex)
            {
                BackwardSyncError?.Invoke(ex);
            }
            finally
            {
                Interlocked.Decrement(ref _pushValueNestingCounter);
            }
        }

        public void Dispose()
        {
            Disposing?.Invoke(this);

            _sourceValueAccesser.ValueChanged -= OnSourceValueChanged;
            _destinationValueAccesser.ValueChanged -= OnDestinationValueChanged;

            Disposed?.Invoke();
        }
    }

}