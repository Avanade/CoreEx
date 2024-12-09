// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using CoreEx.Abstractions;

namespace CoreEx.Events.Subscribing
{
    /// <summary>
    /// Represents an orchestrator that leverages <see cref="EventData"/>-metadata to match a <see cref="AddSubscribers(Type[])">subscribed</see> <see cref="SubscriberBase"/> based on the corresponding
    /// <see cref="EventSubscriberAttribute"/>(s) defined enabling the processing of multiple subscribers.
    /// </summary>
    /// <remarks>Additionally, <see cref="NotSubscribedHandling"/> (and alike) can be defined to further manage the processing of events/messages, both expected and unexpected.</remarks>
    /// <param name="serviceProvider">The optional <see cref="IServiceProvider"/> (where not specified will attempt to use <see cref="ExecutionContext.GetRequiredService{T}"/>, etc).</param>
    public class EventSubscriberOrchestrator(IServiceProvider? serviceProvider = null)
    {
        private readonly List<(IEnumerable<EventSubscriberAttribute> Attributes, Type SubscriberType, Type? ValueType)> _subscribers = [];

        /// <summary>
        /// Gets all the <see cref="SubscriberBase"/> types for a given <typeparamref name="TAssembly"/> that have at least one <see cref="EventSubscriberAttribute"/>.
        /// </summary>
        /// <typeparam name="TAssembly">The <see cref="Type"/> to infer the underlying <see cref="Type.Assembly"/>.</typeparam>
        /// <param name="includeInternalTypes">Indicates whether to include internally defined types.</param>
        /// <returns>The <see cref="SubscriberBase"/> types.</returns>
        public static Type[] GetSubscribers<TAssembly>(bool includeInternalTypes = false)
            => (from type in includeInternalTypes ? typeof(TAssembly).Assembly.GetTypes() : typeof(TAssembly).Assembly.GetExportedTypes()
                where !type.IsAbstract && !type.IsGenericTypeDefinition
                let inherits = typeof(SubscriberBase).IsAssignableFrom(type)
                let attributes = type.GetCustomAttributes<EventSubscriberAttribute>(true)
                where inherits && attributes.Any()
                select type).ToArray();

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/>.
        /// </summary>
        protected IServiceProvider? ServiceProvider { get; } = serviceProvider;

