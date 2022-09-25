using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Benutomo.DataBindings
{
    internal static partial class ObserveStrategy
    {
        class ChangedActionEventObserveStrategy<ObjectT> : IMemberValueChagedObserveStrategy<ObjectT>
        {
            private Action<ObjectT, Action> _register;
            private Action<ObjectT, Action> _unregister;

            private static ConcurrentDictionary<EventInfo, ChangedActionEventObserveStrategy<ObjectT>> s_instanceCache = new ConcurrentDictionary<EventInfo, ChangedActionEventObserveStrategy<ObjectT>>();

            private ChangedActionEventObserveStrategy(EventInfo eventInfo)
            {
                if (eventInfo.AddMethod is null) throw new ArgumentException(null, nameof(eventInfo));
                if (eventInfo.RemoveMethod is null) throw new ArgumentException(null, nameof(eventInfo));

                var objParameterExpression = Expression.Parameter(typeof(ObjectT), "obj");
                var valueChangedCallbackParameterExpression = Expression.Parameter(typeof(Action), "valueChangedCallback");

                var registerExpression = Expression.Lambda<Action<ObjectT, Action>>(
                    Expression.Call(objParameterExpression, eventInfo.AddMethod, Expression.Constant(valueChangedCallbackParameterExpression)),
                    objParameterExpression,
                    valueChangedCallbackParameterExpression
                    );

                var unregisterExpression = Expression.Lambda<Action<ObjectT, Action>>(
                    Expression.Call(objParameterExpression, eventInfo.RemoveMethod, Expression.Constant(valueChangedCallbackParameterExpression)),
                    objParameterExpression,
                    valueChangedCallbackParameterExpression
                    );

                _register = registerExpression.Compile();
                _unregister = unregisterExpression.Compile();
            }

            public static ChangedActionEventObserveStrategy<ObjectT> Get(EventInfo eventInfo)
            {
                if (eventInfo.AddMethod is null) throw new ArgumentException(null, nameof(eventInfo));
                if (eventInfo.RemoveMethod is null) throw new ArgumentException(null, nameof(eventInfo));

                if (eventInfo.AddMethod.ReturnType != typeof(void)) throw new ArgumentException(null, nameof(eventInfo));
                if (eventInfo.RemoveMethod.ReturnType != typeof(void)) throw new ArgumentException(null, nameof(eventInfo));

                return s_instanceCache.GetOrAdd(eventInfo, eventInfo => new ChangedActionEventObserveStrategy<ObjectT>(eventInfo));
            }

            public void RegisterMemberValueChanged(ObjectT obj, Action valueChangedCallback) => _register(obj, valueChangedCallback);

            public void UnregisterMemberValueChanged(ObjectT obj, Action valueChangedCallback) => _unregister(obj, valueChangedCallback);
        }
    }
}