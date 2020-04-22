using System;
using Avalonia.Collections.Pooled;
using Avalonia.Data;

#nullable enable

namespace Avalonia.Reactive
{
    internal abstract class AvaloniaPropertyObservable<T> :
        LightweightObservableBase<AvaloniaPropertyChangedEventArgs<T>>,
        IDescription
    {
        private readonly WeakReference<AvaloniaObject> _owner;
        private PooledQueue<AvaloniaPropertyChangedEventArgs<T>>? _queue;
        private AvaloniaPropertyChangedEventArgs<T>? _publishing;
        private ValueSelector? _valueAdapter;
        private BindingValueSelector? _bindingValueAdapter;
        private UntypedValueSelector? _untypedValueAdapter;

        private AvaloniaPropertyObservable(AvaloniaObject owner)
        {
            owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _owner = new WeakReference<AvaloniaObject>(owner);
        }

        public string Description
        {
            get
            {
                if (_owner.TryGetTarget(out var owner))
                {
                    return $"{owner.GetType().Name}.{Property.Name}";
                }
                else
                {
                    return $"(dead).{Property.Name}";
                }
            }
        }

        public abstract AvaloniaProperty<T> Property { get; }

        public IObservable<T> ValueAdapter
        {
            get => _valueAdapter ??= new ValueSelector(this);
        }

        public IObservable<BindingValue<T>> BindingValueAdapter
        {
            get => _bindingValueAdapter ??= new BindingValueSelector(this);
        }

        public IObservable<object?> UntypedValueAdapter
        {
            get => _untypedValueAdapter ??= new UntypedValueSelector(this);
        }

        public static AvaloniaPropertyObservable<T> Create(AvaloniaObject o, StyledPropertyBase<T> property)
        {
            return new Styled(o, property);
        }

        public static AvaloniaPropertyObservable<T> Create(AvaloniaObject o, DirectPropertyBase<T> property)
        {
            return new Direct(o, property);
        }

        public Optional<T> GetValue()
        {
            if (_owner.TryGetTarget(out var owner))
            {
                return GetValue(owner);
            }

            return default;
        }

        public void Signal(AvaloniaPropertyChangedEventArgs<T> change)
        {
            if (SignalCore(change) && _queue is object)
            {
                while (_queue.Count > 0)
                {
                    var queuedChange = _queue.Dequeue();

                    if (_queue.Count != 0)
                    {
                        queuedChange.MarkOutdated();
                    }

                    SignalCore(queuedChange);
                }

                _queue.Dispose();
                _queue = null;
            }
        }

        protected abstract T GetValue(AvaloniaObject owner);
        
        protected override void Initialize() { }
        protected override void Deinitialize() { }

        private bool SignalCore(AvaloniaPropertyChangedEventArgs<T> change)
        {
            if (_publishing is null)
            {
                _publishing = change;
                PublishNext(change);
                _publishing = null;
                return true;
            }
            else
            {
                _queue ??= new PooledQueue<AvaloniaPropertyChangedEventArgs<T>>();
                _queue.Enqueue(change);
                _publishing.MarkOutdated();
                return false;
            }
        }

        private class ValueSelector : LightweightObservableBase<T>,
            IObserver<AvaloniaPropertyChangedEventArgs<T>>
        {
            private readonly AvaloniaPropertyObservable<T> _source;
            private IDisposable? _subscription;
            public ValueSelector(AvaloniaPropertyObservable<T> source) => _source = source;
            public void OnCompleted() { }
            public void OnError(Exception error) { }

            public void OnNext(AvaloniaPropertyChangedEventArgs<T> value)
            {
                if (value.IsActiveValueChange && !value.IsOutdated)
                {
                    PublishNext(value.NewValue.Value);
                }
            }

            protected override void Initialize() => _subscription = _source.Subscribe(this);
            protected override void Deinitialize() => _subscription?.Dispose();

            protected override void Subscribed(IObserver<T> observer, bool first)
            {
                var value = _source.GetValue();

                if (value.HasValue)
                {
                    observer.OnNext(value.Value);
                }
            }
        }

        private class BindingValueSelector : LightweightObservableBase<BindingValue<T>>,
            IObserver<AvaloniaPropertyChangedEventArgs<T>>
        {
            private readonly AvaloniaPropertyObservable<T> _source;
            private IDisposable? _subscription;
            public BindingValueSelector(AvaloniaPropertyObservable<T> source) => _source = source;
            public void OnCompleted() { }
            public void OnError(Exception error) { }

            public void OnNext(AvaloniaPropertyChangedEventArgs<T> value)
            {
                if (value.IsActiveValueChange && !value.IsOutdated)
                {
                    PublishNext(value.NewValue);
                }
            }

            protected override void Initialize() => _subscription = _source.Subscribe(this);
            protected override void Deinitialize() => _subscription?.Dispose();

            protected override void Subscribed(IObserver<BindingValue<T>> observer, bool first)
            {
                var value = _source.GetValue();

                if (value.HasValue)
                {
                    observer.OnNext(value.Value);
                }
            }
        }

        private class UntypedValueSelector : LightweightObservableBase<object?>,
            IObserver<AvaloniaPropertyChangedEventArgs<T>>
        {
            private readonly AvaloniaPropertyObservable<T> _source;
            private IDisposable? _subscription;
            public UntypedValueSelector(AvaloniaPropertyObservable<T> source) => _source = source;
            public void OnCompleted() { }
            public void OnError(Exception error) { }

            public void OnNext(AvaloniaPropertyChangedEventArgs<T> value)
            {
                if (value.IsActiveValueChange && !value.IsOutdated)
                {
                    PublishNext(value.NewValue.ToUntyped());
                }
            }

            protected override void Initialize() => _subscription = _source.Subscribe(this);
            protected override void Deinitialize() => _subscription?.Dispose();

            protected override void Subscribed(IObserver<object?> observer, bool first)
            {
                var value = _source.GetValue();

                if (value.HasValue)
                {
                    observer.OnNext(value.Value);
                }
            }
        }

        private class Styled : AvaloniaPropertyObservable<T>
        {
            private readonly StyledPropertyBase<T> _property;

            public Styled(AvaloniaObject owner, StyledPropertyBase<T> property)
                : base(owner)
            {
                _property = property ?? throw new ArgumentNullException(nameof(property));
            }

            public override AvaloniaProperty<T> Property => _property;
            protected override T GetValue(AvaloniaObject owner) => owner.GetValue(_property);
        }

        private class Direct : AvaloniaPropertyObservable<T>
        {
            private readonly DirectPropertyBase<T> _property;

            public Direct(AvaloniaObject owner, DirectPropertyBase<T> property)
                : base(owner)
            {
                _property = property ?? throw new ArgumentNullException(nameof(property));
            }

            public override AvaloniaProperty<T> Property => _property;
            protected override T GetValue(AvaloniaObject owner) => owner.GetValue(_property);
        }
    }
}
