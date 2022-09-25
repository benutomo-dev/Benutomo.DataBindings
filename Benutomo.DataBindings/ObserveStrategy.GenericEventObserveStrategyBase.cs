using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace Benutomo.DataBindings
{
    internal static partial class ObserveStrategy
    {
        internal abstract class GenericEventObserveStrategyBase<ObjectT, EventHanderT> : IMemberValueChagedObserveStrategy<ObjectT> where EventHanderT : Delegate
        {
            private Action<ObjectT, EventHanderT> _register;
            private Action<ObjectT, EventHanderT> _unregister;

            private (Action action, EventHanderT eventHandler)[]? _callbackProxyList;

            private static ConcurrentDictionary<EventInfo, (Action<ObjectT, EventHanderT> register, Action<ObjectT, EventHanderT> unregister)> s_instanceCache = new ConcurrentDictionary<EventInfo, (Action<ObjectT, EventHanderT> register, Action<ObjectT, EventHanderT> unregister)>();

            public GenericEventObserveStrategyBase(EventInfo eventInfo)
            {
                if (eventInfo.AddMethod is null) throw new ArgumentException(null, nameof(eventInfo));
                if (eventInfo.RemoveMethod is null) throw new ArgumentException(null, nameof(eventInfo));

                if (eventInfo.AddMethod.ReturnType != typeof(void)) throw new ArgumentException(null, nameof(eventInfo));
                if (eventInfo.RemoveMethod.ReturnType != typeof(void)) throw new ArgumentException(null, nameof(eventInfo));

                (_register, _unregister) = s_instanceCache.GetOrAdd(eventInfo, static eventInfo =>
                {
                    var objParameterExpression = Expression.Parameter(typeof(ObjectT), "obj");
                    var valueChangedCallbackParameterExpression = Expression.Parameter(typeof(EventHanderT), "valueChangedCallback");

                    var registerExpression = Expression.Lambda<Action<ObjectT, EventHanderT>>(
                        Expression.Call(objParameterExpression, eventInfo.AddMethod!, valueChangedCallbackParameterExpression),
                        objParameterExpression,
                        valueChangedCallbackParameterExpression
                        );

                    var unregisterExpression = Expression.Lambda<Action<ObjectT, EventHanderT>>(
                        Expression.Call(objParameterExpression, eventInfo.RemoveMethod!, valueChangedCallbackParameterExpression),
                        objParameterExpression,
                        valueChangedCallbackParameterExpression
                        );

                    var register = registerExpression.Compile();
                    var unregister = unregisterExpression.Compile();

                    return (register, unregister);
                });
            }

            protected abstract EventHanderT CraeteTrumplinDelegate(ObjectT obj, Action valueChangedCallback);

            public void RegisterMemberValueChanged(ObjectT obj, Action valueChangedCallback)
            {
                if (valueChangedCallback is null) throw new ArgumentNullException(nameof(valueChangedCallback));

                var eventHandler = CraeteTrumplinDelegate(obj, valueChangedCallback);

                _register(obj, eventHandler);

                while (true)
                {
                    var currentCallbackProxyList = Volatile.Read(ref _callbackProxyList);

                    var newCallbackList = new (Action action, EventHanderT eventHandler)[(currentCallbackProxyList?.Length ?? 0) + 1];

                    currentCallbackProxyList.AsSpan().CopyTo(newCallbackList.AsSpan());
                    newCallbackList[newCallbackList.Length - 1] = (valueChangedCallback, eventHandler);

                    if (ReferenceEquals(Interlocked.CompareExchange(ref _callbackProxyList, newCallbackList, currentCallbackProxyList), currentCallbackProxyList))
                    {
                        break;
                    }
                }
            }

            public void UnregisterMemberValueChanged(ObjectT obj, Action valueChangedCallback)
            {
                if (valueChangedCallback is null)
                {
                    Debug.Fail(null);
                    return;
                }

                EventHanderT eventHandler;

                while (true)
                {
                    var currentCallbackProxyList = Volatile.Read(ref _callbackProxyList);

                    if (currentCallbackProxyList is null)
                    {
                        Debug.Fail(null);
                        return;
                    }

                    int removeIndex = int.MinValue;
                    for (int i = 0; i < currentCallbackProxyList.Length; i++)
                    {
                        if (currentCallbackProxyList[i].action == valueChangedCallback)
                        {
                            eventHandler = currentCallbackProxyList[i].eventHandler;
                            removeIndex = i;
                            goto SUCCESS_FINDING;
                        }
                    }

                    Debug.Fail(null);
                    return;

                SUCCESS_FINDING:

                    (Action action, EventHanderT eventHandler)[]? newCallbackProxyList;
                    if (currentCallbackProxyList.Length == 1)
                    {
                        newCallbackProxyList = null;
                    }
                    else
                    {
                        newCallbackProxyList = new (Action action, EventHanderT eventHandler)[currentCallbackProxyList.Length - 1];

                        if (removeIndex == 0)
                        {
                            currentCallbackProxyList.AsSpan(0, currentCallbackProxyList.Length - 1).CopyTo(newCallbackProxyList.AsSpan());
                        }
                        else if (removeIndex == currentCallbackProxyList.Length - 1)
                        {
                            currentCallbackProxyList.AsSpan(1).CopyTo(newCallbackProxyList.AsSpan());
                        }
                        else
                        {
                            currentCallbackProxyList.AsSpan(0, removeIndex).CopyTo(newCallbackProxyList.AsSpan());
                            currentCallbackProxyList.AsSpan(removeIndex + 1, currentCallbackProxyList.Length - removeIndex - 1).CopyTo(newCallbackProxyList.AsSpan(removeIndex));
                        }
                    }

                    if (ReferenceEquals(Interlocked.CompareExchange(ref _callbackProxyList, newCallbackProxyList, currentCallbackProxyList), currentCallbackProxyList))
                    {
                        break;
                    }
                }

                _unregister(obj, eventHandler);
            }
        }
    }
}