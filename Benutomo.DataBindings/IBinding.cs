namespace Benutomo.DataBindings
{
    public interface IBinding : IDisposable
    {
        event Action<Exception>? ForwardSyncError;

        event Action<Exception>? BackwardSyncError;

        event Action<IBinding>? Disposing;

        event Action? Disposed;

        BindingDirection BindingDirection { get; set; }

        Action<Action>? ForwardSynchronizationHandler { get; set; }

        Action<Action>? BackwardSynchronizationHandler { get; set; }
    }

}