using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Benutomo.DataBindings
{
    internal class FieldOrPropertyReadOnlyAccessStrategy<ClassT, MemberT> : IReadOnlyMemberAccessStrategy<ClassT, MemberT>
    {
        Func<ClassT, MemberT> _getValue;

        private static ConcurrentDictionary<MemberInfo, Func<ClassT, MemberT>> s_compiledGetValueCache = new ConcurrentDictionary<MemberInfo, Func<ClassT, MemberT>>();

        public FieldOrPropertyReadOnlyAccessStrategy(MemberInfo memberInfo)
        {
            _getValue = s_compiledGetValueCache.GetOrAdd(memberInfo, static memberInfo =>
            {
                var getValueParameterExpression = Expression.Parameter(typeof(ClassT), "obj");

                var getValueExpression = Expression.Lambda<Func<ClassT, MemberT>>(
                    Expression.MakeMemberAccess(getValueParameterExpression, memberInfo),
                    getValueParameterExpression
                    );

                var getValue = getValueExpression.Compile();

                return getValue;
            });
        }

        public MemberT? GetDefaultValue(ClassT? obj)
        {
            return default(MemberT);
        }

        public bool TryGetMemberValue(ClassT obj, out MemberT memberValue)
        {
            memberValue = _getValue(obj);
            return true;
        }
    }

}