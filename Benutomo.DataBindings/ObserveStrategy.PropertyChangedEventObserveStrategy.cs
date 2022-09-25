using System.ComponentModel;
using System.Reflection;

namespace Benutomo.DataBindings
{
    internal static partial class ObserveStrategy
    {
        class PropertyChangedEventObserveStrategy<ObjectT> : GenericEventObserveStrategyBase<ObjectT, PropertyChangedEventHandler>
        {
            private string _propertyName;

            public PropertyChangedEventObserveStrategy(EventInfo eventInfo, string propertyName) : base(eventInfo)
            {
                _propertyName = propertyName;
            }

            protected override PropertyChangedEventHandler CraeteTrumplinDelegate(ObjectT obj, Action valueChangedCallback)
            {
                var eventHandler = new PropertyChangedEventHandler((src, args) =>
                {
                    if (args.PropertyName == _propertyName)
                    {
                        valueChangedCallback();
                    }
                });

                return eventHandler;
            }
        }
    }

}