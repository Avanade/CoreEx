// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Entities;
using CoreEx.Events;
using CoreEx.Http;
using CoreEx.Wildcards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using UnitTestEx.Abstractions;
using UnitTestEx.AspNetCore;

namespace UnitTestEx.Expectations
{
    /// <summary>
    /// Provides extension methods to the core <see href="https://github.com/Avanade/unittestex"/>.
    /// </summary>
    public static class UnitTestExExtensions
    {
        #region Tester

        private const string TesterBaseIsExpectedEventPublisherConfiguredKey = nameof(TesterBase) + "_" + nameof(IsExpectedEventPublisherConfigured);

        /// <summary>
        /// Replaces the <see cref="IEventPublisher"/> with the <see cref="ExpectedEventPublisher"/> to enable the <see cref="EventExpectations{TTester}"/>.
        /// </summary>
        /// <param name="tester">The <see cref="TesterBase"/>.</param>
        /// <returns>This will automatically be invoked where the <see cref="TestSetUp"/> <see cref="EnableExpectedEvents(TestSetUp, bool)"/> has been executed.</returns>
        internal static void UseExpectedEvents(this TesterBase tester)
        {
            var usingExpectedEvents = tester.SetUp.Properties.TryGetValue(TesterBaseIsExpectedEventPublisherConfiguredKey, out var val) && (bool)val!;

            if (!usingExpectedEvents)
            {
                tester.ConfigureServices(sc => sc.ReplaceScoped<IEventPublisher, ExpectedEventPublisher>());
                tester.SetUp.Properties[TesterBaseIsExpectedEventPublisherConfiguredKey] = true;
            }
        }

        /// <summary>
        /// Replaces the <see cref="IEventPublisher"/> with the <see cref="ExpectedEventPublisher"/> to enable the <see cref="EventExpectations{TTester}"/>.
        /// </summary>
        /// <typeparam name="TSelf">The <typeparamref name="TSelf"/> to support fluent-style method-chaining.</typeparam>
        /// <param name="tester">The <see cref="TesterBase"/>.</param>
        /// <returns>This will automatically be invoked where the <see cref="TestSetUp"/> <see cref="EnableExpectedEvents(TestSetUp, bool)"/> has been executed.</returns>
        public static TSelf UseExpectedEvents<TSelf>(this TesterBase<TSelf> tester) where TSelf : TesterBase<TSelf>
        {
            ((TesterBase)tester).UseExpectedEvents();
            return (TSelf)tester;
        }

        /// <summary>
        /// Gets whether the <see cref="UseExpectedEvents{TSelf}(TesterBase{TSelf})"/> has been invoked
        /// </summary>
        /// <param name="owner"></param>
        /// <returns></returns>
        public static bool IsExpectedEventPublisherConfigured(this TesterBase owner) => owner.SetUp.Properties.TryGetValue(TesterBaseIsExpectedEventPublisherConfiguredKey, out var val) && (bool)val!;

        #endregion

        #region TestSetUp

        private const string ExpectedEventsPathsToIgnoreKey = nameof(ExpectedEventPublisher) + "_" + "PathsToIgnore";
        private const string ExpectedEventsWildcardKey = nameof(ExpectedEventPublisher) + "_" + nameof(Wildcard);
        private const string ExpectedEventsFormatterKey = nameof(ExpectedEventPublisher) + "_" + nameof(EventDataFormatter);
        private const string ExpectedEventsEnabledKey = nameof(ExpectedEventPublisher) + "_" + nameof(EnableExpectedEvents);
        private const string ExpectedNoEventsKey = nameof(ExpectedEventPublisher) + "_" + nameof(ExpectNoEvents);

        /// <summary>
        /// Gets the <see cref="ExpectedEventPublisher"/> JSON paths to ignore from the <see cref="TestSetUp.Properties"/>.
        /// </summary>
        /// <param name="setUp">The <see cref="TestSetUp"/>.</param>
        /// <returns>The <see cref="ExpectedEventPublisher"/> JSON paths to ignore.</returns>
        /// <remarks>By default <see cref="EventDataBase.Id"/>, <see cref="EventDataBase.CorrelationId"/>, <see cref="EventDataBase.Timestamp"/>, <see cref="EventDataBase.ETag"/> and <see cref="EventDataBase.Key"/> are ignored.</remarks>
        public static List<string> GetExpectedEventsPathsToIgnore(this TestSetUp setUp)
        {
            if (setUp.Properties.TryGetValue(ExpectedEventsPathsToIgnoreKey, out var pathsToIgnore))
                return (List<string>)pathsToIgnore!;

            var pti = new List<string>() { nameof(EventDataBase.Id), nameof(EventDataBase.CorrelationId), nameof(EventDataBase.Timestamp), nameof(EventDataBase.ETag), nameof(EventDataBase.Key) };
            setUp.Properties.TryAdd(ExpectedEventsPathsToIgnoreKey, pti);
            return pti;
        }

        /// <summary>
        /// Gets the <see cref="ExpectedEventPublisher"/> <see cref="Wildcard"/> parser from the <see cref="TestSetUp.Properties"/>.
        /// </summary>
        /// <param name="setUp">The <see cref="TestSetUp"/>.</param>
        /// <returns>The <see cref="Wildcard"/> parser.</returns>
        public static Wildcard GetExpectedEventsWildcard(this TestSetUp setUp)
            => setUp.Properties.TryGetValue(ExpectedEventsWildcardKey, out var wildcard) ? (Wildcard)wildcard! : SetExpectedEventsWildcard(setUp, Wildcard.MultiAll);

        /// <summary>
        /// Sets the <see cref="ExpectedEventPublisher"/> <see cref="Wildcard"/> parser into the <see cref="TestSetUp.Properties"/>.
        /// </summary>
        /// <param name="setUp">The <see cref="TestSetUp"/>.</param>
        /// <param name="wildcard">The <see cref="Wildcard"/> parser.</param>
        public static Wildcard SetExpectedEventsWildcard(this TestSetUp setUp, Wildcard wildcard)
        {
            setUp.Properties[ExpectedEventsWildcardKey] = wildcard;
            return wildcard;
        }

