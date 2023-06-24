// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Collections.Generic;

namespace CoreEx.Events.Subscribing
{
    /// <summary>
    /// Provides the <see cref="IEventSubscriber"/> arguments; is obstensibly a <see cref="Dictionary{TKey, TValue}"/> with a <see cref="string"/> key and <see cref="object"/> value.
    /// </summary>
    public class EventSubscriberArgs : Dictionary<string, object?> { }
}