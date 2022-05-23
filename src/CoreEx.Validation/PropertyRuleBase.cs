// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions.Reflection;
using CoreEx.Localization;
using System;

namespace CoreEx.Validation
{
    /// <summary>
    /// Represents a base validation rule for an entity property.
    /// </summary>
    public abstract class PropertyRuleBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyRuleBase{TEntity, TProperty}"/> class.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="text">The friendly text name used in validation messages (defaults to <paramref name="name"/> as <see cref="PropertyExpression.ToSentenceCase(string)"/>).</param>
        /// <param name="jsonName">The JSON property name (defaults to <paramref name="name"/>).</param>
        protected PropertyRuleBase(string name, LText? text = null, string? jsonName = null)
        {
            Name = string.IsNullOrEmpty(name) ? throw new ArgumentNullException(nameof(name)) : name;
            Text = text ?? Name.ToSentenceCase();
            JsonName = string.IsNullOrEmpty(jsonName) ? Name : jsonName;
        }

        /// <summary>
        /// Gets the property name.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Gets the JSON property name.
        /// </summary>
        public string JsonName { get; internal set; }

        /// <summary>
        /// Gets or sets the friendly text name used in validation messages.
        /// </summary>
        public LText Text { get; set; }
    }
}