        /// <summary>
        /// Gets the <see cref="ExpectedEventPublisher"/> <see cref="EventDataFormatter"/> from the <see cref="TestSetUp.Properties"/>.
        /// </summary>
        /// <param name="setUp">The <see cref="TestSetUp"/>.</param>
        /// <returns>The <see cref="Wildcard"/> parser.</returns>
        public static EventDataFormatter GetExpectedEventsFormatter(this TestSetUp setUp)
            => setUp.Properties.TryGetValue(ExpectedEventsFormatterKey, out var formatter) ? (EventDataFormatter)formatter! : SetExpectedEventsFormatter(setUp, new EventDataFormatter());

        /// <summary>
        /// Sets the <see cref="ExpectedEventPublisher"/> <see cref="EventDataFormatter"/> into the <see cref="TestSetUp.Properties"/>.
        /// </summary>
        /// <param name="setUp">The <see cref="TestSetUp"/>.</param>
        /// <param name="formatter">The <see cref="Wildcard"/> parser.</param>
        public static EventDataFormatter SetExpectedEventsFormatter(this TestSetUp setUp, EventDataFormatter formatter)
        {
            setUp.Properties[ExpectedEventsFormatterKey] = formatter;
            return formatter;
        }

        /// <summary>
        /// Sets whether the <see cref="EventExpectations{TTester}"/> functionality is enabled.
        /// </summary>
        /// <param name="setup">The <see cref="TestSetUp"/>.</param>
        /// <param name="enabled">The enabled option.</param>
        /// <remarks>Where enabled the <see cref="IEventSender"/> will be automatically replaced by the <see cref="Expectations.ExpectedEventPublisher"/> that is used by the <see cref="EventExpectations{TTester}"/> to verify that the
        /// expected events were sent. Therefore, the events will <b>not</b> be sent to any external eventing/messaging system as a result.</remarks>
        public static void EnableExpectedEvents(this TestSetUp setup, bool enabled = true) => setup.Properties[ExpectedEventsEnabledKey] = enabled;

        /// <summary>
        /// Indicates whether the <b>ExpectedEvents</b> functionality is enabled (see <see cref="EnableExpectedEvents"/>). Defaults to <c>false</c>.
        /// </summary>
        /// <param name="setup">The <see cref="TestSetUp"/>.</param>
        public static bool IsExpectedEventsEnabled(this TestSetUp setup) => setup.Properties.TryGetValue(ExpectedEventsEnabledKey, out var enabled) && (bool)enabled!;

        /// <summary>
        /// Sets whether to verify that <b>no</b> events are published as the default behaviour. Defaults to <c>true</c>.
        /// </summary>
        /// <param name="setup">The <see cref="TestSetUp"/>.</param>
        /// <param name="expectNoEvents">The expectation option.</param>
        public static void ExpectNoEvents(this TestSetUp setup, bool expectNoEvents = true) => setup.Properties[ExpectedNoEventsKey] = expectNoEvents;

        /// <summary>
        /// Indicates whether to verify that <b>no</b> events are published as the default behaviour. Defaults to <c>true</c>.
        /// </summary>
        /// <param name="setup">The <see cref="TestSetUp"/>.</param>
        public static bool IsExpectingNoEvents(this TestSetUp setup) => setup.Properties.TryGetValue(ExpectedNoEventsKey, out var expectNoEvents) && (bool)expectNoEvents!;

        #endregion   

        #region EventExpectations

        /// <summary>
        /// Invokes the set expectation logic.
        /// </summary>
        private static TSelf SetEventExpectation<TSelf>(this IExpectations<TSelf> tester, Action<EventExpectations<TSelf>> action) where TSelf : IExpectations<TSelf>
        {
            var ee = tester.ExpectationsArranger.GetOrAdd(() => new EventExpectations<TSelf>(tester.ExpectationsArranger.Owner, (TSelf)tester));
            action(ee);
            return (TSelf)tester;
        }

        /// <summary>
        /// Expects that no events have been published. 
        /// </summary>
        /// <param name="tester">The <see cref="IExpectations{TSelf}"/>.</param>
        /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
        /// <remarks>On first invocation will automatically replace <see cref="IEventPublisher"/> with a new <see cref="InMemoryPublisher"/> scoped service (DI) to capture events for this expectation. The other services are therefore required
        /// for this to function. As this is a scoped service no parallel execution of services against the same test host is supported as this capability is not considered thread-safe.</remarks>
        public static TSelf ExpectNoEvents<TSelf>(this IExpectations<TSelf> tester) where TSelf : IExpectations<TSelf>
            => tester.SetEventExpectation(e => e.ExpectNoEvents());

        /// <summary>
        /// Expects that at least one event has been published. 
        /// </summary>
        /// <param name="tester">The <see cref="IExpectations{TSelf}"/>.</param>
        /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
        /// <remarks>On first invocation will automatically replace <see cref="IEventPublisher"/> with a new <see cref="InMemoryPublisher"/> scoped service (DI) to capture events for this expectation. The other services are therefore required
        /// for this to function. As this is a scoped service no parallel execution of services against the same test host is supported as this capability is not considered thread-safe.</remarks>
        public static TSelf ExpectEvents<TSelf>(this IExpectations<TSelf> tester) where TSelf : IExpectations<TSelf>
            => tester.SetEventExpectation(e => e.ExpectEvents());

