// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;

namespace CoreEx.Events
{
    /// <summary>
    /// Provides extension methods for events.
    /// </summary>
    public static class EventExtensions
    {
        /// <summary>
        /// Creates an <see cref="EventData"/> and <see cref="IEventPublisher.Publish(EventData[])">publishes</see>.
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        public static IEventPublisher PublishEvent(this IEventPublisher publisher, string? subject = null) => publisher.Publish(CreateEvent(publisher, subject, null));

        /// <summary>
        /// Creates an <see cref="EventData"/> and <see cref="IEventPublisher.Publish(EventData[])">publishes</see>.
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        public static IEventPublisher PublishEvent(this IEventPublisher publisher, string? subject, string? action = null) => publisher.Publish(CreateEvent(publisher, subject, action));

        /// <summary>
        /// Creates an <see cref="EventData"/> including the specified <paramref name="key"/> and <see cref="IEventPublisher.Publish(EventData[])">publishes</see>.
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <param name="key">The <see cref="EventDataBase.Key"/>.</param>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        public static IEventPublisher PublishEvent(this IEventPublisher publisher, string? subject, string? action, CompositeKey? key) => publisher.Publish(CreateEvent(publisher, subject, action, key));

        /// <summary>
        /// Creates an <see cref="EventData"/> including the specified <paramref name="keyArgs"/> and <see cref="IEventPublisher.Publish(EventData[])">publishes</see>.
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <param name="keyArgs">The <see cref="EventDataBase.Key"/> <see cref="CompositeKey.Args"/>.</param>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        public static IEventPublisher PublishEvent(this IEventPublisher publisher, string? subject, string? action, params object?[] keyArgs) => publisher.Publish(CreateEvent(publisher, subject, action, keyArgs));

        /// <summary>
        /// Creates an <see cref="EventData"/> and <see cref="IEventPublisher.Publish(EventData[])">publishes</see>.
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="source">The <see cref="EventDataBase.Source"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        public static IEventPublisher PublishEvent(this IEventPublisher publisher, Uri source, string? subject = null) => publisher.Publish(CreateEvent(publisher, source, subject));

        /// <summary>
        /// Creates an <see cref="EventData"/> and <see cref="IEventPublisher.Publish(EventData[])">publishes</see>.
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="source">The <see cref="EventDataBase.Source"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        public static IEventPublisher PublishEvent(this IEventPublisher publisher, Uri source, string? subject, string? action = null) => publisher.Publish(CreateEvent(publisher, source, subject, action));

        /// <summary>
        /// Creates an <see cref="EventData"/> including the specified <paramref name="key"/> and <see cref="IEventPublisher.Publish(EventData[])">publishes</see>.
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="source">The <see cref="EventDataBase.Source"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <param name="key">The <see cref="EventDataBase.Key"/>.</param>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        public static IEventPublisher PublishEvent(this IEventPublisher publisher, Uri source, string? subject, string? action, CompositeKey? key) => publisher.Publish(CreateEvent(publisher, source, subject, action, key));

        /// <summary>
        /// Creates an <see cref="EventData"/> including the specified <paramref name="keyArgs"/> and <see cref="IEventPublisher.Publish(EventData[])">publishes</see>.
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="source">The <see cref="EventDataBase.Source"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <param name="keyArgs">The <see cref="EventDataBase.Key"/> <see cref="CompositeKey.Args"/>.</param>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        public static IEventPublisher PublishEvent(this IEventPublisher publisher, Uri source, string? subject, string? action, params object?[] keyArgs) => publisher.Publish(CreateEvent(publisher, source, subject, action, keyArgs));

        /// <summary>
        /// Creates an <see cref="EventData{T}"/> and <see cref="IEventPublisher.Publish(EventData[])">publishes</see>.
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="value">The <see cref="EventData{T}.Value"/>.</param>
        /// <param name="source">The <see cref="EventDataBase.Source"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        /// <remarks>The <see cref="EventDataBase.Key"/> is automatically inferred from the <see cref="IEntityKey.EntityKey"/> where implemented.</remarks>
        public static IEventPublisher PublishValueEvent<T>(this IEventPublisher publisher, T value, Uri source, string? subject = null)
            => publisher.Publish(CreateValueEvent(publisher, value, source, subject));

