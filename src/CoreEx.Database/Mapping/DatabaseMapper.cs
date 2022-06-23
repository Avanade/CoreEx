// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Database.Mapping
{
    /// <summary>
    /// Enables <see cref="Create{TSource}"/> or <see cref="CreateAuto{TSource}(string[])"/> of a <see cref="DatabaseMapper{TSource}"/>.
    /// </summary>
    public static class DatabaseMapper
    {
        /// <summary>
        /// Creates a <see cref="DatabaseMapper{TSource}"/> where properties are added manually.
        /// </summary>
        /// <returns>A <see cref="DatabaseMapper{TSource}"/>.</returns>
        public static DatabaseMapper<TSource> Create<TSource>() where TSource : class, new() => new(false);

        /// <summary>
        /// Creates a <see cref="DatabaseMapper{TSource}"/> where properties are added automatically (assumes the property, column and parameter names share the same name).
        /// </summary>
        /// <param name="ignoreSrceProperties">An array of source property names to ignore.</param>
        /// <returns>A <see cref="DatabaseMapper{TSource}"/>.</returns>
        public static DatabaseMapper<TSource> CreateAuto<TSource>(params string[] ignoreSrceProperties) where TSource : class, new() => new(true, ignoreSrceProperties);
    }
}