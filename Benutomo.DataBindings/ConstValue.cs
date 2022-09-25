namespace Benutomo.DataBindings
{
    internal sealed class ConstValue<T> : IReadOnlyValueAccessor<T>
    {
        public event Action ValueChanged
        {
            add { }
            remove { }
        }

        public T Value { get; }

        public ConstValue(T value)
        {
            Value = value;
        }
    }
}