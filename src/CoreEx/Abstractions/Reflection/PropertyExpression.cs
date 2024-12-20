﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Json;
using CoreEx.Localization;
using CoreEx.Text;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

namespace CoreEx.Abstractions.Reflection
{
    /// <summary>
    /// Provides access to the common property expression capabilities.
    /// </summary>
    public static partial class PropertyExpression
    {
        /// <summary>
        /// Gets the <see cref="IMemoryCache"/>.
        /// </summary>
        internal static IMemoryCache Cache => ExecutionContext.GetService<IReflectionCache>() ?? Internal.MemoryCache;

        /// <summary>
        /// Gets or sets the <see cref="IMemoryCache"/> absolute expiration <see cref="TimeSpan"/>.
        /// </summary>
        /// <remarks>Defaults to <c>4</c> hours.</remarks>
        public static TimeSpan? AbsoluteExpirationTimespan { get; set; } = TimeSpan.FromHours(4);

        /// <summary>
        /// Gets or sets the <see cref="IMemoryCache"/> sliding expiration <see cref="TimeSpan"/>.
        /// </summary>
        /// <remarks>Defaults to <c>30</c> minutes.</remarks>
        public static TimeSpan? SlidingExpirationTimespan { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Gets or sets the function to create the <see cref="LText"/> for the specified entity <see cref="Type"/>, <see cref="PropertyInfo"/> and <i>inferred</i> text (being either the corresponding <see cref="DisplayAttribute.Name"/> where specified
        /// or the <see cref="MemberInfo.Name"/> converted to <see cref="SentenceCase.ToSentenceCase"/>).
        /// </summary>
        /// <remarks>Defaults the <see cref="LText.KeyAndOrText"/> to the fully qualified name (being <see cref="Type.FullName"/> + '.' + <see cref="MemberInfo.Name"/>) and the <see cref="LText.FallbackText"/> to the <i>inferred</i> text.</remarks>
        public static Func<Type, PropertyInfo, string, LText> CreatePropertyLText { get; set; } = (entityType, propertyInfo, text) => new LText($"{entityType.FullName}.{propertyInfo.Name}", text);

        /// <summary>
        /// Validates, creates and compiles the property expression; whilst also determinig the property friendly <see cref="PropertyExpression{TEntity, TProperty}.Text"/>.
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="propertyExpression">The <see cref="Expression"/> to reference the entity property.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>. Defaults to <see cref="JsonSerializer.Default"/> where not specified.</param>
        /// <returns>A <see cref="PropertyExpression{TEntity, TProperty}"/> which contains (in order) the compiled <see cref="System.Func{TEntity, TProperty}"/>, member name and resulting property text.</returns>
        /// <remarks>Caching is used to improve performance; subsequent calls will return the corresponding cached value.</remarks>
        public static PropertyExpression<TEntity, TProperty> Create<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression, IJsonSerializer? jsonSerializer = null)
            => PropertyExpression<TEntity, TProperty>.CreateInternal(propertyExpression.ThrowIfNull(nameof(propertyExpression)), DetermineJsonSerializer(jsonSerializer));

        /// <summary>
        /// Gets the <see cref="PropertyExpression{TEntity, TProperty}"/> from the cache.
        /// </summary>
        /// <param name="entityType">The entity <see cref="Type"/>.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>. Defaults to <see cref="JsonSerializer.Default"/> where not specified.</param>
        /// <returns>The <see cref="IPropertyExpression"/> where found; otherwise, <c>null</c>.</returns>
        public static IPropertyExpression? Get(Type entityType, string propertyName, IJsonSerializer? jsonSerializer = null)
            => (IPropertyExpression?)Cache.Get((entityType, propertyName, DetermineJsonSerializer(jsonSerializer).GetType())) ?? null;

        /// <summary>
        /// Determine the <see cref="IJsonSerializer"/> by firstly using the <see cref="ExecutionContext.ServiceProvider"/> to find, then falling back to the <see cref="JsonSerializer.Default"/>.
        /// </summary>
        /// <returns>The <see cref="IJsonSerializer"/>.</returns>
        /// <remarks>This does scream <i>Service Locator</i>, which is considered an anti-pattern by some, but this avoids the added complexity of passing the <see cref="IJsonSerializer"/> where most implementations will default to the
        /// <see cref="CoreEx.Json.JsonSerializer"/> implementation - this just avoids unnecessary awkwardness for sake of purity. Finally, this class is intended for largely internal use only.</remarks>
        private static IJsonSerializer DetermineJsonSerializer(IJsonSerializer? jsonSerializer) => jsonSerializer ?? ExecutionContext.GetService<IJsonSerializer>() ?? JsonSerializer.Default;
    }
}