        /// <summary>
        /// Creates an <see cref="EventData{T}"/> and <see cref="IEventPublisher.Publish(EventData[])">publishes</see>.
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="value">The <see cref="EventData{T}.Value"/>.</param>
        /// <param name="source">The <see cref="EventDataBase.Source"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        /// <remarks>The <see cref="EventDataBase.Key"/> is automatically inferred from the <see cref="IEntityKey.EntityKey"/> where implemented.</remarks>
        public static IEventPublisher PublishValueEvent<T>(this IEventPublisher publisher, T value, Uri source, string? subject, string? action = null)
            => publisher.Publish(CreateValueEvent(publisher, value, source, subject, action));

        /// <summary>
        /// Creates an <see cref="EventData{T}"/> including the specified <paramref name="key"/> and <see cref="IEventPublisher.Publish(EventData[])">publishes</see>.
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="value">The <see cref="EventData{T}.Value"/>.</param>
        /// <param name="source">The <see cref="EventDataBase.Source"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <param name="key">The <see cref="EventDataBase.Key"/> <see cref="CompositeKey"/>.</param>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        /// <remarks>The <see cref="EventDataBase.Key"/> is automatically inferred from the <see cref="IEntityKey.EntityKey"/> where implemented.</remarks>
        public static IEventPublisher PublishValueEvent<T>(this IEventPublisher publisher, T value, Uri source, string? subject, string? action, CompositeKey? key) 
            => publisher.Publish(CreateValueEvent(publisher, value, source, subject, action, key));

        /// <summary>
        /// Creates an <see cref="EventData{T}"/> including the specified <paramref name="keyArgs"/> and <see cref="IEventPublisher.Publish(EventData[])">publishes</see>.
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="value">The <see cref="EventData{T}.Value"/>.</param>
        /// <param name="source">The <see cref="EventDataBase.Source"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <param name="keyArgs">The <see cref="EventDataBase.Key"/> <see cref="CompositeKey.Args"/>.</param>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        /// <remarks>The <see cref="EventDataBase.Key"/> is automatically inferred from the <see cref="IEntityKey.EntityKey"/> where implemented.</remarks>
        public static IEventPublisher PublishValueEvent<T>(this IEventPublisher publisher, T value, Uri source, string? subject, string? action, params object?[] keyArgs)
            => publisher.Publish(CreateValueEvent(publisher, value, source, subject, action, keyArgs));

        /// <summary>
        /// Creates an <see cref="EventData{T}"/> and <see cref="IEventPublisher.Publish(EventData[])">publishes</see>.
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="value">The <see cref="EventData{T}.Value"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        /// <remarks>The <see cref="EventDataBase.Key"/> is automatically inferred from the <see cref="IEntityKey.EntityKey"/> where implemented.</remarks>
        public static IEventPublisher PublishValueEvent<T>(this IEventPublisher publisher, T value, string? subject = null)
            => publisher.Publish(CreateValueEvent(publisher, value, subject, null));

        /// <summary>
        /// Creates an <see cref="EventData{T}"/> and <see cref="IEventPublisher.Publish(EventData[])">publishes</see>.
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="value">The <see cref="EventData{T}.Value"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        /// <remarks>The <see cref="EventDataBase.Key"/> is automatically inferred from the <see cref="IEntityKey.EntityKey"/> where implemented.</remarks>
        public static IEventPublisher PublishValueEvent<T>(this IEventPublisher publisher, T value, string? subject, string? action = null)
            => publisher.Publish(CreateValueEvent(publisher, value, subject, action));

