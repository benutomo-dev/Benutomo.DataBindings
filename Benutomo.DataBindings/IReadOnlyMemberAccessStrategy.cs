namespace Benutomo.DataBindings
{
    internal interface IReadOnlyMemberAccessStrategy<ObjectT, MemberT>
    {
        MemberT? GetDefaultValue(ObjectT? obj);

        bool TryGetMemberValue(ObjectT obj, out MemberT memberValue);
    }
}