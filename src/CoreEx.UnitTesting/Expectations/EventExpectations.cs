// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx;
using CoreEx.Events;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnitTestEx.Abstractions;
using UnitTestEx.Json;

namespace UnitTestEx.Expectations
{
    /// <summary>
    /// Provides <see cref="CoreEx.Events"/> expectations.
    /// </summary>
    /// <param name="owner">The owning <see cref="TesterBase"/>.</param>
    /// <param name="tester">The initiating tester.</param>
    public class EventExpectations<TTester>(TesterBase owner, TTester tester) : ExpectationsBase<TTester>(owner, tester)
    {
        private readonly Dictionary<string, List<(string? Source, string? Subject, string? Action, EventData? Event, string[] PathsToIgnore)>> _expectedEvents = [];
        private bool _expectNoEvents = true;
        private bool _expectEvents;

        /// <inheritdoc/>
        /// <remarks>Overrides to ensure it occurs after majority.</remarks>
        public override int Order => 1000;

        /// <summary>
        /// Expects that no events have been published.
        /// </summary>
        /// <remarks>Any previously explicitly specified expected events will be removed.</remarks>
        public void ExpectNoEvents()
        {
            _expectedEvents.Clear();
            _expectNoEvents = true;
            _expectEvents = false;
        }

        /// <summary>
        /// Expects that at least one event has been published.
        /// </summary>
        /// <remarks>Any previously explicitly specified expected events will be removed.</remarks>
        public void ExpectEvents()
        {
            if (!Owner.IsExpectedEventPublisherConfigured())
                throw new NotSupportedException($"The {nameof(TestSetUp)}.{nameof(UnitTestExExtensions.EnableExpectedEvents)} or {nameof(TesterBase)}.{nameof(UnitTestExExtensions.UseExpectedEvents)} must be used before this functionality can be executed; note that enabling will automatically replace the {nameof(IEventPublisher)} to use the {nameof(ExpectedEventPublisher)}.");

            _expectedEvents.Clear();
            _expectNoEvents = false;
            _expectEvents = true;
        }

        /// <summary>
        /// Adds the event into the dictionary.
        /// </summary>
        private void Add(string? destination, (string? Source, string? Subject, string? Action, EventData? Event, string[] PathsToIgnore) @event)
        {
            if (!Owner.IsExpectedEventPublisherConfigured())
                throw new NotSupportedException($"The {nameof(TestSetUp)}.{nameof(UnitTestExExtensions.EnableExpectedEvents)} or {nameof(TesterBase)}.{nameof(UnitTestExExtensions.UseExpectedEvents)} must be used before this functionality can be executed; note that enabling will automatically replace the {nameof(IEventPublisher)} to use the {nameof(ExpectedEventPublisher)}.");

            var key = destination ?? ExpectedEventPublisher.NullKeyName;
            if (_expectedEvents.TryGetValue(key, out var events))
                events.Add(@event);
            else
                _expectedEvents.Add(key, [@event]);

            _expectNoEvents = false;
            _expectEvents = false;
        }

        /// <summary>
        /// Expects that the corresponding event has been published (in order specified). The expected event <paramref name="subject"/> and <paramref name="action"/> can use wildcards. All other <see cref="EventData"/> properties are not matched/verified.
        /// </summary>
        /// <param name="destination">The named destination (e.g. queue or topic).</param>
        /// <param name="subject">The expected subject (may contain wildcards).</param>
        /// <param name="action">The expected action (may contain wildcards).</param>
        public void Expect(string? destination, string subject, string? action = "*") => Add(destination, ("*", subject.ThrowIfNull(nameof(subject)), action, null, []));

        /// <summary>
        /// Expects that the corresponding event has been published (in order specified). The expected event <paramref name="source"/>, <paramref name="subject"/> and <paramref name="action"/> can use wildcards. All other <see cref="EventData"/> 
        /// properties are not matched/verified.
        /// </summary>
        /// <param name="destination">The named destination (e.g. queue or topic).</param>
        /// <param name="source">The expected source formatted as a <see cref="Uri"/> (may contain wildcards).</param>
        /// <param name="subject">The expected subject (may contain wildcards).</param>
        /// <param name="action">The expected action (may contain wildcards).</param>
        public void Expect(string? destination, string source, string subject, string? action = "*") => Add(destination, (source.ThrowIfNull(nameof(source)), subject.ThrowIfNull(nameof(subject)), action, null, []));