        /// <summary>
        /// Creates an <see cref="EventData{T}"/> including the specified <paramref name="key"/> and <see cref="IEventPublisher.Publish(EventData[])">publishes</see>.
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="value">The <see cref="EventData{T}.Value"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <param name="key">The <see cref="EventDataBase.Key"/> <see cref="CompositeKey"/>.</param>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        /// <remarks>The <see cref="EventDataBase.Key"/> is automatically inferred from the <see cref="IEntityKey.EntityKey"/> where implemented.</remarks>
        public static IEventPublisher PublishValueEvent<T>(this IEventPublisher publisher, T value, string? subject, string? action, CompositeKey? key)
            => publisher.Publish(CreateValueEvent(publisher, value, subject, action, key));

        /// <summary>
        /// Creates an <see cref="EventData{T}"/> including the specified <paramref name="keyArgs"/> and <see cref="IEventPublisher.Publish(EventData[])">publishes</see>.
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="value">The <see cref="EventData{T}.Value"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <param name="keyArgs">The <see cref="EventDataBase.Key"/> <see cref="CompositeKey.Args"/>.</param>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        /// <remarks>The <see cref="EventDataBase.Key"/> is automatically inferred from the <see cref="IEntityKey.EntityKey"/> where implemented.</remarks>
        public static IEventPublisher PublishValueEvent<T>(this IEventPublisher publisher, T value, string? subject, string? action, params object?[] keyArgs)
            => publisher.Publish(CreateValueEvent(publisher, value, subject, action, keyArgs));

        /// <summary>
        /// Creates an <see cref="EventData"/> and <see cref="IEventPublisher.PublishNamed(string, EventData[])">publishes</see> to a named destination (e.g. queue or topic).
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="name">The destination name.</param>
        /// <param name="source">The <see cref="EventDataBase.Source"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        public static IEventPublisher PublishNamedEvent(this IEventPublisher publisher, string name, Uri source, string? subject = null) => publisher.PublishNamed(name, CreateEvent(publisher, source, subject));

        /// <summary>
        /// Creates an <see cref="EventData"/> and <see cref="IEventPublisher.PublishNamed(string, EventData[])">publishes</see> to a named destination (e.g. queue or topic).
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="name">The destination name.</param>
        /// <param name="source">The <see cref="EventDataBase.Source"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        public static IEventPublisher PublishNamedEvent(this IEventPublisher publisher, string name, Uri source, string? subject, string? action = null) => publisher.PublishNamed(name, CreateEvent(publisher, source, subject, action));

        /// <summary>
        /// Creates an <see cref="EventData"/> including the specified <paramref name="key"/> and <see cref="IEventPublisher.PublishNamed(string, EventData[])">publishes</see> to a named destination (e.g. queue or topic).
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="name">The destination name.</param>
        /// <param name="source">The <see cref="EventDataBase.Source"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <param name="key">The <see cref="EventDataBase.Key"/>.</param>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        public static IEventPublisher PublishNamedEvent(this IEventPublisher publisher, string name, Uri source, string? subject, string? action, CompositeKey? key) => publisher.PublishNamed(name, CreateEvent(publisher, source, subject, action, key));

        /// <summary>
        /// Creates an <see cref="EventData"/> including the specified <paramref name="keyArgs"/> and <see cref="IEventPublisher.PublishNamed(string, EventData[])">publishes</see> to a named destination (e.g. queue or topic).
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="name">The destination name.</param>
        /// <param name="source">The <see cref="EventDataBase.Source"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <param name="keyArgs">The <see cref="EventDataBase.Key"/> <see cref="CompositeKey.Args"/>.</param>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        public static IEventPublisher PublishNamedEvent(this IEventPublisher publisher, string name, Uri source, string? subject, string? action, params object?[] keyArgs) => publisher.PublishNamed(name, CreateEvent(publisher, source, subject, action, keyArgs));

