namespace Benutomo.DataBindings
{
    internal sealed class ReadOnlyClassMemberValue<ClassT, MemberT> : ClassMemberValueBase<ClassT, MemberT, IReadOnlyMemberAccessStrategy<ClassT, MemberT>, IMemberValueChagedObserveStrategy<ClassT>>
        where ClassT : class
    {
        public ReadOnlyClassMemberValue(IReadOnlyValueAccessor<ClassT> objectAccessor, IReadOnlyMemberAccessStrategy<ClassT, MemberT> memberAccessStrategy, IMemberValueChagedObserveStrategy<ClassT> memberObserveStrategy) : base(objectAccessor, memberAccessStrategy, memberObserveStrategy)
        {
        }
    }
}