        /// <summary>
        /// Expects that the corresponding <paramref name="event"/> has been published (in order specified). All properties for expected event will be compared again the actual.
        /// </summary>
        /// <param name="destination">The named destination (e.g. queue or topic).</param>
        /// <param name="event">The expected <paramref name="event"/>. Wildcards are supported for <see cref="EventDataBase.Subject"/> and <see cref="EventDataBase.Action"/>.</param>
        /// <param name="pathsToIgnore">The JSON paths to ignore from the comparison. Defaults to <see cref="UnitTestExExtensions.GetExpectedEventsPathsToIgnore"/>.</param>
        /// <remarks>Wildcards are supported for <see cref="EventDataBase.Subject"/>, <see cref="EventDataBase.Action"/> and <see cref="EventDataBase.Type"/>.</remarks>
        public void Expect(string? destination, EventData @event, params string[] pathsToIgnore) => Add(destination, (null, @event?.Subject, @event?.Action, @event.ThrowIfNull(nameof(@event)), pathsToIgnore));

        /// <summary>
        /// Expects that the corresponding <paramref name="event"/> has been published (in order specified). All properties for expected event will be compared again the actual.
        /// </summary>
        /// <param name="destination">The named destination (e.g. queue or topic).</param>
        /// <param name="source">The expected source formatted as a <see cref="Uri"/> (may contain wildcards).</param>
        /// <param name="event">The expected <paramref name="event"/>. Wildcards are supported for <see cref="EventDataBase.Subject"/> and <see cref="EventDataBase.Action"/>.</param>
        /// <param name="pathsToIgnore">The JSON paths to ignore from the comparison. Defaults to <see cref="UnitTestExExtensions.GetExpectedEventsPathsToIgnore"/>.</param>
        /// <remarks>Wildcards are supported for <see cref="EventDataBase.Subject"/>, <see cref="EventDataBase.Action"/> and <see cref="EventDataBase.Type"/>.</remarks>
        public void Expect(string? destination, string source, EventData @event, params string[] pathsToIgnore) => Add(destination, (source, @event?.Subject, @event?.Action, @event.ThrowIfNull(nameof(@event)), pathsToIgnore));

