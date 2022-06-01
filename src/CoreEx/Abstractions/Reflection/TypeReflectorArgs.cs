// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;

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
        /// <param name="cache">The <see cref="IMemoryCache"/> to use versus instantiating each <see cref="TypeReflector"/> per use (expensive operation).</param>
        public TypeReflectorArgs(IJsonSerializer? jsonSerializer = null, IMemoryCache? cache = null)
        {
            JsonSerializer = jsonSerializer ?? Json.JsonSerializer.Default;
            Cache = cache ?? new MemoryCache(new MemoryCacheOptions());
        }

        /// <summary>
        /// Gets the <see cref="IJsonSerializer"/>.
        /// </summary>
        public IJsonSerializer JsonSerializer { get; }

        /// <summary>
        /// Gets the <see cref="IMemoryCache"/> to use versus instantiating each <see cref="TypeReflector"/> per use.
        /// </summary>
        /// <remarks>The <see cref="AbsoluteExpirationTimespan"/> and <see cref="SlidingExpirationTimespan"/> enable additional basic policy configuration for the cached items.</remarks>
        public IMemoryCache Cache { get; }

        /// <summary>
        /// Gets or sets the <see cref="IMemoryCache"/> absolute expiration <see cref="TimeSpan"/>. Default to <c>4</c> hours.
        /// </summary>
        public TimeSpan AbsoluteExpirationTimespan { get; set; } = TimeSpan.FromHours(4);

        /// <summary>
        /// Gets or sets the <see cref="IMemoryCache"/> sliding expiration <see cref="TimeSpan"/>. Default to <c>30</c> minutes.
        /// </summary>
        public TimeSpan SlidingExpirationTimespan { get; set; } = TimeSpan.FromMinutes(30);

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