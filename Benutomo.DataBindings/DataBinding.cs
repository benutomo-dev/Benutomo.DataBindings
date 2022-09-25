using System.Collections.Generic;
using System.Linq.Expressions;

namespace Benutomo.DataBindings
{
    public static class DataBinding
    {
        static class ValueAccessorBuilder<ObjectT, MemberT>
        {
#if DEBUG
            static string? s_objectTName = typeof(ObjectT).FullName;
            static string? s_memberTName = typeof(MemberT).FullName;
#endif

            public static IValueAccesser<MemberT> BuildFromMemberExpression(MemberExpression expression, IReadOnlyDictionary<ParameterExpression, IReadOnlyValueAccessor> parameters)
            {
                if (expression.Expression is null)
                {
                    throw new ArgumentException(null, nameof(expression));
                }

                IReadOnlyValueAccessor? readOnlyValueAccessor;

                if (expression.Expression is ParameterExpression parameterExpression)
                {
                    if (!parameters.TryGetValue(parameterExpression, out readOnlyValueAccessor))
                    {
                        throw new ArgumentException(null, nameof(parameters));
                    }
                }
                else
                {
                    readOnlyValueAccessor = CreateReadOnlyValueFromExpression(expression.Expression, parameters);
                }

                var memberAccessStrategy = new FieldOrPropertyAccessStrategy<ObjectT, MemberT>(expression.Member);
                var observerStrategy = ObserveStrategy.CreateMemberObserverStrategy<ObjectT>(expression.Member);

                if (typeof(ObjectT).IsValueType)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    return (IValueAccesser<MemberT>?)Activator.CreateInstance(
                        typeof(ClassMemberValue<,>).MakeGenericType(typeof(ObjectT), typeof(MemberT)),
                        new object[] { readOnlyValueAccessor, memberAccessStrategy, observerStrategy }
                        )
                        ?? throw new InvalidOperationException();
                }
            }


            public static IValueAccesser<ObjectT> CreateReadOnlyValueFromExpression(Expression expression, IReadOnlyDictionary<ParameterExpression, IReadOnlyValueAccessor> parameters)
            {
                if (expression is MemberExpression memberExpression)
                {
                    if (memberExpression.Expression is null)
                    {
                        throw new ArgumentException(null, nameof(expression));
                    }

                    var builderType = typeof(ValueAccessorBuilder<,>).MakeGenericType(memberExpression.Expression.Type, typeof(ObjectT));

                    return (IValueAccesser<ObjectT>?)builderType.GetMethod(nameof(ValueAccessorBuilder<int, int>.BuildFromMemberExpression))!.Invoke(null, new object[] { memberExpression, parameters }) ?? throw new InvalidOperationException();
                }
                else if (expression is MethodCallExpression methodCallExpression)
                {
                    throw new NotImplementedException();
                }
                else if (expression is ConstantExpression constantExpression)
                {
                    throw new NotImplementedException();
                }
                else if (expression is ParameterExpression parameterExpression)
                {
                    throw new NotImplementedException();
                }
                throw new NotSupportedException();
            }
        }

        static class ValueAccessorBuilder
        {
            public static IValueAccesser<MemberT> Build<Arg1T, MemberT>(Expression<Func<Arg1T, MemberT>> expression, Arg1T arg1)
            {
#if DEBUG
                string? objectTName = typeof(Arg1T).FullName;
                string? memberTName = typeof(MemberT).FullName;
#endif

                if (expression.Parameters.Count != 1)
                {
                    throw new ArgumentException(null, nameof(expression));
                }

                var parameters = new Dictionary<ParameterExpression, IReadOnlyValueAccessor>
                {
                    [expression.Parameters[0]] = new ConstValue<Arg1T>(arg1),
                };

                if (expression.Body is MemberExpression memberExpression)
                {
                    if (memberExpression.Expression is null)
                    {
                        throw new ArgumentException(null, nameof(expression));
                    }

                    var builderType = typeof(ValueAccessorBuilder<,>).MakeGenericType(memberExpression.Expression.Type, typeof(MemberT));

                    return (IValueAccesser<MemberT>?)builderType.GetMethod(nameof(ValueAccessorBuilder<MemberT, MemberT>.BuildFromMemberExpression))!.Invoke(null, new object[] { memberExpression, parameters }) ?? throw new InvalidOperationException();
                }
                else if (expression.Body is MethodCallExpression methodCallExpression)
                {
                    throw new NotImplementedException();
                }
                else if (expression.Body is ConstantExpression constantExpression)
                {
                    throw new NotImplementedException();
                }
                else if (expression.Body is ParameterExpression parameterExpression)
                {
                    throw new NotImplementedException();
                }
                throw new NotSupportedException();
            }
        }

        public static Binding<SourceMemberT, DestMemberT> MakeBinding<SourceT, SourceMemberT, DestT, DestMemberT>(SourceT source, Expression<Func<SourceT, SourceMemberT>> sourceMemberAccessExpression, DestT destination, Expression<Func<DestT, DestMemberT>> destinationMemberAccessExpression)
        {
            var sourceValueAccessor = ValueAccessorBuilder.Build(sourceMemberAccessExpression, source);
            var destinationValueAccessor = ValueAccessorBuilder.Build(destinationMemberAccessExpression, destination);

            return new Binding<SourceMemberT, DestMemberT>(sourceValueAccessor, destinationValueAccessor, BindingDirection.TwoWay);
        }
    }
}