        /// <inheritdoc/>
        protected override Task OnAssertAsync(AssertArgs args)
        {
            var expectedEventPublisher = ExpectedEventPublisher.GetFromSharedState(args.Tester.SharedState);
            if (expectedEventPublisher is null)
            {
                if (_expectNoEvents)
                    return Task.CompletedTask;
                else
                    throw new InvalidOperationException($"The {nameof(ExpectedEventPublisher)}.{nameof(ExpectedEventPublisher.GetFromSharedState)} must not return null; there is an internal issue.");
            }

            if (!expectedEventPublisher.IsEmpty)
                args.Tester.Implementor.AssertFail("Expected Event Publish/Send mismatch; there are one or more published events that have not been sent.");

            var names = expectedEventPublisher.SentEvents.Keys.ToArray();
            if (_expectNoEvents && !_expectEvents && _expectedEvents.Count == 0 && names.Length > 0)
                args.Tester.Implementor.AssertFail($"Expected no Event(s); one or more were published.");

            if (names.Length == 0 && (_expectEvents || _expectedEvents.Count != 0))
                args.Tester.Implementor.AssertFail($"Expected Event(s); none were published.");

            if (_expectEvents)
                return Task.CompletedTask;

            if (names.Length != _expectedEvents.Count)
                args.Tester.Implementor.AssertFail($"Expected {_expectedEvents.Count} event destination(s); there were {names.Length}.");

            if (names.Length == 1 && _expectedEvents.Count == 1 && _expectedEvents.ContainsKey(ExpectedEventPublisher.NullKeyName))
            {
                var key = _expectedEvents.Keys.First();
                AssertDestination(args, key, _expectedEvents[key], [.. EventExpectations<TTester>.GetEvents(expectedEventPublisher, names[0])]);
                return Task.CompletedTask;
            }

            foreach (var name in names)
            {
                if (_expectedEvents.TryGetValue(name, out var exp))
                    AssertDestination(args, name, exp, [.. EventExpectations<TTester>.GetEvents(expectedEventPublisher, name)]);
                else
                    args.Tester.Implementor.AssertFail($"Published event(s) to destination '{name}'; these were not expected.");
            }

            var missing = string.Join(", ", _expectedEvents.Keys.Where(key => !names.Contains(key)).Select(x => $"'{x}'"));
            if (!string.IsNullOrEmpty(missing))
                args.Tester.Implementor.AssertFail($"Expected event(s) to be published to destination(s): {missing}; none were found.");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets the event from the event storage.
        /// </summary>
        private static List<string?> GetEvents(ExpectedEventPublisher expectedEventPublisher, string? name) => expectedEventPublisher!.SentEvents.TryGetValue(name ?? ExpectedEventPublisher.NullKeyName, out var queue) ? [.. queue] : new();

        /// <summary>
        /// Asserts the events for the destination.
        /// </summary>
        private void AssertDestination(AssertArgs args, string? destination, List<(string? Source, string? Subject, string? Action, EventData? Event, string[] PathsToIgnore)> expectedEvents, List<string?> actualEvents)
        {
            if (actualEvents.ThrowIfNull(nameof(actualEvents)).Count != expectedEvents.Count)
                args.Tester.Implementor.AssertFail($"Destination {destination}: Expected {_expectedEvents.Count} event(s); there were {actualEvents.Count} actual.");

            for (int i = 0; i < actualEvents.Count; i++)
            {
                var exp = expectedEvents[i].Event;
                var wcexp = exp ?? new EventData { Subject = expectedEvents[i].Subject, Action = expectedEvents[i].Action };
                var act = (EventData)args.Tester.JsonSerializer.Deserialize(actualEvents[i]!, wcexp.GetType())!;

                // Assert source, subject, action and type using wildcards where specified.
                if (expectedEvents[i].Source != null && !WildcardMatch(args, expectedEvents[i].Source!, act.Source?.ToString(), '/'))
                    args.Tester.Implementor.AssertFail($"Destination {destination}: Expected Event[{i}].{nameof(EventDataBase.Source)} '{expectedEvents[i].Source}' does not match actual '{act.Source}'.");

                if (wcexp.Subject != null && !WildcardMatch(args, wcexp.Subject!, act.Subject?.ToString(), args.Tester.SetUp.GetExpectedEventsFormatter().SubjectSeparatorCharacter))
                    args.Tester.Implementor.AssertFail($"Destination {destination}: Expected Event[{i}].{nameof(EventDataBase.Subject)} '{wcexp.Subject}' does not match actual '{act.Subject}'.");

                if (wcexp.Action != null && !WildcardMatch(args, wcexp.Action!, act.Action?.ToString(), char.MinValue))
                    args.Tester.Implementor.AssertFail($"Destination {destination}: Expected Event[{i}].{nameof(EventDataBase.Action)} '{wcexp.Action}' does not match actual '{act.Action}'.");

                if (wcexp.Type != null && !WildcardMatch(args, wcexp.Type!, act.Type?.ToString(), args.Tester.SetUp.GetExpectedEventsFormatter().TypeSeparatorCharacter))
                    args.Tester.Implementor.AssertFail($"Destination {destination}: Expected Event[{i}].{nameof(EventDataBase.Type)} '{wcexp.Type}' does not match actual '{act.Type}'.");

                // Where there is *no* expected eventdata then skip comparison.
                if (exp == null)
                    continue;

                // Compare the events.
                var list = new List<string>(args.Tester.SetUp.GetExpectedEventsPathsToIgnore());
                list.AddRange(new string[] { nameof(EventDataBase.Source), nameof(EventDataBase.Subject), nameof(EventDataBase.Action), nameof(EventDataBase.Type) });
                list.AddRange(expectedEvents[i].PathsToIgnore);

                var res = JsonElementComparer.Default.Compare(args.Tester.JsonSerializer.Serialize(exp), actualEvents[i]!, [.. list]);
                if (res.HasDifferences)
                    args.Tester.Implementor.AssertFail($"Destination {destination}; Expected event is not equal to actual:{Environment.NewLine}{res}");
            }
        }

        /// <summary>
        /// Performs a wildcard match on each part of the strings using the <paramref name="separatorCharacter"/> to split.
        /// </summary>
        /// <param name="args">The <see cref="AssertArgs"/>.</param>
        /// <param name="expected">The expected including wildcards.</param>
        /// <param name="actual">The actual to compare against the expected.</param>
        /// <param name="separatorCharacter">The seperator character.</param>
        /// <returns><c>true</c> where there is a wildcard match; otherwise, <c>false</c>.</returns>
        public bool WildcardMatch(AssertArgs args, string expected, string? actual, char separatorCharacter)
        {
            if (expected.ThrowIfNull(nameof(expected)) == "*")
                return true;

            var eparts = expected.Split(separatorCharacter);
            if (actual == null)
                return false;

            var aparts = actual.Split(separatorCharacter);

            // Compare each part for an exact match or wildcard.
            for (int i = 0; i < eparts.Length; i++)
            {
                if (i >= aparts.Length)
                    return false;

                if (new string[] { aparts[i] }.WhereWildcard(x => x, eparts[i], ignoreCase: false, wildcard: args.Tester.SetUp.GetExpectedEventsWildcard()).FirstOrDefault() == null)
                    return false;
            }

            if (aparts.Length == eparts.Length)
                return true;

            // Where longer make sure last part is a multi wildcard.
            return aparts.Length > eparts.Length && eparts[^1] == new string(new char[] { args.Tester.SetUp.GetExpectedEventsWildcard().MultiWildcard });
        }
    }
}