using Avalonia.Data;

#nullable enable

namespace Avalonia
{
    /// <summary>
    /// Provides information for an Avalonia property change.
    /// </summary>
    public class AvaloniaPropertyChangedEventArgs<T> : AvaloniaPropertyChangedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaPropertyChangedEventArgs"/> class.
        /// </summary>
        /// <param name="sender">The object that the property changed on.</param>
        /// <param name="property">The property that changed.</param>
        /// <param name="oldValue">The old value of the property.</param>
        /// <param name="newValue">The new value of the property.</param>
        /// <param name="priority">The priority of the binding that produced the value.</param>
        /// <param name="isActiveValueChange">
        /// Whether the change represents a change to the active value.
        /// </param>
        /// <param name="isOutdated">Whether the value is outdated.</param>
        public AvaloniaPropertyChangedEventArgs(
            IAvaloniaObject sender,
            AvaloniaProperty<T> property,
            Optional<T> oldValue,
            BindingValue<T> newValue,
            BindingPriority priority,
            bool isActiveValueChange = true,
            bool isOutdated = false)
            : base(sender, priority)
        {
            Property = property;
            OldValue = oldValue;
            NewValue = newValue;
            IsActiveValueChange = isActiveValueChange;
            IsOutdated = isOutdated;
        }

        /// <summary>
        /// Gets the property that changed.
        /// </summary>
        /// <value>
        /// The property that changed.
        /// </value>
        public new AvaloniaProperty<T> Property { get; }

        /// <summary>
        /// Gets the old value of the property.
        /// </summary>
        /// <value>
        /// The old value of the property.
        /// </value>
        public new Optional<T> OldValue { get; private set; }

        /// <summary>
        /// Gets the new value of the property.
        /// </summary>
        /// <value>
        /// The new value of the property.
        /// </value>
        public new BindingValue<T> NewValue { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the change represents a change to the active value of
        /// the property.
        /// </summary>
        /// <remarks>
        /// If the Listen call requested to not include animation changes then <see cref="NewValue"/>
        /// may not represent a change to the active value of the property on the object.
        /// </remarks>
        public bool IsActiveValueChange { get; }

        /// <summary>
        /// Gets a value indicating whether the value of the property on the object has already
        /// changed since this change began notifying.
        /// </summary>
        public bool IsOutdated { get; private set; }

        internal void MarkOutdated() => IsOutdated = true;

        protected override AvaloniaProperty GetProperty() => Property;

        protected override object? GetOldValue() => OldValue.GetValueOrDefault(AvaloniaProperty.UnsetValue);

        protected override object? GetNewValue() => NewValue.GetValueOrDefault(AvaloniaProperty.UnsetValue);
    }
}
