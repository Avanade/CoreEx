// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Dataverse.Mapping
{
    /// <summary>
    /// Enables <see cref="Create{TSource}"/> or <see cref="CreateAuto{TSource}(string[])"/> of a <see cref="DataverseMapper{TSource}"/>.
    /// </summary>
    public class DataverseMapper
    {
        /// <summary>
        /// Creates a <see cref="DataverseMapper{TSource}"/> where properties are added manually.
        /// </summary>
        /// <returns>A <see cref="DataverseMapper{TSource}"/>.</returns>
        public static DataverseMapper<TSource> Create<TSource>() where TSource : class, new() => new(false);

        /// <summary>
        /// Creates a <see cref="DataverseMapper{TSource}"/> where properties are added automatically (assumes the property and column names share the same name).
        /// </summary>
        /// <param name="ignoreSrceProperties">An array of source property names to ignore.</param>
        /// <returns>A <see cref="DataverseMapper{TSource}"/>.</returns>
        public static DataverseMapper<TSource> CreateAuto<TSource>(params string[] ignoreSrceProperties) where TSource : class, new() => new(true, ignoreSrceProperties);
    }
}