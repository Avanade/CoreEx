// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Json;
using System;
using System.Collections.Concurrent;

namespace CoreEx.Abstractions.Reflection
{
    /// <summary>
    /// Provides the arguments passed to and through a <see cref="EntityReflector"/>.
    /// </summary>
    public class EntityReflectorArgs
    {
        /// <summary>
        /// Initializes an instance of the <see cref="EntityReflectorArgs"/> class with an optional <paramref name="cache"/>.
        /// </summary>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>. Defaults to <see cref="Json.JsonSerializer.Default"/>.</param>
        /// <param name="cache">The <b>cache</b> <see cref="ConcurrentDictionary{Type, IEntityReflector}"/> to use versus instantiating each <see cref="EntityReflector"/> per use.</param>
        public EntityReflectorArgs(IJsonSerializer? jsonSerializer = null, ConcurrentDictionary<Type, IEntityReflector>? cache = null)
        {
            JsonSerializer = jsonSerializer ?? Json.JsonSerializer.Default;
            Cache = cache ?? new();
        }

        /// <summary>
        /// Gets the <see cref="IJsonSerializer"/>.
        /// </summary>
        public IJsonSerializer JsonSerializer { get; }

        /// <summary>
        /// Gets the <b>cache</b> <see cref="ConcurrentDictionary{Type, IEntityReflector}"/> to use versus instantiating each <see cref="EntityReflector"/> per use.
        /// </summary>
        public ConcurrentDictionary<Type, IEntityReflector> Cache { get; }

        /// <summary>
        /// Gets or sets the action to invoke to perform additional logic when reflecting/building the <b>entity</b> <see cref="Type"/>.
        /// </summary>
        public Action<IEntityReflector>? EntityBuilder { get; set; } = null;

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