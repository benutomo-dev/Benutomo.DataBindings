using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Benutomo.DataBindings
{
    internal class FieldOrPropertyAccessStrategy<ClassT, MemberT> : FieldOrPropertyReadOnlyAccessStrategy<ClassT, MemberT>, IMemberAccessStrategy<ClassT, MemberT>
    {
        Action<ClassT, MemberT?> _setValue;

        private static ConcurrentDictionary<MemberInfo, Action<ClassT, MemberT?>> s_compiledSetValueCache = new ConcurrentDictionary<MemberInfo, Action<ClassT, MemberT?>>();

        public FieldOrPropertyAccessStrategy(MemberInfo memberInfo) : base(memberInfo)
        {
            _setValue = s_compiledSetValueCache.GetOrAdd(memberInfo, static memberInfo =>
            {
                var objParameterExpression = Expression.Parameter(typeof(ClassT), "obj");
                var valueParameterExpression = Expression.Parameter(typeof(MemberT), "value");

                var setValueExpression = Expression.Lambda<Action<ClassT, MemberT?>>(
                    Expression.Assign(
                        Expression.MakeMemberAccess(objParameterExpression, memberInfo),
                        valueParameterExpression
                    ),
                    objParameterExpression,
                    valueParameterExpression
                    );

                var setValue = setValueExpression.Compile();

                return setValue;
            });
        }

        public bool TrySetMemberValue(ClassT obj, MemberT? memberValue)
        {
            _setValue(obj, memberValue);
            return true;
        }
    }

}