using System.Threading;

namespace Benutomo.DataBindings
{
    internal abstract class ClassMemberValueBase<ClassT, MemberT, AccessStrategyT, ObserveStrategyT> : IReadOnlyValueAccessor<MemberT>
        where ClassT : class
        where AccessStrategyT : IReadOnlyMemberAccessStrategy<ClassT, MemberT>
        where ObserveStrategyT : IMemberValueChagedObserveStrategy<ClassT>
    {
        public event Action ValueChanged
        {
            add
            {
                lock (this)
                {
                    if (_valueChanged is null)
                    {
                        _objectAccessor.ValueChanged += objectAccessor_ValueChanged;
                        _valueChanged = value;
                    }
                    else
                    {
                        _valueChanged = (Action)Delegate.Combine(_valueChanged, value);
                    }
                }
            }

            remove
            {
                lock (this)
                {
                    _valueChanged = (Action?)Delegate.Remove(_valueChanged, value);

                    if (_valueChanged is null)
                    {
                        _objectAccessor.ValueChanged -= objectAccessor_ValueChanged;
                    }
                }
            }
        }

        public MemberT? Value
        {
            get
            {
                var currentObject = FetchCurrentObject();

                if (currentObject is null) return MemberAccessStrategy.GetDefaultValue(currentObject);

                if (!MemberAccessStrategy.TryGetMemberValue(currentObject, out var memberValue)) return MemberAccessStrategy.GetDefaultValue(currentObject);

                return memberValue;
            }
        }

        protected AccessStrategyT MemberAccessStrategy { get; }

        protected ObserveStrategyT MemberObserveStrategy { get; }

        Action? _valueChanged;

        IReadOnlyValueAccessor<ClassT> _objectAccessor;


        ClassT? _previousObject;

        protected ClassMemberValueBase(IReadOnlyValueAccessor<ClassT> objectAccessor, AccessStrategyT memberAccessStrategy, ObserveStrategyT memberObserveStrategy)
        {
            _objectAccessor = objectAccessor;
            MemberAccessStrategy = memberAccessStrategy;
            MemberObserveStrategy = memberObserveStrategy;

            FetchCurrentObject();
        }

        protected ClassT? FetchCurrentObject()
        {
            var currentObject = _objectAccessor.Value;

            var oldPreviousObject = Interlocked.Exchange(ref _previousObject, currentObject);

            if (!ReferenceEquals(oldPreviousObject, currentObject))
            {
                if (oldPreviousObject is not null)
                {
                    MemberObserveStrategy.UnregisterMemberValueChanged(oldPreviousObject, objectAccessor_ValueChanged);
                }

                if (currentObject is not null)
                {
                    MemberObserveStrategy.RegisterMemberValueChanged(currentObject, objectAccessor_ValueChanged);
                }

                _previousObject = currentObject;
            }

            return currentObject;
        }

        private void objectAccessor_ValueChanged()
        {
            _valueChanged?.Invoke();
        }
    }

}