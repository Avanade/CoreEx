// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Entities;
using CoreEx.Globalization;
using CoreEx.Json;
using System;
using System.Globalization;

namespace CoreEx.Events
{
    /// <summary>
    /// Provides the <see cref="EventData"/> formatting options and corresponding <see cref="Format(EventData)"/>.
    /// </summary>
    /// <remarks>This enables further standardized formatting of the <see cref="EventData"/> prior to serialization.
    /// <para>The <see cref="EventDataBase.Id"/>, <see cref="EventDataBase.Timestamp"/> and <see cref="EventDataBase.CorrelationId"/> will default, where <c>null</c>, to <see cref="Guid.NewGuid()"/>, <see cref="DateTimeOffset.UtcNow"/> and
    /// <see cref="ExecutionContext.Current"/> <see cref="ExecutionContext.CorrelationId"/> respectively.</para></remarks>
    public class EventDataFormatter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventDataFormatter"/> class.
        /// </summary>
        public EventDataFormatter() => TypeDefault = _ => TypeDefaultToValueTypeName ? "None" : null;

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
        /// Indicates whether to append the <see cref="EventDataBase.Key"/> to the <see cref="EventDataBase.Type"/> value.
        /// </summary>
        /// <remarks>This is applied before <see cref="TypeAppendEntityKey"/>.</remarks>
        public bool TypeAppendKey { get; set; } = false;

        /// <summary>
        /// Indicates whether to append the <see cref="IEntityKey.EntityKey"/> to the <see cref="EventDataBase.Type"/> value.
        /// </summary>
        /// <remarks>This is applied after <see cref="TypeAppendKey"/>.</remarks>
        public bool TypeAppendEntityKey { get; set; } = false;

        /// <summary>
        /// Gets or sets the <see cref="EventDataBase.Type"/> separator character.
        /// </summary>
        /// <remarks>Defaults to '<c>.</c>'.</remarks>
        public char TypeSeparatorCharacter { get; set; } = '.';

        /// <summary>
        /// Indicates whether to default the <see cref="EventDataBase.Type"/> to the <see cref="EventData.Value"/> value <see cref="Type.FullName"/> where <c>null</c>.
        /// </summary>
        public bool TypeDefaultToValueTypeName { get; set; } = true;

        /// <summary>
        /// Gets or sets the <see cref="EventDataBase.Subject"/> value casing conversion.
        /// </summary>
        /// <remarks>Defaults to <see cref="TextInfoCasing.Lower"/>.</remarks>
        public TextInfoCasing SubjectCasing = TextInfoCasing.Lower;

        /// <summary>
        /// Indicates whether to append the <see cref="EventDataBase.Key"/> to the <see cref="EventDataBase.Subject"/> value.
        /// </summary>
        /// <remarks>This is applied before <see cref="SubjectAppendEntityKey"/>.</remarks>
        public bool SubjectAppendKey { get; set; } = false;

        /// <summary>
        /// Indicates whether to append the <see cref="IEntityKey.EntityKey"/> to the <see cref="EventDataBase.Subject"/> value.
        /// </summary>
        /// <remarks>This is applied after <see cref="SubjectAppendKey"/>.</remarks>
        public bool SubjectAppendEntityKey { get; set; } = false;

        /// <summary>
        /// Gets or sets the <see cref="EventDataBase.Subject"/> separator character.
        /// </summary>
        /// <remarks>Defaults to '<c>.</c>'.</remarks>
        public char SubjectSeparatorCharacter { get; set; } = '.';

        /// <summary>
        /// Indicates whether to default the <see cref="EventDataBase.Subject"/> to the <see cref="EventData.Value"/> value <see cref="Type.FullName"/> where <c>null</c>.
        /// </summary>
        public bool SubjectDefaultToValueTypeName { get; set; } = false;

        /// <summary>
        /// Gets or sets the <see cref="EventDataBase.Action"/> value casing conversion.
        /// </summary>
        /// <remarks>Defaults to <see cref="TextInfoCasing.Lower"/>.</remarks>
        public TextInfoCasing ActionCasing = TextInfoCasing.Lower;