        /// <summary>
        /// Expects that the corresponding event has been published (in order specified). The expected event <paramref name="subject"/> and <paramref name="action"/> can use wildcards. All other <see cref="EventData"/> properties are not matched/verified.
        /// </summary>
        /// <param name="tester">The <see cref="IExpectations{TSelf}"/>.</param>
        /// <param name="subject">The expected subject (may contain wildcards).</param>
        /// <param name="action">The expected action (may contain wildcards).</param>
        /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
        /// <remarks>On first invocation will automatically replace <see cref="IEventPublisher"/> with a new <see cref="InMemoryPublisher"/> scoped service (DI) to capture events for this expectation. The other services are therefore required
        /// for this to function. As this is a scoped service no parallel execution of services against the same test host is supported as this capability is not considered thread-safe.</remarks>
        public static TSelf ExpectEvent<TSelf>(this IExpectations<TSelf> tester, string subject, string? action = "*") where TSelf : IExpectations<TSelf>
            => tester.SetEventExpectation(e => e.Expect(null, subject, action));

        /// <summary>
        /// Expects that the corresponding event <paramref name="value"/> has been published (in order specified). The expected event <paramref name="subject"/> and <paramref name="action"/> can use wildcards.
        /// </summary>
        /// <param name="tester">The <see cref="IExpectations{TSelf}"/>.</param>
        /// <param name="value">The expected <see cref="EventData.Value"/>.</param>
        /// <param name="subject">The expected subject (may contain wildcards).</param>
        /// <param name="action">The expected action (may contain wildcards).</param>
        /// <param name="pathsToIgnore">The paths to ignore from the comparison. Defaults to <see cref="UnitTestExExtensions.GetExpectedEventsPathsToIgnore"/>.</param>
        /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
        public static TSelf ExpectEventValue<TSelf>(this IExpectations<TSelf> tester, object? value, string subject, string? action, params string[] pathsToIgnore) where TSelf : IExpectations<TSelf>
            => tester.SetEventExpectation(e => e.Expect(null, new EventData { Subject = subject, Action = action, Value = value }, pathsToIgnore));

        /// <summary>
        /// Expects that the corresponding event has been published (in order specified). The expected event <paramref name="source"/>, <paramref name="subject"/> and <paramref name="action"/> can use wildcards. All other <see cref="EventData"/> 
        /// properties are not matched/verified.
        /// </summary>
        /// <param name="tester">The <see cref="IExpectations{TSelf}"/>.</param>
        /// <param name="source">The expected source formatted as a <see cref="Uri"/> (may contain wildcards).</param>
        /// <param name="subject">The expected subject (may contain wildcards).</param>
        /// <param name="action">The expected action (may contain wildcards).</param>
        /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
        /// <remarks>On first invocation will automatically replace <see cref="IEventPublisher"/> with a new <see cref="InMemoryPublisher"/> scoped service (DI) to capture events for this expectation. The other services are therefore required
        /// for this to function. As this is a scoped service no parallel execution of services against the same test host is supported as this capability is not considered thread-safe.</remarks>
        public static TSelf ExpectEvent<TSelf>(this IExpectations<TSelf> tester, string source, string subject, string? action = "*") where TSelf : IExpectations<TSelf>
            => tester.SetEventExpectation(e => e.Expect(null, source, subject, action));

        /// <summary>
        /// Expects that the corresponding <paramref name="event"/> has been published (in order specified). All properties for expected event will be compared again the actual.
        /// </summary>
        /// <param name="tester">The <see cref="IExpectations{TSelf}"/>.</param>
        /// <param name="event">The expected <paramref name="event"/>. Wildcards are supported for <see cref="EventDataBase.Subject"/> and <see cref="EventDataBase.Action"/>.</param>
        /// <param name="pathsToIgnore">The paths to ignore from the comparison. Defaults to <see cref="GetExpectedEventsPathsToIgnore"/>.</param>
        /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
        /// <remarks>Wildcards are supported for <see cref="EventDataBase.Subject"/>, <see cref="EventDataBase.Action"/> and <see cref="EventDataBase.Type"/>.</remarks>
        public static TSelf ExpectEvent<TSelf>(this IExpectations<TSelf> tester, EventData @event, params string[] pathsToIgnore) where TSelf : IExpectations<TSelf>
            => tester.SetEventExpectation(e => e.Expect(null, @event, pathsToIgnore));

        /// <summary>
        /// Expects that the corresponding <paramref name="event"/> has been published (in order specified). All properties for expected event will be compared again the actual.
        /// </summary>
        /// <param name="tester">The <see cref="IExpectations{TSelf}"/>.</param>
        /// <param name="source">The expected source formatted as a <see cref="Uri"/> (may contain wildcards).</param>
        /// <param name="event">The expected <paramref name="event"/>. Wildcards are supported for <see cref="EventDataBase.Subject"/> and <see cref="EventDataBase.Action"/>.</param>
        /// <param name="pathsToIgnore">The paths to ignore from the comparison. Defaults to <see cref="GetExpectedEventsPathsToIgnore"/>.</param>
        /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
        /// <remarks>Wildcards are supported for <see cref="EventDataBase.Subject"/>, <see cref="EventDataBase.Action"/> and <see cref="EventDataBase.Type"/>.</remarks>
        public static TSelf ExpectEvent<TSelf>(this IExpectations<TSelf> tester, string source, EventData @event, params string[] pathsToIgnore) where TSelf : IExpectations<TSelf>
            => tester.SetEventExpectation(e => e.Expect(null, source, @event, pathsToIgnore));

        /// <summary>
        /// Expects that the JSON serialized <see cref="EventData"/> from the named embedded resource has been published (in order specified). All properties for expected event will be compared again the actual.
        /// </summary>
        /// <param name="tester">The <see cref="IExpectations{TSelf}"/>.</param>
        /// <param name="resourceName">The embedded resource name (matches to the end of the fully qualifed resource name) that contains the expected value as serialized JSON.</param>
        /// <param name="pathsToIgnore">The paths to ignore from the comparison.</param>
        /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
        public static TSelf ExpectEventFromJsonResource<TSelf>(this IExpectations<TSelf> tester, string resourceName, params string[] pathsToIgnore) where TSelf : IExpectations<TSelf>
            => ExpectEventFromJsonResource(tester, resourceName, Assembly.GetCallingAssembly(), pathsToIgnore);