        /// <summary>
        /// Gets or sets the optional <see cref="EventDataFormatter"/>.
        /// </summary>
        /// <remarks>Where not specified an instance will be requested from the <see cref="IServiceProvider"/>; otherwise, a default instance will be instantiated.</remarks>
        public EventDataFormatter? EventDataFormatter { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ErrorHandling"/> where an <i>event</i> is encountered that has not been <see cref="AddSubscribers">subscribed</see> to. Defaults to <see cref="ErrorHandling.HandleBySubscriber"/>.
        /// </summary>
        public ErrorHandling NotSubscribedHandling { get; set; } = ErrorHandling.HandleBySubscriber;

        /// <summary>
        /// Gets or sets the <see cref="ErrorHandling"/> where an <i>event</i> is encountered that has more than one <see cref="AddSubscribers">subscriber</see> (is ambiguous). Defaults to <see cref="ErrorHandling.CriticalFailFast"/>.
        /// </summary>
        public ErrorHandling AmbiquousSubscriberHandling { get; set; } = ErrorHandling.CriticalFailFast;

        /// <summary>
        /// Use (set) the <see cref="NotSubscribedHandling"/> <see cref="ErrorHandling"/> where an <i>event</i> is encountered that has not been <see cref="AddSubscribers">subscribed</see> to.
        /// </summary>
        /// <param name="notSubscribedHandling">The <see cref="ErrorHandling"/>.</param>
        /// <returns>The <see cref="EventSubscriberOrchestrator"/> to support fluent-style method-chaining.</returns>
        public EventSubscriberOrchestrator UseNotSubscribedHandling(ErrorHandling notSubscribedHandling)
        {
            NotSubscribedHandling = notSubscribedHandling;
            return this;
        }

        /// <summary>
        /// Use (set) the <see cref="AmbiquousSubscriberHandling"/> <see cref="ErrorHandling"/> where an <i>event</i> is encountered that has more than one <see cref="AddSubscribers">subscriber</see> (is ambiguous).
        /// </summary>
        /// <param name="ambiquousSubscriberHandling">The <see cref="ErrorHandling"/>.</param>
        /// <returns>The <see cref="EventSubscriberOrchestrator"/> to support fluent-style method-chaining.</returns>
        public EventSubscriberOrchestrator UseAmbiquousSubscriberHandling(ErrorHandling ambiquousSubscriberHandling)
        {
            AmbiquousSubscriberHandling = ambiquousSubscriberHandling;
            return this;
        }

        /// <summary>
        /// Adds the <typeparamref name="TSubscriber"/> <see cref="Type"/>.
        /// </summary>
        /// <typeparam name="TSubscriber">The <see cref="SubscriberBase"/> <see cref="Type"/>.</typeparam>
        /// <returns>The <see cref="EventSubscriberOrchestrator"/> to support fluent-style method-chaining.</returns>
        public EventSubscriberOrchestrator AddSubscriber<TSubscriber>() where TSubscriber : SubscriberBase => AddSubscribers(typeof(TSubscriber));

        /// <summary>
        /// Adds the subscribers; being where the <see cref="Type"/> is <see cref="SubscriberBase"/> and has at least one <see cref="EventSubscriberAttribute"/> applied.
        /// </summary>
        /// <param name="types">The <see cref="IEventSubscriber"/> types to add.</param>
        /// <returns>The <see cref="EventSubscriberOrchestrator"/> to support fluent-style method-chaining.</returns>
        public EventSubscriberOrchestrator AddSubscribers(params Type[] types)
        {
            foreach (var type in types.Distinct())
            {
                if (_subscribers.Any(x => x.SubscriberType == type))
                    continue;

                if (!TryGetEventDataValueType(type, out var valueType))
                    throw new ArgumentException($"Type '{type.FullName}' must inherit from {typeof(SubscriberBase).Name} or {typeof(SubscriberBase<>).Name}.", nameof(types));

                var atts = type.GetCustomAttributes<EventSubscriberAttribute>(true);
                if (atts != null && atts.Any())
                {
                    foreach (var att in atts)
                    {
                        if (att.ExtendedMatchMethod is not null)
                        {
                            var mi = type.GetMethod(att.ExtendedMatchMethod, BindingFlags.Public | BindingFlags.Static);
                            if (mi == null || mi.ReturnParameter.ParameterType != typeof(bool) || mi.GetParameters().Length != 2 || mi.GetParameters()[0].ParameterType != typeof(EventData) || mi.GetParameters()[1].ParameterType != typeof(EventSubscriberArgs))
                                throw new ArgumentException($"Type '{type.FullName}' has Attribute with {nameof(EventSubscriberAttribute.ExtendedMatchMethod)} of {att.ExtendedMatchMethod} that either does not exist or has an invalid method signature defined.", nameof(types));

                            att.ExtendedMatchMethodInfo = mi;
                        }
                    }

                    _subscribers.Add((atts, type, valueType));
                }
                else
                    throw new ArgumentException($"Type '{type.FullName}' must have at least one {nameof(EventSubscriberAttribute)} applied.", nameof(types));
            }

            return this;
        }

        /// <summary>
        /// Trys to determine whether inherits from <see cref="SubscriberBase"/> or <see cref="SubscriberBase{TValue}"/> and what the <see cref="EventData.Value"/> <see cref="Type"/> is where applicable.
        /// </summary>
        private static bool TryGetEventDataValueType(Type subscriberType, out Type? valueType)
        {
            Type? t = subscriberType;
            while (t != null && t != typeof(object))
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(SubscriberBase<>))
                {
                    valueType = t.GetGenericArguments()[0];
                    return true;
                }

                t = t.BaseType;
            }

            valueType = null;
            return typeof(SubscriberBase).IsAssignableFrom(subscriberType);
        }

