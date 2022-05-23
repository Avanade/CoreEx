// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Json;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;

namespace CoreEx.Abstractions.Reflection
{
    /// <summary>
    /// Provides the arguments passed to and through a <see cref="TypeReflector"/>.
    /// </summary>
    public class TypeReflectorArgs
    {
        private static readonly Lazy<TypeReflectorArgs> _default = new(() => new TypeReflectorArgs());

        /// <summary>
        /// Gets the default <see cref="TypeReflectorArgs"/>.
        /// </summary>
        public static TypeReflectorArgs Default => (ExecutionContext.HasCurrent ? ExecutionContext.Current?.ServiceProvider?.GetService<TypeReflectorArgs>() : null) ?? _default.Value;

        /// <summary>
        /// Initializes an instance of the <see cref="TypeReflectorArgs"/> class with an optional <paramref name="cache"/>.
        /// </summary>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>. Defaults to <see cref="Json.JsonSerializer.Default"/>.</param>
        /// <param name="cache">The <b>cache</b> <see cref="ConcurrentDictionary{TKey, TValue}"/> to use versus instantiating each <see cref="TypeReflector"/> per use.</param>
        public TypeReflectorArgs(IJsonSerializer? jsonSerializer = null, ConcurrentDictionary<Type, ITypeReflector>? cache = null)
        {
            JsonSerializer = jsonSerializer ?? Json.JsonSerializer.Default;
            Cache = cache ?? new();
        }

        /// <summary>
        /// Gets the <see cref="IJsonSerializer"/>.
        /// </summary>
        public IJsonSerializer JsonSerializer { get; }

        /// <summary>
        /// Gets the <b>cache</b> <see cref="ConcurrentDictionary{TKey, TValue}"/> to use versus instantiating each <see cref="TypeReflector"/> per use.
        /// </summary>
        public ConcurrentDictionary<Type, ITypeReflector> Cache { get; }

        /// <summary>
        /// Gets or sets the action to invoke to perform additional logic when reflecting/building the <b>entity</b> <see cref="Type"/>.
        /// </summary>
        public Action<ITypeReflector>? TypeBuilder { get; set; } = null;

        /// <summary>
        /// Indicates whether to automatically populate the entity properties. Defaults to <c>true</c>.
        /// </summary>
        /// <remarks>Will invoked the optional <see cref="PropertyBuilder"/> as each property is being added.</remarks>
        public bool AutoPopulateProperties { get; set; } = true;

        /// <summary>
        /// Gets or sets the function to invoke to perform additional logic when reflecting/building the <b>property</b> <see cref="Type"/>; the result determines whether the
        /// property should be included (<c>true</c>) or not (<c>false</c>) within the underlying properties collection.
        /// </summary>
        public Func<IPropertyReflector, bool>? PropertyBuilder { get; set; } = null;

        /// <summary>
        /// Defines the <see cref="StringComparer"/> for finding the property/JSON names (defaults to <see cref="StringComparer.Ordinal"/>).
        /// </summary>
        public StringComparer NameComparer { get; set; } = StringComparer.Ordinal;
    }
}