// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Localization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace CoreEx.Entities
{
    /// <summary>
    /// Represents a <see cref="MessageItem"/> collection.
    /// </summary>
    [System.Diagnostics.DebuggerStepThrough]
    public class MessageItemCollection : ObservableCollection<MessageItem>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageItemCollection" /> class.
        /// </summary>
        public MessageItemCollection() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageItemCollection" /> class.
        /// </summary>
        /// <param name="messages">Initial messages to add.</param>
        public MessageItemCollection(IEnumerable<MessageItem> messages) : base(messages) { }

        /// <summary>
        /// Adds zero or more <paramref name="messages"/> to the collection.
        /// </summary>
        /// <param name="messages">The messages.</param>
        public void AddRange(IEnumerable<MessageItem> messages) => messages.ForEach(Add);

        /// <summary>
        /// Adds a new <see cref="MessageItem"/> for a specified <see cref="MessageType"/> and text.
        /// </summary>
        /// <param name="type">The <see cref="MessageType"/>.</param>
        /// <param name="text">The message text.</param>
        /// <returns>A <see cref="MessageItem"/>.</returns>
        public MessageItem Add(MessageType type, LText text)
        {
            MessageItem item = MessageItem.CreateMessage(type, text);
            this.Add(item);
            return item;
        }

        /// <summary>
        /// Adds a new <see cref="MessageItem"/> for a specified <see cref="MessageType"/>, text format and additional values included in the text.
        /// </summary>
        /// <param name="type">The <see cref="MessageType"/>.</param>
        /// <param name="format">The composite format string.</param>
        /// <param name="values">The values that form part of the message text.</param>
        /// <returns>A <see cref="MessageItem"/>.</returns>
        public MessageItem Add(MessageType type, LText format, params object[] values)
        {
            MessageItem item = MessageItem.CreateMessage(type, format, values);
            this.Add(item);
            return item;
        }

        /// <summary>
        /// Adds a new <see cref="MessageItem"/> for the specified <see cref="MessageItem.Property"/>, <see cref="MessageType"/> and text.
        /// </summary>
        /// <param name="property">The property name.</param>
        /// <param name="type">The <see cref="MessageType"/>.</param>
        /// <param name="text">The message text.</param>
        /// <returns>A <see cref="MessageItem"/>.</returns>
        public MessageItem Add(string? property, MessageType type, LText text)
        {
            MessageItem item = MessageItem.CreateMessage(property, type, text);
            this.Add(item);
            return item;
        }

        /// <summary>
        /// Adds a new <see cref="MessageItem"/> for the specified <see cref="MessageItem.Property"/>, <see cref="MessageType"/>, text format and additional values included in the text.
        /// </summary>
        /// <param name="property">The property name.</param>
        /// <param name="type">The <see cref="MessageType"/>.</param>
        /// <param name="format">The composite format string.</param>
        /// <param name="values">The values that form part of the message text.</param>
        /// <returns>A <see cref="MessageItem"/>.</returns>
        public MessageItem Add(string? property, MessageType type, LText format, params object?[] values)
        {
            MessageItem item = MessageItem.CreateMessage(property, type, format, values);
            this.Add(item);
            return item;
        }

        /// <summary>
        /// Adds a new <see cref="MessageType.Error"/> <see cref="MessageItem"/> for a specified text.
        /// </summary>
        /// <param name="text">The message text.</param>
        /// <returns>A <see cref="MessageItem"/>.</returns>
        public MessageItem AddError(LText text) => Add(MessageType.Error, text);

        /// <summary>
        /// Adds a new <see cref="MessageType.Error"/> <see cref="MessageItem"/> for a specified text format and additional values included in the text.
        /// </summary>
        /// <param name="format">The composite format string.</param>
        /// <param name="values">The values that form part of the message text.</param>
        /// <returns>A <see cref="MessageItem"/>.</returns>
        public MessageItem AddError(LText format, params object[] values) => Add(MessageType.Error, format, values);

        /// <summary>
        /// Adds a new <see cref="MessageType.Error"/> <see cref="MessageItem"/> for the specified <see cref="MessageItem.Property"/> and text.
        /// </summary>
        /// <param name="property">The property name.</param>
        /// <param name="text">The message text.</param>
        /// <returns>A <see cref="MessageItem"/>.</returns>
        public MessageItem AddPropertyError(string property, LText text) => Add(property, MessageType.Error, text);

        /// <summary>
        /// Adds a new <see cref="MessageType.Error"/> <see cref="MessageItem"/> for the specified <see cref="MessageItem.Property"/>, text format and and additional values included in the text.
        /// </summary>
        /// <param name="property">The property name.</param>
        /// <param name="format">The composite format string.</param>
        /// <param name="values">The values that form part of the message text.</param>
        /// <returns>A <see cref="MessageItem"/>.</returns>
        public MessageItem AddPropertyError(string property, LText format, params object[] values) => Add(property, MessageType.Error, format, values);

        /// <summary>
        /// Adds a new <see cref="MessageType.Warning"/> <see cref="MessageItem"/> for a specified text.
        /// </summary>
        /// <param name="text">The message text.</param>
        /// <returns>A <see cref="MessageItem"/>.</returns>
        public MessageItem AddWarning(LText text) => Add(MessageType.Warning, text);

        /// <summary>
        /// Adds a new <see cref="MessageType.Warning"/> <see cref="MessageItem"/> for a specified text format and additional values included in the text.
        /// </summary>
        /// <param name="format">The composite format string.</param>
        /// <param name="values">The values that form part of the message text.</param>
        /// <returns>A <see cref="MessageItem"/>.</returns>
        public MessageItem AddWarning(LText format, params object[] values) => Add(MessageType.Warning, format, values);

        /// <summary>
        /// Adds a new <see cref="MessageType.Warning"/> <see cref="MessageItem"/> for the specified <see cref="MessageItem.Property"/> and text.
        /// </summary>
        /// <param name="property">The property name.</param>
        /// <param name="text">The message text.</param>
        /// <returns>A <see cref="MessageItem"/>.</returns>
        public MessageItem AddPropertyWarning(string property, LText text) => Add(property, MessageType.Warning, text);

        /// <summary>
        /// Adds a new <see cref="MessageType.Warning"/> <see cref="MessageItem"/> for the specified <see cref="MessageItem.Property"/>, text format and and additional values included in the text.
        /// </summary>
        /// <param name="property">The property name.</param>
        /// <param name="format">The composite format string.</param>
        /// <param name="values">The values that form part of the message text.</param>
        /// <returns>A <see cref="MessageItem"/>.</returns>
        public MessageItem AddPropertyWarning(string property, LText format, params object[] values) => Add(property, MessageType.Warning, format, values);

        /// <summary>
        /// Adds a new <see cref="MessageType.Info"/> <see cref="MessageItem"/> for a specified text.
        /// </summary>
        /// <param name="text">The message text.</param>
        /// <returns>A <see cref="MessageItem"/>.</returns>
        public MessageItem AddInfo(LText text) => Add(MessageType.Info, text);

        /// <summary>
        /// Adds a new <see cref="MessageType.Info"/> <see cref="MessageItem"/> for a specified text format and additional values included in the text.
        /// </summary>
        /// <param name="format">The composite format string.</param>
        /// <param name="values">The values that form part of the message text.</param>
        /// <returns>A <see cref="MessageItem"/>.</returns>
        public MessageItem AddInfo(LText format, params object[] values) => Add(MessageType.Info, format, values);

        /// <summary>
        /// Adds a new <see cref="MessageType.Info"/> <see cref="MessageItem"/> for the specified <see cref="MessageItem.Property"/> and text.
        /// </summary>
        /// <param name="property">The property name.</param>
        /// <param name="text">The message text.</param>
        /// <returns>A <see cref="MessageItem"/>.</returns>
        public MessageItem AddPropertyInfo(string property, LText text) => Add(property, MessageType.Info, text);

        /// <summary>
        /// Adds a new <see cref="MessageType.Info"/> <see cref="MessageItem"/> for the specified <see cref="MessageItem.Property"/>, text format and and additional values included in the text.
        /// </summary>
        /// <param name="property">The property name.</param>
        /// <param name="format">The composite format string.</param>
        /// <param name="values">The values that form part of the message text.</param>
        /// <returns>A <see cref="MessageItem"/>.</returns>
        public MessageItem AddPropertyInfo(string property, LText format, params object[] values) => Add(property, MessageType.Info, format, values);

        /// <summary>
        /// Gets a new <see cref="MessageItemCollection"/> for a selected <see cref="MessageType"/>.
        /// </summary>
        /// <param name="type">Message validatorType.</param>
        /// <returns>A new <see cref="MessageItemCollection"/>.</returns>
        public MessageItemCollection GetMessagesForType(MessageType type) => new(this.Where(x => x.Type == type));

        /// <summary>
        /// Gets a new <see cref="MessageItemCollection"/> for a selected <see cref="MessageType"/> and <see cref="MessageItem.Property"/>.
        /// </summary>
        /// <param name="type">Message validatorType.</param>
        /// <param name="property">The name of the property that the message relates to.</param>
        /// <returns>A new <see cref="MessageItemCollection"/>.</returns>
        public MessageItemCollection GetMessagesForType(MessageType type, string property) => new(this.Where(x => x.Type == type && x.Property == property));

        /// <summary>
        /// Gets a new <see cref="MessageItemCollection"/> for a selected <see cref="MessageItem.Property"/>.
        /// </summary>
        /// <param name="property">The name of the property that the message relates to.</param>
        /// <returns>A new <see cref="MessageItemCollection"/>.</returns>
        public MessageItemCollection GetMessagesForProperty(string property) => new(this.Where(x => x.Property == property));

        /// <summary>
        /// Determines whether a message exists for a <see cref="MessageItem.Property"/> <see cref="MessageType.Error"/>.
        /// </summary>
        /// <param name="property">The name of the property that the message relates to.</param>
        /// <returns><c>true</c> if a message exists; otherwise, <c>false</c>.</returns>
        public bool ContainsError(string property) => ContainsType(MessageType.Error, property);

        /// <summary>
		/// Determines whether a message exists for a selected <see cref="MessageType"/>.
		/// </summary>
        /// <param name="type">The <see cref="MessageType"/>.</param>
		/// <returns><c>true</c> if a message exists; otherwise, <c>false</c>.</returns>
		public bool ContainsType(MessageType type) => this.Any(x => x.Type == type);

        /// <summary>
        /// Determines whether a message exists for a selected <see cref="MessageType"/> and <see cref="MessageItem.Property"/>.
        /// </summary>
        /// <param name="type">The <see cref="MessageType"/>.</param>
        /// <param name="property">The name of the property that the message relates to.</param>
        /// <returns><c>true</c> if a message exists; otherwise, <c>false</c>.</returns>
        public bool ContainsType(MessageType type, string property) => this.Any(x => x.Type == type && x.Property == property);

        /// <summary>
        /// Determines whether a message exists for a selected <see cref="MessageItem.Property"/>.
        /// </summary>
        /// <param name="property">The name of the property that the message relates to.</param>
        /// <returns><c>true</c> if a message exists; otherwise, <c>false</c>.</returns>
        public bool ContainsProperty(string property) => this.Any(x => x.Property == property);

        /// <summary>
        /// Outputs the list of messages as a <see cref="string"/>.
        /// </summary>
        /// <returns>The list of messages as a <see cref="string"/>.</returns>
        public override string ToString()
        {
            if (Count == 0)
                return new LText("None.");

            var sb = new StringBuilder();
            foreach (var item in this)
            {
                if (sb.Length > 0)
                    sb.AppendLine();

                sb.Append($"{item.Type}: {item.Text}");
                if (!string.IsNullOrEmpty(item.Property))
                    sb.Append($" [{item.Property}]");
            }

            return sb.ToString();
        }
    }
}