// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.OData.Mapping
{
    /// <summary>
    /// Enables <see cref="Create{TSource}"/> or <see cref="CreateAuto{TSource}(string[])"/> of a <see cref="ODataMapper{TSource}"/>.
    /// </summary>
    public class ODataMapping
    {
        /// <summary>
        /// Creates a <see cref="ODataMapper{TSource}"/> where properties are added manually.
        /// </summary>
        /// <returns>A <see cref="ODataMapper{TSource}"/>.</returns>
        public static ODataMapper<TSource> Create<TSource>() where TSource : class, new() => new(false);

        /// <summary>
        /// Creates a <see cref="ODataMapper{TSource}"/> where properties are added automatically (assumes the property and column names share the same name).
        /// </summary>
        /// <param name="ignoreSrceProperties">An array of source property names to ignore.</param>
        /// <returns>A <see cref="ODataMapper{TSource}"/>.</returns>
        public static ODataMapper<TSource> CreateAuto<TSource>(params string[] ignoreSrceProperties) where TSource : class, new() => new(true, ignoreSrceProperties);
    }
}