        /// <summary>
        /// Creates an <see cref="EventData"/> and <see cref="IEventPublisher.PublishNamed(string, EventData[])">publishes</see> to a named destination (e.g. queue or topic).
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="name">The destination name.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        public static IEventPublisher PublishNamedEvent(this IEventPublisher publisher, string name, string? subject = null) => publisher.PublishNamed(name, CreateEvent(publisher, subject, null));

        /// <summary>
        /// Creates an <see cref="EventData"/> and <see cref="IEventPublisher.PublishNamed(string, EventData[])">publishes</see> to a named destination (e.g. queue or topic).
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="name">The destination name.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        public static IEventPublisher PublishNamedEvent(this IEventPublisher publisher, string name, string? subject, string? action = null) => publisher.PublishNamed(name, CreateEvent(publisher, subject, action));

        /// <summary>
        /// Creates an <see cref="EventData"/> including the specified <paramref name="key"/> and <see cref="IEventPublisher.PublishNamed(string, EventData[])">publishes</see> to a named destination (e.g. queue or topic).
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="name">The destination name.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <param name="key">The <see cref="EventDataBase.Key"/>.</param>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        public static IEventPublisher PublishNamedEvent(this IEventPublisher publisher, string name, string? subject, string? action, CompositeKey? key) => publisher.PublishNamed(name, CreateEvent(publisher, subject, action, key));

        /// <summary>
        /// Creates an <see cref="EventData"/> including the specified <paramref name="keyArgs"/> and <see cref="IEventPublisher.PublishNamed(string, EventData[])">publishes</see> to a named destination (e.g. queue or topic).
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="name">The destination name.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <param name="keyArgs">The <see cref="EventDataBase.Key"/> <see cref="CompositeKey.Args"/>.</param>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        public static IEventPublisher PublishNamedEvent(this IEventPublisher publisher, string name, string? subject, string? action, params object?[] keyArgs) => publisher.PublishNamed(name, CreateEvent(publisher, subject, action, keyArgs));

        /// <summary>
        /// Creates an <see cref="EventData{T}"/> and <see cref="IEventPublisher.PublishNamed(string, EventData[])">publishes</see> to a named destination (e.g. queue or topic).
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="name">The destination name.</param>
        /// <param name="value">The <see cref="EventData{T}.Value"/>.</param>
        /// <param name="source">The <see cref="EventDataBase.Source"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        /// <remarks>The <see cref="EventDataBase.Key"/> is automatically inferred from the <see cref="IEntityKey.EntityKey"/> where implemented.</remarks>
        public static IEventPublisher PublishNamedValueEvent<T>(this IEventPublisher publisher, string name, T value, Uri source, string? subject = null) => publisher.PublishNamed(name, CreateValueEvent(publisher, value, source, subject));

        /// <summary>
        /// Creates an <see cref="EventData{T}"/> and <see cref="IEventPublisher.PublishNamed(string, EventData[])">publishes</see> to a named destination (e.g. queue or topic).
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="name">The destination name.</param>
        /// <param name="value">The <see cref="EventData{T}.Value"/>.</param>
        /// <param name="source">The <see cref="EventDataBase.Source"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        /// <remarks>The <see cref="EventDataBase.Key"/> is automatically inferred from the <see cref="IEntityKey.EntityKey"/> where implemented.</remarks>
        public static IEventPublisher PublishNamedValueEvent<T>(this IEventPublisher publisher, string name, T value, Uri source, string? subject, string? action = null) => publisher.PublishNamed(name, CreateValueEvent(publisher, value, source, subject, action));

        /// <summary>
        /// Creates an <see cref="EventData{T}"/> including the specified <paramref name="key"/> and <see cref="IEventPublisher.Publish(EventData[])">publishes</see> to a named destination (e.g. queue or topic).
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="name">The destination name.</param>
        /// <param name="value">The <see cref="EventData{T}.Value"/>.</param>
        /// <param name="source">The <see cref="EventDataBase.Source"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <param name="key">The <see cref="EventDataBase.Key"/> <see cref="CompositeKey"/>.</param>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        /// <remarks>The <see cref="EventDataBase.Key"/> is automatically inferred from the <see cref="IEntityKey.EntityKey"/> where implemented.</remarks>
        public static IEventPublisher PublishNamedValueEvent<T>(this IEventPublisher publisher, string name, T value, Uri source, string? subject, string? action, CompositeKey? key)
            => publisher.PublishNamed(name, CreateValueEvent(publisher, value, source, subject, action, key));

