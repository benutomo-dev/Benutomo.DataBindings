namespace Benutomo.DataBindings
{
    internal interface IMemberAccessStrategy<ObjectT, MemberT> : IReadOnlyMemberAccessStrategy<ObjectT, MemberT>
    {
        bool TrySetMemberValue(ObjectT obj, MemberT? memberValue);
    }
}