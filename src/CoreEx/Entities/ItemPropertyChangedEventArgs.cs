// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.ComponentModel;

namespace CoreEx.Entities
{
    /// <summary>
    /// Provides data for the <see cref="INotifyPropertyChanged.PropertyChanged"/> when an <i>item</i> within an <see cref="IEntityBaseCollection"/> is changed.
    /// </summary>
    public class ItemPropertyChangedEventArgs : PropertyChangedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemPropertyChangedEventArgs"/> class.
        /// </summary>
        /// <param name="item">The item that had the property change.</param>
        /// <param name="propertyName">The name of the property that changed.</param>
        public ItemPropertyChangedEventArgs(object item, string propertyName) : base(propertyName) => Item = item;

        /// <summary>
        /// Gets the item that had the property change.
        /// </summary>
        public object Item { get; }
    }
}