// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.ComponentModel;

namespace CoreEx.Entities.Extended
{
    /// <summary>
    /// Provides data for the <see cref="INotifyCollectionItemChanged.CollectionItemChanged"/> when an <i>item</i> within an collection is changed.
    /// </summary>
    /// <param name="item">The item that had the property change.</param>
    /// <param name="propertyName">The name of the property that changed.</param>
    public class CollectionItemChangedEventArgs(object? item, string? propertyName) : PropertyChangedEventArgs(propertyName)
    {
        /// <summary>
        /// Gets the item that had the property change.
        /// </summary>
        public object? Item { get; } = item;
    }
}