        /// <summary>
        /// Gets or sets the <see cref="EventDataBase.Source"/> default delegate to be used where not specified.
        /// </summary>
        /// <remarks>Defaults to <c>null</c>.</remarks>
        public Func<EventData, Uri?>? SourceDefault { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="EventDataBase.Type"/> default delegate to be used where not specified.
        /// </summary>
        /// <remarks>Defaults to a function that returns '<c>None</c>' when <see cref="TypeDefaultToValueTypeName"/> is <c>true</c>.</remarks>
        public Func<EventData, string?>? TypeDefault { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="EventDataBase.Key"/> separator character.
        /// </summary>
        /// <remarks>Defaults to '<c>,</c>'.</remarks>
        public char KeySeparatorCharacter { get; set; } = ',';

        /// <summary>
        /// Indicates whether to default the <see cref="EventDataBase.ETag"/> to the <see cref="EventData.Value"/> value where it implements <see cref="IETag"/>.
        /// </summary>
        /// <remarks>This is applied before <see cref="ETagDefaultGenerated"/>.</remarks>
        public bool ETagDefaultFromValue { get; set; } = true;

        /// <summary>
        /// Indicates whether to default the <see cref="EventDataBase.ETag"/> to the <see cref="EventData.Value"/> value by using the <see cref="ETagGenerator"/>.
        /// </summary>
        /// <remarks>This is applied after <see cref="ETagDefaultFromValue"/>.</remarks>
        public bool ETagDefaultGenerated { get; set; } = false;

        /// <summary>
        /// Indicates whether to default the <see cref="EventDataBase.PartitionKey"/> to the <see cref="EventData.Value"/> value where it implements <see cref="IPartitionKey"/>.
        /// </summary>
        public bool PartitionKeyDefaultFromValue { get; set; } = true;

        /// <summary>
        /// Indicates whether to default the <see cref="EventDataBase.TenantId"/> to the <see cref="EventData.Value"/> value where it implements <see cref="ITenantId"/>.
        /// </summary>
        public bool TenantIdDefaultFromValue { get; set; } = true;

        /// <summary>
        /// Gets or sets the <see cref="IJsonSerializer"/>.
        /// </summary>
        /// <remarks>Required for the <see cref="ETagDefaultGenerated"/>.</remarks>
        public IJsonSerializer? JsonSerializer { get; set; }

        /// <summary>
        /// Formats the <paramref name="event"/> using the configured formatting options where applicable.
        /// </summary>
        /// <param name="event">The <see cref="EventData"/> to format.</param>
        public virtual void Format(EventData @event)
        {
            var value = @event.Value;

            @event.Id ??= Guid.NewGuid().ToString();
            @event.Timestamp ??= DateTimeOffset.UtcNow;

            if (PropertySelection.HasFlag(EventDataProperty.Key))
            {
                if (@event.Key is null && value is not null && value is IEntityKey ek)
                    @event.Key = ek.EntityKey.ToString(KeySeparatorCharacter);
            }

            if (PropertySelection.HasFlag(EventDataProperty.Subject))
            {
                if (@event.Subject == null && SubjectDefaultToValueTypeName && value != null)
                    @event.Subject = TextInfo.ToCasing(GetDataType(value, SubjectSeparatorCharacter), SubjectCasing);
                else if (@event.Subject != null && SubjectCasing != TextInfoCasing.None)
                    @event.Subject = TextInfo.ToCasing(@event.Subject, SubjectCasing);

                if (SubjectAppendKey && @event.Key != null)
                    @event.Subject = Concatenate(@event.Subject, SubjectSeparatorCharacter, @event.Key);
                else if (SubjectAppendEntityKey && value is IEntityKey ek)
                    @event.Subject = Concatenate(@event.Subject, SubjectSeparatorCharacter, ek.EntityKey.ToString(KeySeparatorCharacter));
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

                if (TypeAppendKey && @event.Key != null)
                    @event.Type = Concatenate(@event.Type, TypeSeparatorCharacter, @event.Key);
                else if (TypeAppendEntityKey && value is IEntityKey ek)
                    @event.Type = Concatenate(@event.Type, TypeSeparatorCharacter, ek.EntityKey.ToString(KeySeparatorCharacter));

                @event.Type ??= TextInfo.ToCasing(TypeDefault?.Invoke(@event), TypeCasing);
            }
            else
                @event.Type = null;

            if (PropertySelection.HasFlag(EventDataProperty.CorrelationId))
                @event.CorrelationId ??= ExecutionContext.HasCurrent ? ExecutionContext.Current.CorrelationId : @event.Id;
            else
                @event.CorrelationId = null;

            if (PropertySelection.HasFlag(EventDataProperty.TenantId))
            {
                if (@event.TenantId is null && TenantIdDefaultFromValue && value is not null && value is ITenantId tid)
                    @event.TenantId = tid.TenantId;
            }
            else
                @event.TenantId = null;

            if (PropertySelection.HasFlag(EventDataProperty.PartitionKey))
            {
                if (@event.PartitionKey is null && PartitionKeyDefaultFromValue && value is not null && value is IPartitionKey pk)
                    @event.PartitionKey = pk.PartitionKey;
            }
            else
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

            if (!PropertySelection.HasFlag(EventDataProperty.Attributes) && @event.HasAttributes)
                @event.Attributes = null;

            if (PropertySelection.HasFlag(EventDataProperty.Source))
                @event.Source ??= SourceDefault?.Invoke(@event);
            else
                @event.Source = null;

            if (!PropertySelection.HasFlag(EventDataProperty.Key))
                @event.Key = null;

            if (@event.Value is not null && @event.Value is IEventDataFormatter formattable)
                formattable.Format(@event);
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