        /// <summary>
        /// Expects that the JSON serialized <see cref="EventData"/> from the named embedded resource has been published (in order specified). All properties for expected event will be compared again the actual.
        /// </summary>
        /// <param name="tester">The <see cref="IExpectations{TSelf}"/>.</param>
        /// <param name="resourceName">The embedded resource name (matches to the end of the fully qualifed resource name) that contains the expected value as serialized JSON.</param>
        /// <param name="assembly">The <see cref="Assembly"/> that contains the embedded resource.</param>
        /// <param name="pathsToIgnore">The paths to ignore from the comparison.</param>
        /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
        public static TSelf ExpectEventFromJsonResource<TSelf>(this IExpectations<TSelf> tester, string resourceName, Assembly assembly, params string[] pathsToIgnore) where TSelf : IExpectations<TSelf>
            => tester.SetEventExpectation(e => e.Expect(null, Resource.GetJsonValue<EventData>(resourceName, assembly ?? Assembly.GetCallingAssembly(), e.Owner.JsonSerializer), pathsToIgnore));

        /// <summary>
        /// Expects that the corresponding <paramref name="destination"/> event has been published (in order specified). The expected event <paramref name="subject"/> and <paramref name="action"/> can use wildcards. All other <see cref="EventData"/> properties are not matched/verified.
        /// </summary>
        /// <param name="tester">The <see cref="HttpTesterBase{TSelf}"/>.</param>
        /// <param name="destination">The named destination (e.g. queue or topic).</param>
        /// <param name="subject">The expected subject (may contain wildcards).</param>
        /// <param name="action">The expected action (may contain wildcards).</param>
        /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
        /// <remarks>On first invocation will automatically replace <see cref="IEventPublisher"/> with a new <see cref="InMemoryPublisher"/> scoped service (DI) to capture events for this expectation. The other services are therefore required
        /// for this to function. As this is a scoped service no parallel execution of services against the same test host is supported as this capability is not considered thread-safe.</remarks>
        public static TSelf ExpectDestinationEvent<TSelf>(this IExpectations<TSelf> tester, string destination, string subject, string? action = "*") where TSelf : IExpectations<TSelf>
            => tester.SetEventExpectation(e => e.Expect(destination, subject, action));

        /// <summary>
        /// Expects that the corresponding <paramref name="destination"/> event <paramref name="value"/> has been published (in order specified). The expected event <paramref name="subject"/> and <paramref name="action"/> can use wildcards.
        /// </summary>
        /// <param name="tester">The <see cref="IExpectations{TSelf}"/>.</param>
        /// <param name="destination">The named destination (e.g. queue or topic).</param>
        /// <param name="value">The expected <see cref="EventData.Value"/>.</param>
        /// <param name="subject">The expected subject (may contain wildcards).</param>
        /// <param name="action">The expected action (may contain wildcards).</param>
        /// <param name="pathsToIgnore">The paths to ignore from the comparison. Defaults to <see cref="GetExpectedEventsPathsToIgnore"/>.</param>
        /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
        public static TSelf ExpectDestinationEventValue<TSelf>(this IExpectations<TSelf> tester, object? value, string destination, string subject, string? action, params string[] pathsToIgnore) where TSelf : IExpectations<TSelf>
            => tester.SetEventExpectation(e => e.Expect(destination, new EventData { Subject = subject, Action = action, Value = value }, pathsToIgnore));

        /// <summary>
        /// Expects that the corresponding <paramref name="destination"/> event has been published (in order specified). The expected event <paramref name="source"/>, <paramref name="subject"/> and <paramref name="action"/> can use wildcards. All other <see cref="EventData"/> 
        /// properties are not matched/verified.
        /// </summary>
        /// <param name="tester">The <see cref="IExpectations{TSelf}"/>.</param>
        /// <param name="destination">The named destination (e.g. queue or topic).</param>
        /// <param name="source">The expected source formatted as a <see cref="Uri"/> (may contain wildcards).</param>
        /// <param name="subject">The expected subject (may contain wildcards).</param>
        /// <param name="action">The expected action (may contain wildcards).</param>
        /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
        /// <remarks>On first invocation will automatically replace <see cref="IEventPublisher"/> with a new <see cref="InMemoryPublisher"/> scoped service (DI) to capture events for this expectation. The other services are therefore required
        /// for this to function. As this is a scoped service no parallel execution of services against the same test host is supported as this capability is not considered thread-safe.</remarks>
        public static TSelf ExpectDestinationEvent<TSelf>(this IExpectations<TSelf> tester, string destination, string source, string subject, string? action = "*") where TSelf : IExpectations<TSelf>
            => tester.SetEventExpectation(e => e.Expect(destination, source, subject, action));

        /// <summary>
        /// Expects that the corresponding <paramref name="destination"/> <paramref name="event"/> has been published (in order specified). All properties for expected event will be compared again the actual.
        /// </summary>
        /// <param name="tester">The <see cref="IExpectations{TSelf}"/>.</param>
        /// <param name="destination">The named destination (e.g. queue or topic).</param>
        /// <param name="event">The expected <paramref name="event"/>. Wildcards are supported for <see cref="EventDataBase.Subject"/> and <see cref="EventDataBase.Action"/>.</param>
        /// <param name="pathsToIgnore">The paths to ignore from the comparison. Defaults to <see cref="GetExpectedEventsPathsToIgnore"/>.</param>
        /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
        /// <remarks>Wildcards are supported for <see cref="EventDataBase.Subject"/>, <see cref="EventDataBase.Action"/> and <see cref="EventDataBase.Type"/>.</remarks>
        public static TSelf ExpectDestinationEvent<TSelf>(this IExpectations<TSelf> tester, string destination, EventData @event, params string[] pathsToIgnore) where TSelf : IExpectations<TSelf>
            => tester.SetEventExpectation(e => e.Expect(destination, @event, pathsToIgnore));

