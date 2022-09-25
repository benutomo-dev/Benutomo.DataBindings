namespace Benutomo.DataBindings
{
    internal interface IReadOnlyValueAccessor
    {
    }

    internal interface IReadOnlyValueAccessor<T> : IReadOnlyValueAccessor
    {
        event Action ValueChanged;

        T? Value { get; }
    }
}