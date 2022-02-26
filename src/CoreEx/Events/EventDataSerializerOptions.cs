// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Globalization;
using CoreEx.Json;
using CoreEx.Utility;
using System;
using System.Globalization;

namespace CoreEx.Events
{
    /// <summary>
    /// Provides the <see cref="EventData"/> serializer options.
    /// </summary>
    public class EventDataSerializerOptions
    {
        /// <summary>
        /// Gets or sets the <see cref="IJsonSerializer"/>.
        /// </summary>
        public IJsonSerializer? JsonSerializer { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="EventData"/> property selection for serialization and deserialization.
        /// </summary>
        /// <remarks>Defaults to <see cref="EventDataProperty.All"/>.</remarks>
        public EventDataProperty PropertySelection { get; set; } = EventDataProperty.All;

        /// <summary>
        /// Gets or sets the <see cref="System.Globalization.TextInfo"/> to manage the underlying <see cref="TextInfoCasing"/> applications.
        /// </summary>
        public TextInfo TextInfo { get; set; } = CultureInfo.InvariantCulture.TextInfo;

        /// <summary>
        /// Gets or sets the <see cref="EventData.Type"/> value casing conversion.
        /// </summary>
        /// <remarks>Defaults to <see cref="TextInfoCasing.Lower"/>.</remarks>
        public TextInfoCasing TypeValueCasing = TextInfoCasing.Lower;

        /// <summary>
        /// Gets or sets the <see cref="EventData.Subject"/> value casing conversion.
        /// </summary>
        /// <remarks>Defaults to <see cref="TextInfoCasing.Lower"/>.</remarks>
        public TextInfoCasing SubjectValueCasing = TextInfoCasing.Lower;

        /// <summary>
        /// Gets or sets the <see cref="EventData.Action"/> value casing conversion.
        /// </summary>
        /// <remarks>Defaults to <see cref="TextInfoCasing.Lower"/>.</remarks>
        public TextInfoCasing ActionValueCasing = TextInfoCasing.Lower;

        /// <summary>
        /// Indicates whether to default the <see cref="EventData.Type"/> to the <see cref="EventData.Data"/> value <see cref="Type.FullName"/> where <c>null</c>.
        /// </summary>
        public bool DefaultTypeToDataTypeName { get; set; } = true;

        /// <summary>
        /// Gets or sets the default <see cref="EventData.Source"/> where not specified.
        /// </summary>
        /// <remarks>Defaults to the <see cref="string"/> literal '<c>null</c>'.</remarks>
        public Uri? DefaultSource { get; set; } = new Uri("null", UriKind.Relative);

        /// <summary>
        /// Indicates whether to default the <see cref="EventData.Subject"/> to the <see cref="EventData.Data"/> value <see cref="Type.FullName"/> where <c>null</c>.
        /// </summary>
        public bool DefaultSubjectToDataTypeName { get; set; } = false;

        /// <summary>
        /// Indicates whether to default the <see cref="EventData.ETag"/> to the <see cref="EventData"/> value where it implements <see cref="IETag"/>.
        /// </summary>
        /// <remarks>This is applied before <see cref="DefaultETagFromDataGenerated"/>.</remarks>
        public bool DefaultETagFromDataValue { get; set; }

        /// <summary>
        /// Indicates whether to default the <see cref="EventData.ETag"/> to the <see cref="EventData"/> value by using the <see cref="ETagGenerator"/>.
        /// </summary>
        /// <remarks>This is applied after <see cref="DefaultETagFromDataValue"/>.</remarks>
        public bool DefaultETagFromDataGenerated { get; set; }

        /// <summary>
        /// Applies the selected options where applicable.
        /// </summary>
        /// <param name="ed">The <see cref="EventData"/> to apply to.</param>
        public void Apply(EventData ed)
        {
            if (ed.Type == null && DefaultTypeToDataTypeName && ed.Data != null)
                ed.Type = TextInfo.ToCasing(ed.Data.GetType().FullName, TypeValueCasing);
            else if (ed.Type != null && TypeValueCasing != TextInfoCasing.None)
                ed.Type = TextInfo.ToCasing(ed.Type, TypeValueCasing);

            if (ed.Source == null)
                ed.Source = DefaultSource;

            if (ed.Subject == null && DefaultSubjectToDataTypeName && ed.Data != null)
                ed.Subject = TextInfo.ToCasing(ed.Data.GetType().FullName, SubjectValueCasing);
            else if (ed.Subject != null && SubjectValueCasing != TextInfoCasing.None)
                ed.Subject = TextInfo.ToCasing(ed.Subject, SubjectValueCasing);

            if (ed.Action != null && ActionValueCasing != TextInfoCasing.None)
                ed.Action = TextInfo.ToCasing(ed.Action, ActionValueCasing);

            if (ed.ETag == null)
            {
                if (DefaultETagFromDataValue && ed.Data != null && ed.Data is IETag etag)
                    ed.ETag = etag.ETag;

                if (ed.ETag == null && DefaultETagFromDataGenerated && ed.Data != null)
                    ed.ETag = ETagGenerator.Generate(JsonSerializer ?? throw new InvalidOperationException($"The {nameof(JsonSerializer)} must be provided for the {nameof(DefaultETagFromDataGenerated)} to function."), ed.Data);
            }
        }
    }
}