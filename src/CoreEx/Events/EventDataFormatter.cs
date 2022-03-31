// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Globalization;
using CoreEx.Json;
using CoreEx.Utility;
using System;
using System.Globalization;

namespace CoreEx.Events
{
    /// <summary>
    /// Provides the <see cref="EventData"/> formatting options and corresponding <see cref="Format(EventDataBase)"/>.
    /// </summary>
    /// <remarks>This enables further standardized formatting of the <see cref="EventData"/> prior to serialization.
    /// <para>The <see cref="EventDataBase.Id"/>, <see cref="EventDataBase.Timestamp"/> and <see cref="EventDataBase.CorrelationId"/> will default, where <c>null</c>, to <see cref="Guid.NewGuid()"/>, <see cref="DateTimeOffset.UtcNow"/> and
    /// <see cref="ExecutionContext.Current"/> <see cref="ExecutionContext.CorrelationId"/> respectively.</para></remarks>
    public class EventDataFormatter
    {
        /// <summary>
        /// Gets or sets the <see cref="EventData"/> property selection; where a property is not selected its value will be reset to <c>null</c>.
        /// </summary>
        /// <remarks>Defaults to <see cref="EventDataProperty.All"/>.</remarks>
        public EventDataProperty PropertySelection { get; set; } = EventDataProperty.All;

        /// <summary>
        /// Gets or sets the <see cref="System.Globalization.TextInfo"/> to manage the underlying <see cref="TextInfoCasing"/> applications.
        /// </summary>
        public TextInfo TextInfo { get; set; } = CultureInfo.InvariantCulture.TextInfo;

        /// <summary>
        /// Gets or sets the <see cref="EventDataBase.Type"/> value casing conversion.
        /// </summary>
        /// <remarks>Defaults to <see cref="TextInfoCasing.Lower"/>.</remarks>
        public TextInfoCasing TypeCasing = TextInfoCasing.Lower;

        /// <summary>
        /// Indicates whether to append the <see cref="IIdentifier.Id"/> or <see cref="IPrimaryKey.PrimaryKey"/> to the <see cref="EventDataBase.Type"/> value.
        /// </summary>
        public bool TypeAppendIdOrPrimaryKey { get; set; } = false;

        /// <summary>
        /// Gets or sets the <see cref="EventDataBase.Type"/> separator character.
        /// </summary>
        /// <remarks>Defaults to '<c>.</c>'.</remarks>
        public char TypeSeparatorCharacter { get; set; } = '.';

        /// <summary>
        /// Indicates whether to default the <see cref="EventDataBase.Type"/> to the <see cref="EventDataBase.GetValue"/> value <see cref="Type.FullName"/> where <c>null</c>.
        /// </summary>
        public bool TypeDefaultToValueTypeName { get; set; } = true;

        /// <summary>
        /// Gets or sets the <see cref="EventDataBase.Subject"/> value casing conversion.
        /// </summary>
        /// <remarks>Defaults to <see cref="TextInfoCasing.Lower"/>.</remarks>
        public TextInfoCasing SubjectCasing = TextInfoCasing.Lower;

        /// <summary>
        /// Indicates whether to append the <see cref="IIdentifier.Id"/> or <see cref="IPrimaryKey.PrimaryKey"/> to the <see cref="EventDataBase.Subject"/> value.
        /// </summary>
        public bool SubjectAppendIdOrPrimaryKey { get; set; } = false;

        /// <summary>
        /// Gets or sets the <see cref="EventDataBase.Subject"/> separator character.
        /// </summary>
        /// <remarks>Defaults to '<c>.</c>'.</remarks>
        public char SubjectSeparatorCharacter { get; set; } = '.';

        /// <summary>
        /// Indicates whether to default the <see cref="EventDataBase.Subject"/> to the <see cref="EventDataBase.GetValue"/> value <see cref="Type.FullName"/> where <c>null</c>.
        /// </summary>
        public bool SubjectDefaultToValueTypeName { get; set; } = false;

        /// <summary>
        /// Gets or sets the <see cref="EventDataBase.Action"/> value casing conversion.
        /// </summary>
        /// <remarks>Defaults to <see cref="TextInfoCasing.Lower"/>.</remarks>
        public TextInfoCasing ActionCasing = TextInfoCasing.Lower;

        /// <summary>
        /// Gets or sets the default <see cref="EventDataBase.Source"/> where not specified.
        /// </summary>
        /// <remarks>Defaults to the <see cref="string"/> literal '<c>null</c>'.</remarks>
        public Uri? SourceDefault { get; set; } = new Uri("null", UriKind.Relative);

        /// <summary>
        /// Indicates whether to default the <see cref="EventDataBase.ETag"/> to the <see cref="EventDataBase.GetValue"/> value where it implements <see cref="IETag"/>.
        /// </summary>
        /// <remarks>This is applied before <see cref="ETagDefaultGenerated"/>.</remarks>
        public bool ETagDefaultFromValue { get; set; }

