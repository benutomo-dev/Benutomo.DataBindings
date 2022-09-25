using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Benutomo.DataBindings
{
    internal static partial class ObserveStrategy
    {
        public static IMemberValueChagedObserveStrategy<ObjectT> CreateMemberObserverStrategy<ObjectT>(MemberInfo memberInfo)
        {
            var memberName = memberInfo.Name;
            var declaringType = memberInfo.DeclaringType;
            bool isPublic;

            if (declaringType is null)
            {
                throw new ArgumentException(null, nameof(memberInfo));
            }

            if (!declaringType.IsAssignableFrom(typeof(ObjectT)))
            {
                throw new ArgumentException(null, nameof(memberInfo));
            }

            if (memberInfo is FieldInfo fieldInfo)
            {
                if (fieldInfo.IsStatic)
                {
                    throw new ArgumentException(null, nameof(memberInfo));
                }

                isPublic = fieldInfo.IsPublic;
            }
            else if (memberInfo is PropertyInfo propertyInfo)
            {
                if (propertyInfo.GetMethod is null)
                {
                    throw new ArgumentException(null, nameof(memberInfo));
                }

                if (propertyInfo.GetMethod.IsStatic)
                {
                    throw new ArgumentException(null, nameof(memberInfo));
                }

                isPublic = propertyInfo.GetMethod.IsPublic;
            }
            else
            {
                throw new ArgumentException(null, nameof(memberInfo));
            }

            var bindingFlags = BindingFlags.Instance | (isPublic ? BindingFlags.Public : BindingFlags.NonPublic);

            IMemberValueChagedObserveStrategy<ObjectT>? strategy;

            if (TryGetStrategy(declaringType, memberName, bindingFlags, out strategy))
            {
                return strategy;
            }

            if (declaringType != typeof(ObjectT) && TryGetStrategy(typeof(ObjectT), memberName, bindingFlags, out strategy))
            {
                return strategy;
            }

            return NullObserveStrategy<ObjectT>.Default;


            static bool TryGetStrategy(Type type, string memberName, BindingFlags bindingFlags, [MaybeNullWhen(false)] out IMemberValueChagedObserveStrategy<ObjectT> strategy)
            {
                var changedEvent = type.GetEvent($"{memberName}Changed", bindingFlags);

                if (changedEvent is not null && changedEvent.AddMethod?.ReturnType == typeof(void))
                {
                    var parameters = changedEvent.AddMethod.GetParameters()[0].ParameterType.GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public)!.GetParameters();
                    if (parameters.Length == 0)
                    {
                        strategy = ChangedActionEventObserveStrategy<ObjectT>.Get(changedEvent);
                        return true;
                    }
                    else if (parameters.Length == 2 && parameters[0].ParameterType == typeof(object) && parameters[1].ParameterType == typeof(EventArgs))
                    {
                        strategy = new ChangedEventHandlerEventObserveStrategy<ObjectT>(changedEvent);
                        return true;
                    }
                }

                var propertyChangedEvent = type.GetEvent(nameof(INotifyPropertyChanged.PropertyChanged), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (propertyChangedEvent is not null && propertyChangedEvent.AddMethod?.ReturnType == typeof(void))
                {
                    var parameters = propertyChangedEvent.AddMethod.GetParameters();

                    if (parameters.Length == 2 && parameters[0].ParameterType == typeof(object) && parameters[1].ParameterType == typeof(PropertyChangedEventArgs))
                    {
                        strategy = new PropertyChangedEventObserveStrategy<ObjectT>(propertyChangedEvent, memberName);
                        return true;
                    }
                }

                strategy = default;
                return false;
            }
        }
    }
}