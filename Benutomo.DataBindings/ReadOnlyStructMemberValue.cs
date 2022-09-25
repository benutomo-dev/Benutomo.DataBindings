namespace Benutomo.DataBindings
{
    internal class ReadOnlyStructMemberValue<StructT, MemberT, AccessStrategyT, ObserveStrategyT> : IReadOnlyValueAccessor<MemberT>
        where StructT : struct
        where AccessStrategyT : IReadOnlyMemberAccessStrategy<StructT, MemberT>
        where ObserveStrategyT : IMemberValueChagedObserveStrategy<StructT>
    {
        public event Action ValueChanged
        {
            add
            {
                lock (this)
                {
                    if (_valueChanged is null)
                    {
                        _objectAccessor.ValueChanged += objectAccessor_ValueChanged;
                        _valueChanged = value;
                    }
                    else
                    {
                        _valueChanged = (Action)Delegate.Combine(_valueChanged, value);
                    }
                }
            }

            remove
            {
                lock (this)
                {
                    _valueChanged = (Action?)Delegate.Remove(_valueChanged, value);

                    if (_valueChanged is null)
                    {
                        _objectAccessor.ValueChanged -= objectAccessor_ValueChanged;
                    }
                }
            }
        }

        public MemberT? Value
        {
            get
            {
                var currentObject = FetchCurrentObject();

                if (!MemberAccessStrategy.TryGetMemberValue(currentObject, out var memberValue)) return MemberAccessStrategy.GetDefaultValue(currentObject);

                return memberValue;
            }
        }

        Action? _valueChanged;

        IReadOnlyValueAccessor<StructT> _objectAccessor;

        protected AccessStrategyT MemberAccessStrategy { get; }
        protected ObserveStrategyT MemberObserveStrategy { get; }

        StructT _previousObject;

        public ReadOnlyStructMemberValue(IReadOnlyValueAccessor<StructT> objectAccessor, AccessStrategyT memberAccessStrategy, ObserveStrategyT memberObserveStrategy)
        {
            _objectAccessor = objectAccessor;
            MemberAccessStrategy = memberAccessStrategy;
            MemberObserveStrategy = memberObserveStrategy;

            FetchCurrentObject();
        }

        private StructT FetchCurrentObject()
        {
            lock (this)
            {
                var oldPreviousObject = _previousObject;

                var currentObject = _objectAccessor.Value;

                MemberObserveStrategy.UnregisterMemberValueChanged(_previousObject, objectAccessor_ValueChanged);

                MemberObserveStrategy.RegisterMemberValueChanged(currentObject, objectAccessor_ValueChanged);

                _previousObject = currentObject;

                return currentObject;
            }
        }

        private void objectAccessor_ValueChanged()
        {
            _valueChanged?.Invoke();
        }
    }
}