        /// <summary>
        /// Expects that the corresponding <paramref name="destination"/> <paramref name="event"/> has been published (in order specified). All properties for expected event will be compared again the actual.
        /// </summary>
        /// <param name="tester">The <see cref="IExpectations{TSelf}"/>.</param>
        /// <param name="destination">The named destination (e.g. queue or topic).</param>
        /// <param name="source">The expected source formatted as a <see cref="Uri"/> (may contain wildcards).</param>
        /// <param name="event">The expected <paramref name="event"/>. Wildcards are supported for <see cref="EventDataBase.Subject"/> and <see cref="EventDataBase.Action"/>.</param>
        /// <param name="pathsToIgnore">The paths to ignore from the comparison. Defaults to <see cref="GetExpectedEventsPathsToIgnore"/>.</param>
        /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
        /// <remarks>Wildcards are supported for <see cref="EventDataBase.Subject"/>, <see cref="EventDataBase.Action"/> and <see cref="EventDataBase.Type"/>.</remarks>
        public static TSelf ExpectDestinationEvent<TSelf>(this IExpectations<TSelf> tester, string destination, string source, EventData @event, params string[] pathsToIgnore) where TSelf : IExpectations<TSelf>
            => tester.SetEventExpectation(e => e.Expect(destination, source, @event, pathsToIgnore));

        /// <summary>
        /// Expects that the JSON serialized <see cref="EventData"/> from the named embedded resource has been published (in order specified). All properties for expected event will be compared again the actual.
        /// </summary>
        /// <param name="tester">The <see cref="IExpectations{TSelf}"/>.</param>
        /// <param name="destination">The named destination (e.g. queue or topic).</param>
        /// <param name="resourceName">The embedded resource name (matches to the end of the fully qualifed resource name) that contains the expected value as serialized JSON.</param>
        /// <param name="pathsToIgnore">The paths to ignore from the comparison.</param>
        /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
        public static TSelf ExpectDestinationEventFromJsonResource<TSelf>(this IExpectations<TSelf> tester, string destination, string resourceName, params string[] pathsToIgnore) where TSelf : IExpectations<TSelf>
            => ExpectDestinationEventFromJsonResource(tester, destination, resourceName, Assembly.GetCallingAssembly(), pathsToIgnore);

        /// <summary>
        /// Expects that the JSON serialized <see cref="EventData"/> from the named embedded resource has been published (in order specified). All properties for expected event will be compared again the actual.
        /// </summary>
        /// <param name="tester">The <see cref="IExpectations{TSelf}"/>.</param>
        /// <param name="destination">The named destination (e.g. queue or topic).</param>
        /// <param name="resourceName">The embedded resource name (matches to the end of the fully qualifed resource name) that contains the expected value as serialized JSON.</param>
        /// <param name="assembly">The <see cref="Assembly"/> that contains the embedded resource.</param>
        /// <param name="pathsToIgnore">The paths to ignore from the comparison.</param>
        /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
        public static TSelf ExpectDestinationEventFromJsonResource<TSelf>(this IExpectations<TSelf> tester, string destination, string resourceName, Assembly assembly, params string[] pathsToIgnore) where TSelf : IExpectations<TSelf>
            => tester.SetEventExpectation(e => e.Expect(destination, Resource.GetJsonValue<EventData>(resourceName, assembly ?? Assembly.GetCallingAssembly(), e.Owner.JsonSerializer), pathsToIgnore));

        #endregion

        #region ValueExpectations

        /// <summary>
        /// Invokes the set expectation logic.
        /// </summary>
        private static TSelf SetValueExpectationExtension<TValue, TSelf>(this IValueExpectations<TValue, TSelf> tester, Func<AssertArgs, Task<bool>> extension) where TSelf : IValueExpectations<TValue, TSelf>
        {
            tester.ExpectationsArranger.GetOrAdd(() => new ValueExpectations<TSelf>(tester.ExpectationsArranger.Owner, (TSelf)tester)).AddExtension(extension);
            return (TSelf)tester;
        }

        /// <summary>
        /// Verifies implements <see cref="Type"/>.
        /// </summary>
        private static void VerifyImplements<TValue, TInterface>()
        {
            if (typeof(TValue).GetInterface(typeof(TInterface).FullName ?? typeof(TInterface).Name) == null)
                throw new InvalidOperationException($"{typeof(TValue).Name} must implement the interface {typeof(TInterface).Name}.");
        }

        /// <summary>
        /// Expects the <see cref="IIdentifier"/> to be implemented and have non-default <see cref="IIdentifier.Id"/>.
        /// </summary>
        /// <typeparam name="TSelf">The expectations <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
        /// <param name="tester">The <see cref="IValueExpectations{TValue, TSelf}"/> tester.</param>
        /// <param name="identifier">The optional expected identifier to compare to.</param>
        /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
        public static TSelf ExpectIdentifier<TValue, TSelf>(this IValueExpectations<TValue, TSelf> tester, object? identifier = null) where TSelf : IValueExpectations<TValue, TSelf>
        {
            VerifyImplements<TValue, IIdentifier>();
            IgnoreIdentifier(tester);
            var pn = $"{nameof(IIdentifier)}.{nameof(IIdentifier.Id)}";

            Task<bool> extension(AssertArgs args)
            {
                var id = args.Value as IIdentifier;
                if (id is null || id.Id is null)
                    args.Tester.Implementor.AssertFail($"Expected {pn} to have a non-null value.");

                if (identifier is null)
                {
                    if (System.Collections.Comparer.Default.Compare(id!.Id, id!.GetType().IsClass ? null! : Activator.CreateInstance(id!.GetType())) == 0)
                        args.Tester.Implementor.AssertFail($"Expected {pn} to have a non-default value.");
                }
                else
                    args.Tester.Implementor.AssertAreEqual(identifier, id!.Id, $"Expected {pn} value of '{identifier}'; actual '{id.Id}'.");

                return Task.FromResult(false);
            }

            return SetValueExpectationExtension(tester, extension);
        }