        /// <summary>
        /// Creates an <see cref="EventData{T}"/> including the specified <paramref name="keyArgs"/> and <see cref="IEventPublisher.Publish(EventData[])">publishes</see> to a named destination (e.g. queue or topic).
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="name">The destination name.</param>
        /// <param name="value">The <see cref="EventData{T}.Value"/>.</param>
        /// <param name="source">The <see cref="EventDataBase.Source"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <param name="keyArgs">The <see cref="EventDataBase.Key"/> <see cref="CompositeKey.Args"/>.</param>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        /// <remarks>The <see cref="EventDataBase.Key"/> is automatically inferred from the <see cref="IEntityKey.EntityKey"/> where implemented.</remarks>
        public static IEventPublisher PublishNamedValueEvent<T>(this IEventPublisher publisher, string name, T value, Uri source, string? subject, string? action, params object?[] keyArgs)
            => publisher.PublishNamed(name, CreateValueEvent(publisher, value, source, subject, action, keyArgs));

        /// <summary>
        /// Creates an <see cref="EventData{T}"/> and <see cref="IEventPublisher.PublishNamed(string, EventData[])">publishes</see> to a named destination (e.g. queue or topic).
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="name">The destination name.</param>
        /// <param name="value">The <see cref="EventData{T}.Value"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        /// <remarks>The <see cref="EventDataBase.Key"/> is automatically inferred from the <see cref="IEntityKey.EntityKey"/> where implemented.</remarks>
        public static IEventPublisher PublishNamedValueEvent<T>(this IEventPublisher publisher, string name, T value, string? subject = null) => publisher.PublishNamed(name, CreateValueEvent(publisher, value, subject, null));

        /// <summary>
        /// Creates an <see cref="EventData{T}"/> and <see cref="IEventPublisher.PublishNamed(string, EventData[])">publishes</see> to a named destination (e.g. queue or topic).
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="name">The destination name.</param>
        /// <param name="value">The <see cref="EventData{T}.Value"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        /// <remarks>The <see cref="EventDataBase.Key"/> is automatically inferred from the <see cref="IEntityKey.EntityKey"/> where implemented.</remarks>
        public static IEventPublisher PublishNamedValueEvent<T>(this IEventPublisher publisher, string name, T value, string? subject, string? action = null) => publisher.PublishNamed(name, CreateValueEvent(publisher, value, subject, action));

        /// <summary>
        /// Creates an <see cref="EventData{T}"/> including the specified <paramref name="key"/> and <see cref="IEventPublisher.Publish(EventData[])">publishes</see> to a named destination (e.g. queue or topic).
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="name">The destination name.</param>
        /// <param name="value">The <see cref="EventData{T}.Value"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <param name="key">The <see cref="EventDataBase.Key"/> <see cref="CompositeKey"/>.</param>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        /// <remarks>The <see cref="EventDataBase.Key"/> is automatically inferred from the <see cref="IEntityKey.EntityKey"/> where implemented.</remarks>
        public static IEventPublisher PublishNamedValueEvent<T>(this IEventPublisher publisher, string name, T value, string? subject, string? action, CompositeKey? key)
            => publisher.PublishNamed(name, CreateValueEvent(publisher, value, subject, action, key));