        /// <summary>
        /// Indicates whether to default the <see cref="EventDataBase.ETag"/> to the <see cref="EventDataBase.GetValue"/> value by using the <see cref="ETagGenerator"/>.
        /// </summary>
        /// <remarks>This is applied after <see cref="ETagDefaultFromValue"/>.</remarks>
        public bool ETagDefaultGenerated { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IJsonSerializer"/>.
        /// </summary>
        /// <remarks>Required for the <see cref="ETagDefaultGenerated"/>.</remarks>
        public IJsonSerializer? JsonSerializer { get; set; }

        /// <summary>
        /// Formats the <paramref name="event"/> using the configured formatting options where applicable.
        /// </summary>
        /// <param name="event">The <see cref="EventDataBase"/> to format.</param>
        public virtual void Format(EventDataBase @event)
        {
            var value = @event.GetValue();

            if (@event.Id == null)
                @event.Id = Guid.NewGuid().ToString();

            if (@event.Timestamp == null)
                @event.Timestamp = DateTimeOffset.UtcNow;

            if (@event.CorrelationId == null)
                @event.CorrelationId = ExecutionContext.Current.CorrelationId;

            if (PropertySelection.HasFlag(EventDataProperty.Subject))
            {
                if (@event.Subject == null && SubjectDefaultToValueTypeName && value != null)
                    @event.Subject = TextInfo.ToCasing(GetDataType(value, SubjectSeparatorCharacter), SubjectCasing);
                else if (@event.Subject != null && SubjectCasing != TextInfoCasing.None)
                    @event.Subject = TextInfo.ToCasing(@event.Subject, SubjectCasing);

                if (SubjectAppendIdOrPrimaryKey && value != null)
                {
                    if (value is IIdentifier ii)
                        @event.Subject = Concatenate(@event.Subject, SubjectSeparatorCharacter, new CompositeKey(ii.Id).ToString());
                    else if (value is IPrimaryKey pk)
                        @event.Subject = Concatenate(@event.Subject, SubjectSeparatorCharacter, pk.PrimaryKey.ToString());
                }
            }
            else
                @event.Subject = null;

            if (PropertySelection.HasFlag(EventDataProperty.Action))
            {
                if (@event.Action != null && ActionCasing != TextInfoCasing.None)
                    @event.Action = TextInfo.ToCasing(@event.Action, ActionCasing);
            }
            else
                @event.Action = null;

            if (PropertySelection.HasFlag(EventDataProperty.Type))
            {
                if (@event.Type == null && TypeDefaultToValueTypeName && value != null)
                    @event.Type = TextInfo.ToCasing(GetDataType(value, TypeSeparatorCharacter), TypeCasing);
                else if (@event.Type != null && TypeCasing != TextInfoCasing.None)
                    @event.Type = TextInfo.ToCasing(@event.Type, TypeCasing);

                if (TypeAppendIdOrPrimaryKey && @event.GetValue() != null)
                {
                    if (value is IIdentifier ii)
                        @event.Type = Concatenate(@event.Type, TypeSeparatorCharacter, new CompositeKey(ii.Id).ToString());
                    else if (value is IPrimaryKey pk)
                        @event.Type = Concatenate(@event.Type, TypeSeparatorCharacter, pk.PrimaryKey.ToString());
                }
            }
            else
                @event.Type = null;

            if (PropertySelection.HasFlag(EventDataProperty.Source))
            {
                if (@event.Source == null)
                    @event.Source = SourceDefault;
            }
            else
                @event.Source = null;

            if (!PropertySelection.HasFlag(EventDataProperty.CorrelationId))
                @event.CorrelationId = null;

            if (!PropertySelection.HasFlag(EventDataProperty.TenantId))
                @event.TenantId = null;

            if (!PropertySelection.HasFlag(EventDataProperty.PartitionKey))
                @event.PartitionKey = null;

            if (PropertySelection.HasFlag(EventDataProperty.ETag))
            {
                if (@event.ETag == null)
                {
                    if (ETagDefaultFromValue && value != null && value is IETag etag)
                        @event.ETag = etag.ETag;

                    if (@event.ETag == null && ETagDefaultGenerated && value != null)
                        @event.ETag = ETagGenerator.Generate(JsonSerializer ?? throw new InvalidOperationException($"The {nameof(JsonSerializer)} must be provided for the {nameof(ETagDefaultGenerated)} to function."), value);
                }
            }
            else
                @event.ETag = null;

            if (!PropertySelection.HasFlag(EventDataProperty.Attributes))
                @event.Attributes = null;
        }

        /// <summary>
        /// Gets the formatted EventData.Data Type name.
        /// </summary>
        private static string? GetDataType(object? val, char separator)
        {
            if (val == null)
                return null;

            var name = val.GetType().FullName;
            return separator == '.' ? name : name?.Replace('.', separator);
        }

        /// <summary>
        /// Concatenate to make new string value.
        /// </summary>
        private static string? Concatenate(string? left, char separator, string? right)
        {
            if (string.IsNullOrEmpty(left))
                return right;
            else if (string.IsNullOrEmpty(right))
                return left;
            else
                return string.Concat(left, separator, right);
        }
    }
}