        /// <summary>
        /// Expects the <see cref="IPrimaryKey"/> to be implemented and have non-default <see cref="IPrimaryKey.PrimaryKey"/>.
        /// </summary>
        /// <typeparam name="TSelf">The expectations <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
        /// <param name="tester">The <see cref="IValueExpectations{TValue, TSelf}"/> tester.</param>
        /// <param name="primaryKey">The optional expected primary key to compare to.</param>
        /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
        public static TSelf ExpectPrimaryKey<TValue, TSelf>(this IValueExpectations<TValue, TSelf> tester, object? primaryKey) where TSelf : IValueExpectations<TValue, TSelf>
            => ExpectPrimaryKey(tester, new CompositeKey(primaryKey));

        /// <summary>
        /// Expects the <see cref="IPrimaryKey"/> to be implemented and have non-default <see cref="IPrimaryKey.PrimaryKey"/>.
        /// </summary>
        /// <typeparam name="TSelf">The expectations <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
        /// <param name="tester">The <see cref="IValueExpectations{TValue, TSelf}"/> tester.</param>
        /// <param name="primaryKey">The optional expected primary key to compare to.</param>
        /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
        public static TSelf ExpectPrimaryKey<TValue, TSelf>(this IValueExpectations<TValue, TSelf> tester, CompositeKey? primaryKey = null) where TSelf : IValueExpectations<TValue, TSelf>
        {
            VerifyImplements<TValue, IPrimaryKey>();
            IgnorePrimaryKey(tester);
            var pn = $"{nameof(IPrimaryKey)}.{nameof(IPrimaryKey.PrimaryKey)}";

            Task<bool> extension(AssertArgs args)
            {
                var pk = args.Value as IPrimaryKey;
                if (pk is null)
                    args.Tester.Implementor.AssertFail($"Expected {pn} to have a non-null value.");

                if (pk!.PrimaryKey.IsInitial)
                    args.Tester.Implementor.AssertFail($"Expected {pn}.{nameof(CompositeKey.Args)} to have one or more non-default values.");

                if (primaryKey.HasValue)
                     args.Tester.Implementor.AssertAreEqual(primaryKey.Value, pk.PrimaryKey, $"Expected {pn} value of '{primaryKey.Value}'; actual '{pk.PrimaryKey}'.");

                return Task.FromResult(false);
            }

            return SetValueExpectationExtension(tester, extension);
        }

        /// <summary>
        /// Expects the <see cref="IETag"/> to be implemented and have non-default <see cref="IETag.ETag"/>.
        /// </summary>
        /// <typeparam name="TSelf">The expectations <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
        /// <param name="tester">The <see cref="IValueExpectations{TValue, TSelf}"/> tester.</param>
        /// <param name="previousETag">The optional previous ETag to compare not equal to; i.e. it must be different.</param>
        /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
        public static TSelf ExpectETag<TValue, TSelf>(this IValueExpectations<TValue, TSelf> tester, string? previousETag = null) where TSelf : IValueExpectations<TValue, TSelf>
        {
            VerifyImplements<TValue, IETag>();
            IgnoreETag(tester);
            var pn = $"{nameof(IETag)}.{nameof(IETag.ETag)}";

            Task<bool> extension(AssertArgs args)
            {
                var etag = args.Value as IETag;
                if (etag is null || etag.ETag is null)
                    args.Tester.Implementor.AssertFail($"Expected {pn} to have a non-null value.");

                if (previousETag is not null && previousETag == etag!.ETag)
                    args.Tester.Implementor.AssertFail($"Expected {pn} value of '{previousETag}' to be different to actual.");

                return Task.FromResult(false);
            }

            return SetValueExpectationExtension(tester, extension);
        }

        /// <summary>
        /// Expects the <see cref="IChangeLog"/> to be implemented and have non-default <see cref="IChangeLog.ChangeLog"/> <see cref="ChangeLog.CreatedBy"/> and <see cref="ChangeLog.CreatedDate"/> values.
        /// </summary>
        /// <typeparam name="TSelf">The expectations <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
        /// <param name="tester">The <see cref="IValueExpectations{TValue, TSelf}"/> tester.</param>
        /// <param name="createdBy">The specific <see cref="ChangeLog.CreatedBy"/> value where specified (can include wildcards); otherwise, indicates to check for user running the test (see <see cref="Abstractions.TesterBase.UserName"/>).</param>
        /// <param name="createdDateGreaterThan">The <see cref="DateTime"/> in which the <see cref="ChangeLog.CreatedDate"/> should be greater than or equal to; where <c>null</c> it will default to <see cref="DateTime.UtcNow"/>.</param>
        /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
        public static TSelf ExpectChangeLogCreated<TValue, TSelf>(this IValueExpectations<TValue, TSelf> tester, string? createdBy = null, DateTime? createdDateGreaterThan = null) where TSelf : IValueExpectations<TValue, TSelf>
        {
            VerifyImplements<TValue, IChangeLog>();
            IgnoreChangeLog(tester);
            var pn = $"{nameof(IChangeLog)}.{nameof(IChangeLog.ChangeLog)}";

            createdBy ??= tester.ExpectationsArranger.Owner.UserName;
            createdDateGreaterThan = Cleaner.Clean(createdDateGreaterThan ?? DateTime.UtcNow.Subtract(new TimeSpan(0, 0, 1)));

            Task<bool> extension(AssertArgs args)
            {
                var cl = args.Value as IChangeLogAuditLog;
                if (cl is null || cl.ChangeLogAudit == null)
                    args.Tester.Implementor.AssertFail($"Expected Change Log ({nameof(IChangeLogAuditLog)}.{nameof(IChangeLogAuditLog.ChangeLogAudit)}) to have a non-null value.");

                if (cl!.ChangeLogAudit!.CreatedBy == null)
                    args.Tester.Implementor.AssertFail($"Expected Change Log ({nameof(IChangeLogAuditLog)}.{nameof(IChangeLogAuditLog.ChangeLogAudit)}).{nameof(ChangeLog.CreatedBy)} value of '{createdBy}'; actual was null.");
                else
                {
                    var wcr = Wildcard.BothAll.Parse(createdBy).ThrowOnError();
                    if (cl?.ChangeLogAudit?.CreatedBy == null || !wcr.CreateRegex().IsMatch(cl!.ChangeLogAudit!.CreatedBy))
                        args.Tester.Implementor.AssertFail($"Expected Change Log ({nameof(IChangeLogAuditLog)}.{nameof(IChangeLogAuditLog.ChangeLogAudit)}).{nameof(ChangeLog.CreatedBy)} value of '{createdBy}'; actual '{cl?.ChangeLogAudit?.CreatedBy}'.");
                }

                if (!cl!.ChangeLogAudit!.CreatedDate.HasValue)
                    args.Tester.Implementor.AssertFail($"Expected Change Log ({nameof(IChangeLogAuditLog)}.{nameof(IChangeLogAuditLog.ChangeLogAudit)}).{nameof(ChangeLog.CreatedDate)} to have a non-null value.");
                else if (Cleaner.Clean(cl!.ChangeLogAudit!.CreatedDate.Value) < createdDateGreaterThan.Value)
                    args.Tester.Implementor.AssertFail($"Expected Change Log ({nameof(IChangeLogAuditLog)}.{nameof(IChangeLogAuditLog.ChangeLogAudit)}).{nameof(ChangeLog.CreatedDate)} value of '{createdDateGreaterThan.Value}'; actual '{cl.ChangeLogAudit.CreatedDate.Value}' must be greater than or equal to expected.");

                return Task.FromResult(false);
            }

            return SetValueExpectationExtension(tester, extension);
        }

