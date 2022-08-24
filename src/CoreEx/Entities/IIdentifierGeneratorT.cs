// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Threading.Tasks;

namespace CoreEx.Entities
{
    /// <summary>
    /// Enables the generation of a new identifier value.
    /// </summary>
    /// <typeparam name="TId">The identifier <see cref="System.Type"/>.</typeparam>
    public interface IIdentifierGenerator<TId>
    {
        /// <summary>
        /// Generate a new identifier value.
        /// </summary>
        /// <typeparam name="TFor">The <see cref="System.Type"/> to generate for.</typeparam>
        /// <returns>The newly generated identifier.</returns>
        /// <remarks>The <typeparamref name="TFor"/> allows for the likes of different identity sequences per <see cref="System.Type"/> for example.</remarks>
        Task<TId> GenerateIdentifierAsync<TFor>();
    }
}