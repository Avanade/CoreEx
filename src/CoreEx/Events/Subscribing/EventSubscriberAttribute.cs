// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Wildcards;
using System;
using System.Linq;
using System.Reflection;

namespace CoreEx.Events.Subscribing
{
    /// <summary>
    /// Defines the <see cref="IEventSubscriber"/> matching criteria.
    /// </summary>
    /// <remarks>The matching supports wildcards where specifically allowed; this is performed by <see cref="IsMatch(string?, string?, char)"/> (<see cref="IsMatch(string?, string?, char)">see</see> for supported capabilities).</remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class EventSubscriberAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventSubscriberAttribute"/> class.
        /// </summary>
        /// <param name="subject">The <see cref="EventDataBase.Subject"/> template path (may contain wildcards).</param>
        /// <param name="actions">The <see cref="EventDataBase.Action"/> templates where at least one must match where specified (may contain wildcards).</param>
        public EventSubscriberAttribute(string? subject = null, params string[]? actions)
        {
            Subject = subject;
            Actions = actions;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventSubscriberAttribute"/> class.
        /// </summary>
        /// <param name="source">The <see cref="EventDataBase.Source"/> template <see cref="Uri"/> (may contain wildcards)</param>
        /// <param name="type">The <see cref="EventDataBase.Type"/> template path (may contain wildcards).</param>
        public EventSubscriberAttribute(Uri source, string? type = null)
        {
            Source = source;
            Type = type;
        }

        /// <summary>
        /// Gets or sets the <see cref="EventDataBase.Subject"/> template path (may contain wildcards).
        /// </summary>
        public string? Subject { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="EventDataBase.Action"/> template(s) where at least one must match where specified (may contain wildcards).
        /// </summary>
        public string[]? Actions { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="EventDataBase.Source"/> template <see cref="Uri"/> (<see cref="Uri.AbsolutePath"/> may contain wildcards).
        /// </summary>
        public Uri? Source { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="EventDataBase.Type"/> template path (may contain wildcards).
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Indicates whether the matching should ignore case (default) or not.
        /// </summary>
        public bool IgnoreCase { get; set; } = true;

        /// <summary>
        /// Gets or sets the extended match method name that is to be invoked on the <see cref="IEventSubscriber"/> to perform any extended match for the declared <see cref="EventSubscriberAttribute"/>.
        /// </summary>
        /// <remarks>The extended match method signature must be: <c>public static bool ExtendedMatchName(EventData)</c>. The current <see cref="EventData"/> will be passed as the parameter. The method must return a <c>bool</c> where <c>true</c>
        /// indicates a match; otherwise, <c>false</c>.
        /// <para>The static method will only be invoked where the <see cref="Subject"/>, <see cref="Type"/> and <see cref="Actions"/> have resulted in a match; i.e. this allows additional match filtering.</para></remarks>
        public string? ExtendedMatchMethod { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ExtendedMatchMethod"/> <see cref="MethodInfo"/>.
        /// </summary>
        internal MethodInfo? ExtendedMatchMethodInfo { get; set; }

        /// <summary>
        /// Indicates whether the actual event metadata matches the subscribing criteria.
        /// </summary>
        /// <param name="formatter">The <see cref="EventDataFormatter"/>.</param>
        /// <param name="event">The actual <see cref="EventData"/> to match.</param>
        /// <returns><c>true</c> indicates a match; otherwise, <c>false</c>.</returns>
        /// <remarks>Where the actual event metadata values are all <c>null</c> then it will immediately fail the match.</remarks>
        public bool IsMatch(EventDataFormatter formatter, EventData @event) => IsMatch(formatter, @event.Subject, @event.Type, @event.Action, @event.Source);

        /// <summary>
        /// Matches on the key subject, type and action properties.
        /// </summary>
        private bool IsMatch(EventDataFormatter formatter, string? subject, string? type, string? action, Uri? source)
        {
            if (string.IsNullOrEmpty(subject) && string.IsNullOrEmpty(type) && string.IsNullOrEmpty(action) && source is null)
                return false;

            if (subject != null && !IsMatch(Subject, subject, formatter.SubjectSeparatorCharacter))
                return false;

            if (type != null && !IsMatch(Type, type, formatter.TypeSeparatorCharacter))
                return false;

            if (source != null && !IsMatchSource(Source, source))
                return false;

            if (Actions == null || Actions.Length == 0)
                return true;

            foreach (var at in Actions)
            {
                if (string.IsNullOrEmpty(at) || at == action || at == "*")
                    return true;

                if (Wildcard.Default.Parse(at).CreateRegex(IgnoreCase).IsMatch(action))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Splits template and action into parts using seperator, then compares part by part using wildcards to determine match.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="seperator">The seperator character.</param>
        /// <remarks>Wildcards are matached using the <see cref="Wildcard.Default"/> functionality; supports the standard wildcards characters being '<c>*</c>' (<see cref="Wildcard.MultiWildcardCharacter"/>) and '<c>?</c>' (<see cref="Wildcard.SingleWildcardCharacter"/>).
        /// <para>To support matching the contents of all children paths (regardless of depth) the double wilcard '<c>**</c>' must be used; for example '<c>root/**</c>' allows '<c>root/abc</c>' and '<c>root/abc/def</c>', etc.</para></remarks>
        public bool IsMatch(string? template, string? actual, char seperator = '/')
        {
            if (string.IsNullOrEmpty(template) || template == "*" || template == actual)
                return true;

            if (string.IsNullOrEmpty(actual))
                return false;

            var tparts = template.Split(seperator);
            var aparts = actual.Split(seperator);

            if (tparts.Length > aparts.Length)
                return false;

            for (int i = 0; i < tparts.Length; i++)
            {
                if (i > aparts.Length)
                    return false;

                if (!Wildcard.Default.Parse(tparts[i]).CreateRegex(IgnoreCase).IsMatch(aparts[i]))
                    return false;
            }

            if (aparts.Length > tparts.Length && tparts.Last() != "**")
                return false;

            return true;
        }

        /// <summary>
        /// Matches the source URI.
        /// </summary>
        private bool IsMatchSource(Uri? template, Uri actual)
        {
            if (template == null || (!template.IsAbsoluteUri && template.OriginalString == "*"))
                return true;

            if (!template.IsAbsoluteUri)
                return IsMatch(template.OriginalString, RemoveLeadingSeperator(actual.IsAbsoluteUri ? actual.AbsolutePath : actual.OriginalString, '/'), '/');

            if (!actual.IsAbsoluteUri)
                return false;

            if (template.Host != actual.Host || template.HostNameType != actual.HostNameType || template.Port != actual.Port || template.Scheme != actual.Scheme)
                return false;

            return IsMatch(RemoveLeadingSeperator(template.AbsolutePath, '/'), RemoveLeadingSeperator(actual.AbsolutePath, '/'), '/');
        }

        /// <summary>
        /// Removes the leading seperator.
        /// </summary>
        private static string RemoveLeadingSeperator(string text, char seperator) => text.StartsWith(seperator) ? text[1..] : text;
    }
}