        /// <summary>
        /// Expects the <see cref="IChangeLog"/> to be implemented and have non-default <see cref="IChangeLog.ChangeLog"/> <see cref="ChangeLog.UpdatedBy"/> and <see cref="ChangeLog.UpdatedDate"/> values.
        /// </summary>
        /// <typeparam name="TSelf">The expectations <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
        /// <param name="tester">The <see cref="IValueExpectations{TValue, TSelf}"/> tester.</param>
        /// <param name="updatedBy">The specific <see cref="ChangeLog.UpdatedBy"/> value where specified (can include wildcards); otherwise, indicates to check for user running the test (see <see cref="Abstractions.TesterBase.UserName"/>).</param>
        /// <param name="updatedDateGreaterThan">The <see cref="DateTime"/> in which the <see cref="ChangeLog.UpdatedDate"/> should be greater than or equal to; where <c>null</c> it will default to <see cref="DateTime.UtcNow"/>.</param>
        /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
        public static TSelf ExpectChangeLogUpdated<TValue, TSelf>(this IValueExpectations<TValue, TSelf> tester, string? updatedBy = null, DateTime? updatedDateGreaterThan = null) where TSelf : IValueExpectations<TValue, TSelf>
        {
            VerifyImplements<TValue, IChangeLog>();
            IgnoreChangeLog(tester);
            var pn = $"{nameof(IChangeLog)}.{nameof(IChangeLog.ChangeLog)}";

            updatedBy ??= tester.ExpectationsArranger.Owner.UserName;
            updatedDateGreaterThan = Cleaner.Clean(updatedDateGreaterThan ?? DateTime.UtcNow.Subtract(new TimeSpan(0, 0, 1)));

            Task<bool> extension(AssertArgs args)
            {
                var cl = args.Value as IChangeLogAuditLog;
                if (cl is null || cl.ChangeLogAudit == null)
                    args.Tester.Implementor.AssertFail($"Expected Change Log ({nameof(IChangeLogAuditLog)}.{nameof(IChangeLogAuditLog.ChangeLogAudit)}) to have a non-null value.");

                if (cl!.ChangeLogAudit!.UpdatedBy == null)
                    args.Tester.Implementor.AssertFail($"Expected Change Log ({nameof(IChangeLogAuditLog)}.{nameof(IChangeLogAuditLog.ChangeLogAudit)}).{nameof(ChangeLog.UpdatedBy)} value of '{updatedBy}'; actual was null.");
                else
                {
                    var wcr = Wildcard.BothAll.Parse(updatedBy).ThrowOnError();
                    if (cl?.ChangeLogAudit?.UpdatedBy == null || !wcr.CreateRegex().IsMatch(cl!.ChangeLogAudit!.UpdatedBy))
                        args.Tester.Implementor.AssertFail($"Expected Change Log ({nameof(IChangeLogAuditLog)}.{nameof(IChangeLogAuditLog.ChangeLogAudit)}).{nameof(ChangeLog.UpdatedBy)} value of '{updatedBy}'; actual '{cl?.ChangeLogAudit?.UpdatedBy}'.");
                }

                if (!cl!.ChangeLogAudit!.UpdatedDate.HasValue)
                    args.Tester.Implementor.AssertFail($"Expected Change Log ({nameof(IChangeLogAuditLog)}.{nameof(IChangeLogAuditLog.ChangeLogAudit)}).{nameof(ChangeLog.UpdatedDate)} to have a non-null value.");
                else if (Cleaner.Clean(cl!.ChangeLogAudit!.UpdatedDate.Value) < updatedDateGreaterThan.Value)
                    args.Tester.Implementor.AssertFail($"Expected Change Log ({nameof(IChangeLogAuditLog)}.{nameof(IChangeLogAuditLog.ChangeLogAudit)}).{nameof(ChangeLog.UpdatedDate)} value of '{updatedDateGreaterThan.Value}'; actual '{cl.ChangeLogAudit.UpdatedDate.Value}' must be greater than or equal to expected.");

                return Task.FromResult(false);
            }

            return SetValueExpectationExtension(tester, extension);
        }

        #endregion

        #region IgnorePathsExpectations

