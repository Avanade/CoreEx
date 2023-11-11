// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Json.Mapping
{
    /// <summary>
    /// Enables <see cref="Create{TSource}"/> or <see cref="CreateAuto{TSource}(string[])"/> of a <see cref="JsonObjectMapper{TSource}"/>.
    /// </summary>
    public class JsonObjectMapper
    {
        /// <summary>
        /// Creates a <see cref="JsonObjectMapper{TSource}"/> where properties are added manually.
        /// </summary>
        /// <param name="jsonSerializer">The <see cref="Text.Json.JsonSerializer"/>; defaults where not specified.</param>
        /// <returns>A <see cref="JsonObjectMapper{TSource}"/>.</returns>
        public static JsonObjectMapper<TSource> Create<TSource>(Text.Json.JsonSerializer? jsonSerializer = null) where TSource : class, new() => new(jsonSerializer, false);

        /// <summary>
        /// Creates a <see cref="JsonObjectMapper{TSource}"/> where properties are added automatically (assumes the property and column names share the same name).
        /// </summary>
        /// <param name="ignoreSrceProperties">An array of source property names to ignore.</param>
        /// <returns>A <see cref="JsonObjectMapper{TSource}"/>.</returns>
        public static JsonObjectMapper<TSource> CreateAuto<TSource>(params string[] ignoreSrceProperties) where TSource : class, new() => new(null, true, ignoreSrceProperties);

        /// <summary>
        /// Creates a <see cref="JsonObjectMapper{TSource}"/> where properties are added automatically (assumes the property and column names share the same name).
        /// </summary>
        /// <param name="jsonSerializer">The <see cref="Text.Json.JsonSerializer"/>; defaults where not specified.</param>
        /// <param name="ignoreSrceProperties">An array of source property names to ignore.</param>
        /// <returns>A <see cref="JsonObjectMapper{TSource}"/>.</returns>
        public static JsonObjectMapper<TSource> CreateAuto<TSource>(Text.Json.JsonSerializer? jsonSerializer, params string[] ignoreSrceProperties) where TSource : class, new() => new(jsonSerializer, true, ignoreSrceProperties);
    }
}