using System.Reflection;

namespace Benutomo.DataBindings
{
    internal static partial class ObserveStrategy
    {
        class ChangedEventHandlerEventObserveStrategy<ObjectT> : GenericEventObserveStrategyBase<ObjectT, EventHandler>
        {
            public ChangedEventHandlerEventObserveStrategy(EventInfo eventInfo) : base(eventInfo)
            {
            }

            protected override EventHandler CraeteTrumplinDelegate(ObjectT obj, Action valueChangedCallback)
            {
                var eventHandler = new EventHandler((src, args) => valueChangedCallback.Invoke());

                return eventHandler;
            }
        }
    }
}