        /// <summary>
        /// Ignores the <see cref="IIdentifier.Id"/> JSON path.
        /// </summary>
        /// <typeparam name="TSelf">The expectations <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
        /// <param name="tester">The <see cref="IValueExpectations{TValue, TSelf}"/> tester.</param>
        /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
        public static TSelf IgnoreIdentifier<TValue, TSelf>(this IValueExpectations<TValue, TSelf> tester) where TSelf : IValueExpectations<TValue, TSelf> => IgnorePaths(tester, nameof(IIdentifier.Id));

        /// <summary>
        /// Ignores the <see cref="IPrimaryKey.PrimaryKey"/> JSON path.
        /// </summary>
        /// <typeparam name="TSelf">The expectations <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
        /// <param name="tester">The <see cref="IValueExpectations{TValue, TSelf}"/> tester.</param>
        /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
        public static TSelf IgnorePrimaryKey<TValue, TSelf>(this IValueExpectations<TValue, TSelf> tester) where TSelf : IValueExpectations<TValue, TSelf> => IgnorePaths(tester, nameof(IPrimaryKey.PrimaryKey));

        /// <summary>
        /// Ignores the <see cref="IETag.ETag"/> JSON path.
        /// </summary>
        /// <typeparam name="TSelf">The expectations <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
        /// <param name="tester">The <see cref="IValueExpectations{TValue, TSelf}"/> tester.</param>
        /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
        public static TSelf IgnoreETag<TValue, TSelf>(this IValueExpectations<TValue, TSelf> tester) where TSelf : IValueExpectations<TValue, TSelf> => IgnorePaths(tester, nameof(IETag.ETag));

        /// <summary>
        /// Ignores the <see cref="IChangeLog.ChangeLog"/> JSON path.
        /// </summary>
        /// <typeparam name="TSelf">The expectations <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
        /// <param name="tester">The <see cref="IValueExpectations{TValue, TSelf}"/> tester.</param>
        /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
        public static TSelf IgnoreChangeLog<TValue, TSelf>(this IValueExpectations<TValue, TSelf> tester) where TSelf : IValueExpectations<TValue, TSelf> => IgnorePaths(tester, nameof(IChangeLog.ChangeLog));

        /// <summary>
        /// Adds <paramref name="paths"/> to ignore from the JSON value comparison.
        /// </summary>
        /// <typeparam name="TSelf">The expectations <see cref="Type"/>.</typeparam>
        /// <param name="tester">The <see cref="IExpectations{TSelf}"/> tester.</param>
        /// <param name="paths">The JSON paths to ignore.</param>
        /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
        public static TSelf IgnorePaths<TSelf>(IExpectations<TSelf> tester, params string[] paths) where TSelf : IExpectations<TSelf>
        {
            tester.ExpectationsArranger.PathsToIgnore.AddRange(paths);
            return (TSelf)tester;
        }

        #endregion

        #region ErrorExpectations

        /// <summary>
        /// Expects the specified <paramref name="errorType"/>.
        /// </summary>
        /// <typeparam name="TSelf">The expectations <see cref="Type"/>.</typeparam>
        /// <param name="expectations">The <see cref="IExpectations{TSelf}"/>.</param>
        /// <param name="errorType">The expected <see cref="ErrorType"/>.</param>
        /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
        public static TSelf ExpectErrorType<TSelf>(this IExpectations<TSelf> expectations, ErrorType errorType) where TSelf : IExpectations<TSelf>
        {
            var ee = expectations.ExpectationsArranger.GetOrAdd(() => new ErrorExpectations<TSelf>(expectations.ExpectationsArranger.Owner, (TSelf)expectations));

            ee.AddExtension(args =>
            {
                if (args.Exception is not null)
                {
                    if (args.Exception is IExtendedException eex)
                    {
                        if (eex.ErrorType != errorType.ToString())
                            args.Tester.Implementor.AssertFail($"Expected error type of '{errorType}' but actual was '{eex.ErrorType}'.");
                        else
                            return Task.FromResult(false);
                    }
                }

                if (args.TryGetExtra<HttpResponseMessage>(out var result) && result is not null)
                {
                    if (result.Headers.TryGetValues(HttpConsts.ErrorTypeHeaderName, out var vals))
                    {
                        if (vals.Contains(errorType.ToString()))
                            return Task.FromResult(false);
                        else
                            args.Tester.Implementor.AssertFail($"Expected error type of '{errorType}' but actual was {string.Join(", ", vals.Select(x => $"'{x}'"))}.");
                    }
                }

                args.Tester.Implementor.AssertFail($"Expected error type of '{errorType}' but none was returned.");
                return Task.FromResult(false);
            });

            return (TSelf)expectations;
        }

        /// <summary>
        /// Expects that one or more errors will be returned matching the specified <paramref name="messages"/>.
        /// </summary>
        /// <typeparam name="TSelf">The expectations <see cref="Type"/>.</typeparam>
        /// <param name="expectations">The <see cref="IExpectations{TSelf}"/>.</param>
        /// <param name="messages">The error messages.</param>
        /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns> 
        public static TSelf ExpectErrors<TSelf>(this IExpectations<TSelf> expectations, MessageItemCollection messages) where TSelf : IExpectations<TSelf>
            => ExpectErrors(expectations, messages is null ? [] : messages.ToArray());

        /// <summary>
        /// Expects that one or more errors will be returned matching the specified <paramref name="messages"/>.
        /// </summary>
        /// <typeparam name="TSelf">The expectations <see cref="Type"/>.</typeparam>
        /// <param name="expectations">The <see cref="IExpectations{TSelf}"/>.</param>
        /// <param name="messages">The error messages.</param>
        /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns> 
        public static TSelf ExpectErrors<TSelf>(this IExpectations<TSelf> expectations, params MessageItem[] messages) where TSelf : IExpectations<TSelf>
        {
            if (messages.Length == 0)
                return (TSelf)expectations;

            var errors = new List<ApiError>();
            foreach (var msg in messages.Where(x => x.Type == MessageType.Error))
                errors.Add(new ApiError(msg.Property, msg.Text ?? string.Empty));

            return expectations.ExpectErrors(errors.ToArray());
        }

        #endregion
    }
}