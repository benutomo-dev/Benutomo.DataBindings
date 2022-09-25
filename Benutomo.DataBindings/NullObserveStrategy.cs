namespace Benutomo.DataBindings
{
    internal sealed class NullObserveStrategy<ObjectT> : IMemberValueChagedObserveStrategy<ObjectT>
    {
        public static NullObserveStrategy<ObjectT> Default { get; } = new NullObserveStrategy<ObjectT>();

        private NullObserveStrategy()
        {
        }

        public void RegisterMemberValueChanged(ObjectT obj, Action valueChangedCallback)
        {
        }

        public void UnregisterMemberValueChanged(ObjectT obj, Action valueChangedCallback)
        {
        }
    }
}