        /// <summary>
        /// Creates an <see cref="EventData{T}"/> including the specified <paramref name="keyArgs"/> and <see cref="IEventPublisher.Publish(EventData[])">publishes</see> to a named destination (e.g. queue or topic).
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="name">The destination name.</param>
        /// <param name="value">The <see cref="EventData{T}.Value"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <param name="keyArgs">The <see cref="EventDataBase.Key"/> <see cref="CompositeKey.Args"/>.</param>
        /// <returns>The <see cref="IEventPublisher"/> to support fluent-style method-chaining.</returns>
        /// <remarks>The <see cref="EventDataBase.Key"/> is automatically inferred from the <see cref="IEntityKey.EntityKey"/> where implemented.</remarks>
        public static IEventPublisher PublishNamedValueEvent<T>(this IEventPublisher publisher, string name, T value, string? subject, string? action, params object?[] keyArgs)
            => publisher.PublishNamed(name, CreateValueEvent(publisher, value, subject, action, keyArgs));

        /// <summary>
        /// Creates an <see cref="EventData"/>.
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <returns>The <see cref="EventData"/>.</returns>
        public static EventData CreateEvent(this IEventPublisher publisher, string? subject = null)
            => UpdateEventData(publisher, new() { Subject = subject }, null);

        /// <summary>
        /// Creates an <see cref="EventData"/>.
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <returns>The <see cref="EventData"/>.</returns>
        public static EventData CreateEvent(this IEventPublisher publisher, string? subject, string? action = null)
            => UpdateEventData(publisher, new() { Subject = subject, Action = action }, null);

        /// <summary>
        /// Creates an <see cref="EventData"/> including the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <param name="key">The <see cref="EventDataBase.Key"/> <see cref="CompositeKey"/>.</param>
        /// <returns>The <see cref="EventData"/>.</returns>
        public static EventData CreateEvent(this IEventPublisher publisher, string? subject, string? action, CompositeKey? key)
            => UpdateEventData(publisher, new() { Subject = subject, Action = action }, key);

        /// <summary>
        /// Creates an <see cref="EventData"/> including the specified <paramref name="keyArgs"/>.
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <param name="keyArgs">The <see cref="EventDataBase.Key"/> <see cref="CompositeKey.Args"/>.</param>
        /// <returns>The <see cref="EventData"/>.</returns>
        public static EventData CreateEvent(this IEventPublisher publisher, string? subject, string? action, params object?[] keyArgs)
            => CreateEvent(publisher, subject, action, new CompositeKey(keyArgs));

        /// <summary>
        /// Creates an <see cref="EventData"/>.
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="source">The <see cref="EventDataBase.Source"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <returns>The <see cref="EventData"/>.</returns>
        public static EventData CreateEvent(this IEventPublisher publisher, Uri source, string? subject = null)
            => UpdateEventData(publisher, new() { Source = source.ThrowIfNull(nameof(source)), Subject = subject }, null);

        /// <summary>
        /// Creates an <see cref="EventData"/>.
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="source">The <see cref="EventDataBase.Source"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <returns>The <see cref="EventData"/>.</returns>
        public static EventData CreateEvent(this IEventPublisher publisher, Uri source, string? subject, string? action = null)
            => UpdateEventData(publisher, new() { Source = source.ThrowIfNull(nameof(source)), Subject = subject, Action = action }, null);

        /// <summary>
        /// Creates an <see cref="EventData"/> including the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="source">The <see cref="EventDataBase.Source"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <param name="key">The <see cref="EventDataBase.Key"/> <see cref="CompositeKey"/>.</param>
        /// <returns>The <see cref="EventData"/>.</returns>
        public static EventData CreateEvent(this IEventPublisher publisher, Uri source, string? subject, string? action, CompositeKey? key)
            => UpdateEventData(publisher, new() { Source = source.ThrowIfNull(nameof(source)), Subject = subject, Action = action }, key);

        /// <summary>
        /// Creates an <see cref="EventData"/> including the specified <paramref name="keyArgs"/>.
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="source">The <see cref="EventDataBase.Source"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <param name="keyArgs">The <see cref="EventDataBase.Key"/> <see cref="CompositeKey.Args"/>.</param>
        /// <returns>The <see cref="EventData"/>.</returns>
        public static EventData CreateEvent(this IEventPublisher publisher, Uri source, string? subject, string? action, params object?[] keyArgs)
            => CreateEvent(publisher, source, subject, action, new CompositeKey(keyArgs));

