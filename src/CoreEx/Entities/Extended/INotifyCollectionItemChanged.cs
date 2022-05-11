// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Entities.Extended
{
    /// <summary>
    /// Notifies consumers when an underlying collection item changes.
    /// </summary>
    public interface INotifyCollectionItemChanged
    {
        /// <summary>
        /// Occurs when the contents for a collection item is changed.
        /// </summary>
        event CollectionItemChangedEventHandler CollectionItemChanged;
    }
}