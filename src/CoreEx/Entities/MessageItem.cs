﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities.Extended;
using CoreEx.Localization;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CoreEx.Entities
{
    /// <summary>
    /// Represents a <see cref="MessageItem"/>.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Type = {Type}, Text = {Text}, Property = {Property}")]
    [System.Diagnostics.DebuggerStepThrough]
    public class MessageItem : EntityBase
    {
        private MessageType _type;
        private string? _text;
        private string? _property;
        private object? _tag;

        #region Static

        /// <summary>
        /// Creates a new <see cref="MessageItem"/> with a specified <see cref="MessageType"/> and text.
        /// </summary>
        /// <param name="type">The <see cref="MessageType"/>.</param>
        /// <param name="text">The message text.</param>
        /// <returns>A <see cref="MessageItem"/>.</returns>
        public static MessageItem CreateMessage(MessageType type, LText text) => new() { Type = type, Text = text };

        /// <summary>
        /// Creates a new <see cref="MessageItem"/> with a specified <see cref="MessageType"/>, text format and and additional values included in the text.
        /// </summary>
        /// <param name="type">The <see cref="MessageType"/>.</param>
        /// <param name="format">The composite format string.</param>
        /// <param name="values">The values that form part of the message text.</param>
        /// <returns>A <see cref="MessageItem"/>.</returns>
        public static MessageItem CreateMessage(MessageType type, LText format, params object[] values) => new() { Type = type, Text = string.Format(System.Globalization.CultureInfo.CurrentCulture, format, values) };

        /// <summary>
        /// Creates a new <see cref="MessageItem"/> with the specified <see cref="Property"/>, <see cref="MessageType"/> and text.
        /// </summary>
        /// <param name="property">The property name.</param>
        /// <param name="type">The <see cref="MessageType"/>.</param>
        /// <param name="text">The message text.</param>
        /// <returns>A <see cref="MessageItem"/>.</returns>
        public static MessageItem CreateMessage(string? property, MessageType type, LText text) => new() { Property = property, Type = type, Text = text };

        /// <summary>
        /// Creates a new <see cref="MessageItem"/> with the specified <see cref="Property"/>, <see cref="MessageType"/>, text format and additional values included in the text.
        /// </summary>
        /// <param name="property">The property name.</param>
        /// <param name="type">The <see cref="MessageType"/>.</param>
        /// <param name="format">The composite format string.</param>
        /// <param name="values">The values that form part of the message text.</param>
        /// <returns>A <see cref="MessageItem"/>.</returns>
        public static MessageItem CreateMessage(string? property, MessageType type, LText format, params object?[] values)
            => new() { Property = property, Type = type, Text = string.Format(System.Globalization.CultureInfo.CurrentCulture, format, values) };

        /// <summary>
        /// Creates a new <see cref="MessageType.Error"/> <see cref="MessageItem"/> with the specified <see cref="Property"/> and text.
        /// </summary>
        /// <param name="property">The property name.</param>
        /// <param name="text">The message text.</param>
        /// <returns>A <see cref="MessageItem"/>.</returns>
        public static MessageItem CreateErrorMessage(string? property, LText text) => new() { Property = property, Type = MessageType.Error, Text = text };

        /// <summary>
        /// Creates a new <see cref="MessageType.Error"/> <see cref="MessageItem"/> with the specified <see cref="Property"/>, text format and additional values included in the text.
        /// </summary>
        /// <param name="property">The property name.</param>
        /// <param name="format">The composite format string.</param>
        /// <param name="values">The values that form part of the message text.</param>
        /// <returns>A <see cref="MessageItem"/>.</returns>
        public static MessageItem CreateErrorMessage(string property, LText format, params object?[] values) 
            => new() { Property = property, Type = MessageType.Error, Text = string.Format(System.Globalization.CultureInfo.CurrentCulture, format, values) };

        #endregion

        /// <summary>
        /// Gets the message severity validatorType.
        /// </summary>
        public MessageType Type { get => _type; set => SetValue(ref _type, value); }

        /// <summary>
        /// Gets or sets the message text.
        /// </summary>
        public string? Text { get => _text; set => SetValue(ref _text, value); }

        /// <summary>
        /// Gets or sets the name of the property that the message relates to.
        /// </summary>
        public string? Property { get => _property; set => SetValue(ref _property, value); }

        /// <summary>
        /// Gets or sets an optional user tag associated with the message.
        /// </summary>
        /// <remarks>Note: This property is not serialized/deserialized.</remarks>
        [JsonIgnore]
        public object? Tag { get => _tag; set => SetValue(ref _tag, value); }

        /// <summary>
        /// Returns the message <see cref="Text"/>.
        /// </summary>
        /// <returns>The message <see cref="Text"/>.</returns>
        public override string ToString() => Text ?? string.Empty;

        /// <summary>
        /// Sets the <see cref="Property"/> and returns <see cref="MessageItem"/> instance to enable fluent-style.
        /// </summary>
        /// <param name="property">The name of the property that the message relates to.</param>
        /// <returns>This <see cref="MessageItem"/> instance.</returns>
        public MessageItem SetProperty(string property)
        {
            Property = property;
            return this;
        }

        /// <inheritdoc/>
        protected override IEnumerable<IPropertyValue> GetPropertyValues()
        {
            yield return CreateProperty(nameof(Type), Type, v => Type = v);
            yield return CreateProperty(nameof(Text), Text, v => Text = v);
            yield return CreateProperty(nameof(Property), Property, v => Property = v);
            yield return CreateProperty(nameof(Tag), Tag, v => Tag = v);
        }
    }
}