namespace Benutomo.DataBindings
{
    internal interface IValueAccesser<T> : IReadOnlyValueAccessor<T>
    {
        void SetValue(T? value, bool ignoreIfSameObject);
    }
}