        /// <summary>
        /// Creates an <see cref="EventData{T}"/>.
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="value">The <see cref="EventData{T}.Value"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <returns>The <see cref="EventData{T}"/>.</returns>
        /// <remarks>The <see cref="EventDataBase.Key"/> is automatically inferred from the <see cref="IEntityKey.EntityKey"/> where implemented.</remarks>
        public static EventData<T> CreateValueEvent<T>(this IEventPublisher publisher, T value, string? subject = null)
            => (EventData<T>)UpdateEventData(publisher, new EventData<T>() { Value = value.ThrowIfNull(nameof(value)), Subject = subject }, value is IEntityKey ek ? ek.EntityKey : null);

        /// <summary>
        /// Creates an <see cref="EventData{T}"/>.
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="value">The <see cref="EventData{T}.Value"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <returns>The <see cref="EventData{T}"/>.</returns>
        /// <remarks>The <see cref="EventDataBase.Key"/> is automatically inferred from the <see cref="IEntityKey.EntityKey"/> where implemented.</remarks>
        public static EventData<T> CreateValueEvent<T>(this IEventPublisher publisher, T value, string? subject, string? action = null)
            => (EventData<T>)UpdateEventData(publisher, new EventData<T>() { Value = value.ThrowIfNull(nameof(value)), Subject = subject, Action = action }, value is IEntityKey ek ? ek.EntityKey : null);

        /// <summary>
        /// Creates an <see cref="EventData{T}"/> including the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="value">The <see cref="EventData{T}.Value"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <param name="key">The <see cref="EventDataBase.Key"/> <see cref="CompositeKey"/>.</param>
        /// <returns>The <see cref="EventData{T}"/>.</returns>
        /// <remarks>The <see cref="EventDataBase.Key"/> is automatically inferred from the <see cref="IEntityKey.EntityKey"/> where implemented.</remarks>
        public static EventData<T> CreateValueEvent<T>(this IEventPublisher publisher, T value, string? subject, string? action, CompositeKey key)
            => (EventData<T>)UpdateEventData(publisher, new EventData<T>() { Value = value.ThrowIfNull(nameof(value)), Subject = subject, Action = action }, key);

        /// <summary>
        /// Creates an <see cref="EventData{T}"/> including the specified <paramref name="keyArgs"/>.
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="value">The <see cref="EventData{T}.Value"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <param name="keyArgs">The <see cref="EventDataBase.Key"/> <see cref="CompositeKey.Args"/>.</param>
        /// <returns>The <see cref="EventData{T}"/>.</returns>
        /// <remarks>The <see cref="EventDataBase.Key"/> is automatically inferred from the <see cref="IEntityKey.EntityKey"/> where implemented.</remarks>
        public static EventData<T> CreateValueEvent<T>(this IEventPublisher publisher, T value, string? subject, string? action, params object?[] keyArgs)
            => (EventData<T>)UpdateEventData(publisher, new EventData<T>() { Value = value.ThrowIfNull(nameof(value)), Subject = subject, Action = action }, keyArgs.Length == 1 && keyArgs[0] is CompositeKey ck ? ck : new CompositeKey(keyArgs));

        /// <summary>
        /// Creates an <see cref="EventData{T}"/>.
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="value">The <see cref="EventData{T}.Value"/>.</param>
        /// <param name="source">The <see cref="EventDataBase.Source"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <returns>The <see cref="EventData{T}"/>.</returns>
        /// <remarks>The <see cref="EventDataBase.Key"/> is automatically inferred from the <see cref="IEntityKey.EntityKey"/> where implemented.</remarks>
        public static EventData<T> CreateValueEvent<T>(this IEventPublisher publisher, T value, Uri source, string? subject = null)
            => (EventData<T>)UpdateEventData(publisher, new EventData<T>() { Value = value.ThrowIfNull(nameof(value)), Source = source.ThrowIfNull(nameof(source)), Subject = subject }, value is IEntityKey ek ? ek.EntityKey : null);

