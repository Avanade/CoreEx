// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.ComponentModel;

namespace CoreEx.Entities.Extended
{
    /// <summary>
    /// Provides data for the <see cref="INotifyCollectionItemChanged.CollectionItemChanged"/> when an <i>item</i> within an collection is changed.
    /// </summary>
    public class CollectionItemChangedEventArgs : PropertyChangedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionItemChangedEventArgs"/> class.
        /// </summary>
        /// <param name="item">The item that had the property change.</param>
        /// <param name="propertyName">The name of the property that changed.</param>
        public CollectionItemChangedEventArgs(object? item, string? propertyName) : base(propertyName) => Item = item;

        /// <summary>
        /// Gets the item that had the property change.
        /// </summary>
        public object? Item { get; }
    }
}