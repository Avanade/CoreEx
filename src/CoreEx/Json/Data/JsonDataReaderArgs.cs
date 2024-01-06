// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.RefData;
using System;
using System.Collections.Generic;

namespace CoreEx.Json.Data
{
    /// <summary>
    /// Represents the <see cref="JsonDataReader"/> arguments.
    /// </summary>
    public class JsonDataReaderArgs
    {
        /// <summary>
        /// Gets the <c>UserName</c> <see cref="Parameters"/> key.
        /// </summary>
        public const string UserNameKey = "UserName";

        /// <summary>
        /// Gets the <c>DateTimeNow</c> <see cref="Parameters"/> key.
        /// </summary>
        public const string DateTimeNowKey = "DateTimeNow";

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonDataReaderArgs"/> class.
        /// </summary>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
        /// <param name="username">The user name. Defaults to '<c><see cref="Environment.UserDomainName"/>\<see cref="Environment.UserName"/></c>'.</param>
        /// <param name="dateTimeNow">The current <see cref="DateTime"/>. Defaults to <see cref="DateTime.UtcNow"/>.</param>
        /// <remarks>The <paramref name="jsonSerializer"/> defaults to a new <see cref="Text.Json.JsonSerializer"/> instance with a <see cref="Text.Json.NumberToStringConverter"/> added to the default <see cref="System.Text.Json.JsonSerializerOptions"/>
        /// to support numbers specified as strings which YAML is more permissive with.</remarks>
        public JsonDataReaderArgs(IJsonSerializer? jsonSerializer = null, string? username = null, DateTime? dateTimeNow = null)
        {
            Parameters.Add(UserNameKey, username ?? (Environment.UserDomainName == null ? Environment.UserName : $"{Environment.UserDomainName}\\{Environment.UserName}"));
            Parameters.Add(DateTimeNowKey, dateTimeNow ?? DateTime.UtcNow);

            RefDataColumnDefaults.Add(nameof(IReferenceData.IsActive), _ => true);
            RefDataColumnDefaults.Add(nameof(IReferenceData.SortOrder), i => i + 1);

            if (jsonSerializer is null)
            {
                var jo = new System.Text.Json.JsonSerializerOptions(Text.Json.JsonSerializer.DefaultOptions);
                jo.Converters.Add(new Text.Json.NumberToStringConverter());
                JsonSerializer = new Text.Json.JsonSerializer(jo);
            }
            else
                JsonSerializer = jsonSerializer;
        }

        /// <summary>
        /// Gets or sets the <see cref="IJsonSerializer"/>.
        /// </summary>
        public IJsonSerializer JsonSerializer { get; }

        /// <summary>
        /// Indiates whether to replace '<c>^n</c>' values where '<c>n</c>' is an integer with a <see cref="Guid"/> equivalent; e.g. '<c>^1</c>' will be '<c>00000001-0000-0000-0000-000000000000</c>'
        /// </summary>
        public bool ReplaceShorthandGuids { get; set; } = true;

        /// <summary>
        /// Gets or sets the <see cref="IIdentifierGenerator"/>.
        /// </summary>
        /// <remarks>Defaults to <see cref="IdentifierGenerator"/>.</remarks>
        public IIdentifierGenerator? IdentifierGenerator { get; set; } = new IdentifierGenerator();

        /// <summary>
        /// Gets or sets the reference data property defaults dictionary.
        /// </summary>
        /// <remarks>The dictionary should contain the property name and corresponding function that returns the default value; the input to the function is the item index (zero-based).
        /// <para>Defaults following properties:
        /// <list type="bullet">
        ///   <item><see cref="IReferenceData.IsActive"/> with function '<c>_ => true</c>' (always <c>true</c>).</item>
        ///   <item><see cref="IReferenceData.SortOrder"/> with function '<c>i => i + 1</c>' (increment by 1 from 1).</item>
        /// </list>
        /// </para></remarks>
        public Dictionary<string, Func<int, object?>> RefDataColumnDefaults { get; } = [];

        /// <summary>
        /// Gets the runtime parameters.
        /// </summary>
        public Dictionary<string, object?> Parameters { get; } = [];
    }
}