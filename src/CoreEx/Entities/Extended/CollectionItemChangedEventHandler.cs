// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Entities.Extended
{
    /// <summary>
    /// Represents the method that will handle the <see cref="INotifyCollectionItemChanged.CollectionItemChanged"/> event raised when a property is changed on a collection item.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="CollectionItemChangedEventArgs"/>.</param>
    public delegate void CollectionItemChangedEventHandler(object sender, CollectionItemChangedEventArgs e);
}