        /// <summary>
        /// Creates an <see cref="EventData{T}"/>.
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="value">The <see cref="EventData{T}.Value"/>.</param>
        /// <param name="source">The <see cref="EventDataBase.Source"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <returns>The <see cref="EventData{T}"/>.</returns>
        /// <remarks>The <see cref="EventDataBase.Key"/> is automatically inferred from the <see cref="IEntityKey.EntityKey"/> where implemented.</remarks>
        public static EventData<T> CreateValueEvent<T>(this IEventPublisher publisher, T value, Uri source, string? subject, string? action = null)
            => (EventData<T>)UpdateEventData(publisher, new EventData<T>() { Value = value.ThrowIfNull(nameof(value)), Source = source.ThrowIfNull(nameof(source)), Subject = subject, Action = action }, value is IEntityKey ek ? ek.EntityKey : null);

        /// <summary>
        /// Creates an <see cref="EventData{T}"/> including the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="value">The <see cref="EventData{T}.Value"/>.</param>
        /// <param name="source">The <see cref="EventDataBase.Source"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <param name="key">The <see cref="EventDataBase.Key"/> <see cref="CompositeKey"/>.</param>
        /// <returns>The <see cref="EventData{T}"/>.</returns>
        /// <remarks>The <see cref="EventDataBase.Key"/> is automatically inferred from the <see cref="IEntityKey.EntityKey"/> where implemented.</remarks>
        public static EventData<T> CreateValueEvent<T>(this IEventPublisher publisher, T value, Uri source, string? subject, string? action, CompositeKey key)
            => (EventData<T>)UpdateEventData(publisher, new EventData<T>() { Value = value.ThrowIfNull(nameof(value)), Source = source.ThrowIfNull(nameof(source)), Subject = subject, Action = action }, key);

        /// <summary>
        /// Creates an <see cref="EventData{T}"/> including the specified <paramref name="keyArgs"/>.
        /// </summary>
        /// <param name="publisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="value">The <see cref="EventData{T}.Value"/>.</param>
        /// <param name="source">The <see cref="EventDataBase.Source"/>.</param>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/>.</param>
        /// <param name="action">The <see cref="EventDataBase.Action"/>.</param>
        /// <param name="keyArgs">The <see cref="EventDataBase.Key"/> <see cref="CompositeKey.Args"/>.</param>
        /// <returns>The <see cref="EventData{T}"/>.</returns>
        /// <remarks>The <see cref="EventDataBase.Key"/> is automatically inferred from the <see cref="IEntityKey.EntityKey"/> where implemented.</remarks>
        public static EventData<T> CreateValueEvent<T>(this IEventPublisher publisher, T value, Uri source, string? subject, string? action, params object?[] keyArgs)
            => (EventData<T>)UpdateEventData(publisher, new EventData<T>() { Value = value.ThrowIfNull(nameof(value)), Source = source.ThrowIfNull(nameof(source)), Subject = subject, Action = action }, keyArgs.Length == 1 && keyArgs[0] is CompositeKey ck ? ck : new CompositeKey(keyArgs));

        /// <summary>
        /// Adds the formatted <paramref name="key"/> to the <paramref name="event"/> and stores the underlying <see cref="CompositeKey.Args"/> within <see cref="EventDataBase.Internal"/>.  
        /// </summary>
        private static EventData UpdateEventData(IEventPublisher publisher, EventData @event, CompositeKey? key)
        {
            if (key is not null)
            {
                @event.Key = key.Value.ToString(publisher.EventDataFormatter.KeySeparatorCharacter);
                @event.Internal.Add(nameof(EventDataBase.Key), key.Value.Args);
            }

            return @event;
        }
    }
}