        /// <summary>
        /// Trys to match and return a <see cref="IEventSubscriber"/> from within the registered <see cref="AddSubscriber{TSubscriber}">subscribers</see>; whilst also determining the resulting <see cref="EventData.Value"/> <see cref="Type"/>.
        /// </summary>
        /// <param name="parent">The parent (owning) <see cref="EventSubscriberBase"/>.</param>
        /// <param name="event">The actual <see cref="EventData"/>.</param>
        /// <param name="args">The optional <see cref="EventSubscriberArgs"/>.</param>
        /// <returns>The resulting match (<c>bool</c>), <see cref="IEventSubscriber"/> and <see cref="EventData.Value"/> <see cref="Type"/> where found.</returns>
        public (bool Matched, IEventSubscriber? Subscriber, Type? ValueType) TryMatchSubscriber(EventSubscriberBase parent, EventData @event, EventSubscriberArgs args)
        {
            parent.ThrowIfNull(nameof(parent));
            @event.ThrowIfNull(nameof(@event));

            if (TryMatchSubscriberInternal(@event, args, out var subscriber, out var valueType))
                return (true, subscriber, valueType);

            return (false, subscriber, valueType);
        }

        /// <summary>
        /// Try and match a subscriber.
        /// </summary>
        private bool TryMatchSubscriberInternal(EventData @event, EventSubscriberArgs args, out IEventSubscriber? subscriber, out Type? valueType)
        {
            subscriber = null;
            valueType = null;
            var eventDataFormatter = EventDataFormatter ?? ServiceProvider?.GetService<EventDataFormatter>() ?? ExecutionContext.GetService<EventDataFormatter>() ?? new EventDataFormatter();

            foreach (var item in _subscribers)
            {
                foreach (var att in item.Attributes)
                {
                    if (att.IsMatch(eventDataFormatter, @event))
                    {
                        if (subscriber != null)
                            return false;

                        if (att.ExtendedMatchMethodInfo is not null && !(bool)att.ExtendedMatchMethodInfo.Invoke(null, [@event, args])!)
                            return false;

                        subscriber = (IEventSubscriber)(ServiceProvider?.GetService(item.SubscriberType) ?? ExecutionContext.GetRequiredService(item.SubscriberType));
                        valueType = item.ValueType;
                    }
                }
            }

            return subscriber != null;
        }

        /// <summary>
        /// Receive and process the <paramref name="event"/>.
        /// </summary>
        /// <param name="parent">The parent (owning) <see cref="EventSubscriberBase"/>.</param>
        /// <param name="subscriber">The <see cref="IEventSubscriber"/> that should receive the <paramref name="event"/>.</param>
        /// <param name="event">The <see cref="EventData"/>.</param>
        /// <param name="args">The optional <see cref="EventSubscriberArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public async virtual Task ReceiveAsync(EventSubscriberBase parent, IEventSubscriber subscriber, EventData @event, EventSubscriberArgs? args = null, CancellationToken cancellationToken = default)
        {
            parent.ThrowIfNull(nameof(parent));
            subscriber.ThrowIfNull(nameof(subscriber));
            @event.ThrowIfNull(nameof(@event));

            try
            {
                await parent.EventSubscriberInvoker.InvokeAsync(parent, async (_, ct) =>
                {
                    var result = await subscriber!.ReceiveAsync(@event, args ??= [], ct);
                    result.ThrowOnError();

                    // Perform the complete/success instrumentation.
                    if (parent.WorkStateOrchestrator is not null)
                        await parent.WorkStateOrchestrator.CompleteAsync(@event.Id!, ct).ConfigureAwait(false);

                    parent.Instrumentation?.Instrument();
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (EventSubscriberException) { throw; } // This is considered as handled and should not be rethrown.
            catch (Exception ex) when (ex is IExtendedException eex)
            {
                // Handle the exception based on the subscriber configuration.
                var handling = ErrorHandler.DetermineErrorHandling(subscriber, eex);
                if (handling == ErrorHandling.HandleByHost)
                    throw;

                await parent.ErrorHandler.HandleErrorAsync(new ErrorHandlerArgs(@event.Id, new EventSubscriberException(ex.Message, ex), handling, parent.Logger) { Instrumentation = parent.Instrumentation, WorkOrchestrator = parent.WorkStateOrchestrator }, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (subscriber.UnhandledHandling != ErrorHandling.HandleByHost) // Where unhandled is none, just let the unhandled exception bubble up.
            {
                await parent.ErrorHandler.HandleErrorAsync(new ErrorHandlerArgs(@event.Id, new EventSubscriberException(ex.Message, ex), subscriber.UnhandledHandling, parent.Logger) { Instrumentation = parent.Instrumentation, WorkOrchestrator = parent.WorkStateOrchestrator }, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}