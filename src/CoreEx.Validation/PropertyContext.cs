// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions.Reflection;
using CoreEx.Entities;
using CoreEx.Localization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreEx.Validation
{
    /// <summary>
    /// Provides a validation context for a property.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    public class PropertyContext<TEntity, TProperty> : IPropertyContext<TEntity, TProperty> where TEntity : class
    {
        private readonly bool _doNotAppendName = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyContext{TEntity, TProperty}"/> class.
        /// </summary>
        /// <param name="context">The validation context for the parent entity.</param>
        /// <param name="value">The property value.</param>
        /// <param name="name">The property name.</param>
        /// <param name="jsonName">The JSON property name.</param>
        /// <param name="text">The property text.</param>
        public PropertyContext(ValidationContext<TEntity> context, TProperty? value, string name, string? jsonName = null, LText? text = null)
        {
            Parent = context ?? throw new ArgumentNullException(nameof(context));
            Name = string.IsNullOrEmpty(name) ? throw new ArgumentNullException(nameof(name)) : name;
            JsonName = jsonName ?? Name;
            UseJsonName = context.UsedJsonNames;
            Text = text ?? Name.ToSentenceCase()!;
            Value = value;
            FullyQualifiedPropertyName = CreateFullyQualifiedPropertyName(Name);
            FullyQualifiedJsonPropertyName = CreateFullyQualifiedJsonPropertyName(JsonName);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyContext{TEntity, TProperty}"/> class where the property is considered the value (impacts the fully-qualified names, etc.).
        /// </summary>
        /// <param name="text">The property text.</param>
        /// <param name="context">The validation context for the parent entity.</param>
        /// <param name="value">The property value.</param>
        public PropertyContext(LText? text, ValidationContext<TEntity> context, TProperty? value)
        {
            Parent = context ?? throw new ArgumentNullException(nameof(context));
            FullyQualifiedPropertyName = Parent.FullyQualifiedEntityName ?? Validation.ValueNameDefault;
            FullyQualifiedJsonPropertyName = Parent.FullyQualifiedJsonEntityName ?? Validation.ValueNameDefault;
            Name = FullyQualifiedPropertyName.Split('.', StringSplitOptions.RemoveEmptyEntries).Last();
            JsonName = FullyQualifiedJsonPropertyName.Split('.', StringSplitOptions.RemoveEmptyEntries).Last();
            Text = text ?? Name.ToSentenceCase()!;
            Value = value;
            _doNotAppendName = true;
        }

        /// <summary>
        /// Gets the <see cref="ExecutionContext.Current"/> instance.
        /// </summary>
        public CoreEx.ExecutionContext ExecutionContext => ExecutionContext.Current;

        /// <summary>
        /// Gets the <see cref="ValidationContext{TEntity}"/> for the parent entity.
        /// </summary>
        public ValidationContext<TEntity> Parent { get; }

        /// <summary>
        /// Gets the <see cref="IValidationContext"/> for the parent entity.
        /// </summary>
        IValidationContext IPropertyContext.Parent => Parent;

        /// <summary>
        /// Gets the property name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the JSON property name.
        /// </summary>
        public string JsonName { get; }

        /// <summary>
        /// Gets the fully qualified property name.
        /// </summary>
        public string FullyQualifiedPropertyName { get; }

        /// <summary>
        /// Gets the fully qualified Json property name.
        /// </summary>
        public string FullyQualifiedJsonPropertyName { get; }

        /// <summary>
        /// Indicates whether to use the JSON property name for the <see cref="MessageItem"/> <see cref="MessageItem.Property"/>; by default (<c>false</c>) uses the .NET property name.
        /// </summary>
        public bool UseJsonName { get; }

        /// <summary>
        /// Gets the property text.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Gets the property value.
        /// </summary>
        object? IPropertyContext.Value => Value;

        /// <summary>
        /// Gets the property value.
        /// </summary>
        public TProperty? Value { get; private set; }

        /// <summary>
        /// Indicates whether there has been a validation error.
        /// </summary>
        public bool HasError { get; internal set; }

        /// <summary>
        /// Enables the underlying value to be overridden (updated).
        /// </summary>
        /// <param name="value">The override value.</param>
        public void OverrideValue(TProperty? value)
        {
            if (Comparer<TProperty?>.Default.Compare(Value, value) == 0)
                return;

            // Get the property info.
            if (Parent.Value is ValidationValue<TProperty> vv)
            {
                if (vv.Entity != null)
                {
                    var pr = TypeReflector.GetReflector(TypeReflectorArgs.Default, vv.Entity.GetType()).GetProperty(Name) ?? throw new InvalidOperationException($"Property '{Name}' does not exist for Type {Parent.EntityType.Name}.");

                    try
                    {
                        pr.PropertyExpression.SetValue(vv.Entity, value);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Type '{Parent.EntityType.Name}' Property '{Name}' value cannot be overridden: {ex.Message}", ex);
                    }
                }
                else
                    throw new InvalidOperationException("A value without a parent object owning the property value cannot be overridden.");
            }
            else
            {
                var pr = TypeReflector.GetReflector<TEntity>(TypeReflectorArgs.Default).GetProperty(Name) ?? throw new InvalidOperationException($"Property '{Name}' does not exist for Type {typeof(TEntity).Name}.");

                try
                {
                    pr.PropertyExpression.SetValue(Parent.Value, value);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Type '{typeof(TEntity).Name}' Property '{Name}' value cannot be overridden: {ex.Message}", ex);
                }
            }

            Value = value;
        }

        /// <summary>
        /// Creates a new <see cref="MessageType.Error"/> <see cref="MessageItem"/> with the specified format and <b>adds</b> to the underlying <see cref="IValidationContext"/>.
        /// The friendly <see cref="Text"/> and <see cref="Value"/> are automatically passed as the first two arguments to the string formatter.
        /// </summary>
        /// <param name="format">The composite format string.</param>
        /// <returns>A <see cref="MessageItem"/>.</returns>
        public MessageItem CreateErrorMessage(LText format) => CreateErrorMessage(format, Array.Empty<object>());

        /// <summary>
        /// Creates a new <see cref="MessageType.Error"/> <see cref="MessageItem"/> with the specified format and additional values to be included in the text and <b>adds</b> to the underlying <see cref="IValidationContext"/>.
        /// The friendly <see cref="Text"/> and <see cref="Value"/> are automatically passed as the first two arguments to the string formatter.
        /// </summary>
        /// <param name="format">The composite format string.</param>
        /// <param name="values">The values that form part of the message text (<see cref="Text"/> and <see cref="Value"/> are automatically passed as the first two arguments to the string formatter).</param>
        /// <returns>A <see cref="MessageItem"/>.</returns>
        public MessageItem CreateErrorMessage(LText format, params object?[] values)
        {
            HasError = true;
            var fVals = (new string[] { Text, Value?.ToString()! }).Concat(values).ToArray();

            if (_doNotAppendName)
                return Parent.AddMessage(MessageType.Error, format, fVals);
            else
                return Parent.AddMessage(Name, JsonName, MessageType.Error, format, fVals);
        }

        /// <summary>
        /// Creates a fully qualified property name for the name.
        /// </summary>
        /// <param name="name">The property name.</param>
        public string CreateFullyQualifiedPropertyName(string name) => (Parent.FullyQualifiedEntityName == null) ? name : (Parent.FullyQualifiedEntityName + (name.StartsWith("[") ? "" : ".") + name);

        /// <summary>
        /// Creates a fully qualified JSON property name for the name.
        /// </summary>
        /// <param name="name">The property name.</param>
        public string CreateFullyQualifiedJsonPropertyName(string name) => (Parent.FullyQualifiedJsonEntityName == null) ? name : (Parent.FullyQualifiedJsonEntityName + (name.StartsWith("[") ? "" : ".") + name);

        /// <summary>
        /// Creates a new <see cref="ValidationArgs"/> from the <see cref="PropertyContext{TEntity, TProperty}"/>.
        /// </summary>
        /// <returns>A <see cref="ValidationArgs"/>.</returns>
        public ValidationArgs CreateValidationArgs()
        {
            var args = new ValidationArgs
            {
                FullyQualifiedEntityName = FullyQualifiedPropertyName,
                FullyQualifiedJsonEntityName = FullyQualifiedJsonPropertyName,
                SelectedPropertyName = Parent?.SelectedPropertyName,
                UseJsonNames = Parent?.UsedJsonNames
            };

            // Copy the configuration values; do not allow the higher-level dictionaries (stack) to be extended by lower-level validators.
            if (Parent?.Config != null)
            {
                if (args.Config == null)
                    args.Config = new Dictionary<string, object?>();

                foreach (var cfg in Parent.Config)
                {
                    args.Config.Add(cfg.Key, cfg.Value);
                }
            }

            return args;
        }

        /// <summary>
        /// Merges a validation result into this.
        /// </summary>
        /// <param name="context">The <see cref="IValidationContext"/> to merge.</param>
        public void MergeResult(IValidationContext context)
        {
            if (context == null)
                return;

            if (context.HasErrors)
                HasError = true;

            Parent.MergeResult(context?.Messages);
        }
    }
}