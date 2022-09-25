namespace Benutomo.DataBindings
{
    internal interface IMemberValueChagedObserveStrategy<ObjectT>
    {
        void RegisterMemberValueChanged(ObjectT obj, Action valueChangedCallback);

        void UnregisterMemberValueChanged(ObjectT obj, Action valueChangedCallback);
    }
}