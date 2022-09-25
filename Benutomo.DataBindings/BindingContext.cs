using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Threading;

namespace Benutomo.DataBindings
{
    public class BindingContext : Component
    {
        public event Action<Exception>? ForwardSyncError;

        public event Action<Exception>? BackwardSyncError;

        List<IBinding?>? _bindings = new List<IBinding?>();

        int _count;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Action<Action>? DefaultForwardSynchronizationHandler
        {
            get;
            set;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Action<Action>? DefaultBackwardSynchronizationHandler
        {
            get;
            set;
        }

        public BindingContext()
        {

        }

        public BindingContext(IContainer container)
        {
            container.Add(this);
        }

        public Binding<SourceMemberT, DestMemberT> MakeBinding<SourceT, SourceMemberT, DestT, DestMemberT>(SourceT source, Expression<Func<SourceT, SourceMemberT>> sourceMemberAccessExpression, DestT destination, Expression<Func<DestT, DestMemberT>> destinationMemberAccessExpression)
        {
            if (!(Volatile.Read(ref _bindings) is { } bindings)) throw new ObjectDisposedException(null);

            Binding<SourceMemberT, DestMemberT>? binding = null;
            
            try
            {
                binding = DataBinding.MakeBinding(source, sourceMemberAccessExpression, destination, destinationMemberAccessExpression);

                binding.ForwardSynchronizationHandler = DefaultForwardSynchronizationHandler;
                binding.BackwardSynchronizationHandler = DefaultBackwardSynchronizationHandler;

                binding.ForwardSyncError += Binding_ForwardSyncError;
                binding.BackwardSyncError += Binding_BackwardSyncError;

                Attach(binding);
            }
            catch
            {
                binding?.Dispose();
                throw;
            }

            return binding;
        }

        private void Attach(IBinding binding)
        {
            if (!(Volatile.Read(ref _bindings) is { } bindings)) throw new ObjectDisposedException(null);

            lock (bindings)
            {
                // すれ違いでDiposeが呼び出されている
                if (Volatile.Read(ref _bindings) is null) throw new ObjectDisposedException(null);

                if (bindings.Count > _count)
                {
                    for (int i = 0; i < bindings.Count; i++)
                    {
                        if (bindings[i] is null)
                        {
                            bindings[i] = binding;
                            _count++;
                            return;
                        }
                    }
                }

                binding.Disposing += Detach;
                bindings.Add(binding);
                _count++;
                return;
            }
        }

        private void Detach(IBinding binding)
        {
            binding.ForwardSyncError -= Binding_ForwardSyncError;
            binding.BackwardSyncError -= Binding_BackwardSyncError;

            if (!(Volatile.Read(ref _bindings) is { } bindings)) return;

            lock (bindings)
            {
                for (int i = 0; i < bindings.Count; i++)
                {
                    if (ReferenceEquals(bindings[i], binding))
                    {
                        bindings[i] = null;
                        _count--;
                        return;
                    }
                }
            }
        }

        private void Binding_BackwardSyncError(Exception exception)
        {
            ForwardSyncError?.Invoke(exception);
        }

        private void Binding_ForwardSyncError(Exception exception)
        {
            BackwardSyncError?.Invoke(exception);
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;

            var bindings = Interlocked.Exchange(ref _bindings, null);

            if (bindings is null) return;

            lock(bindings)
            {
                for (int i = 0; i < bindings.Count; i++)
                {
                    var binding = bindings[i];
                    if (binding is null) continue;

                    binding.Disposing -= Detach;
                    bindings[i] = null;
                    _count--;

                    binding.Dispose();
                }
            }

        }
    }
}