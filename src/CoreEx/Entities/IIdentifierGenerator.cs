// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading.Tasks;

namespace CoreEx.Entities
{
    /// <summary>
    /// Enables the generation of a new identifier value for <i>any</i> identifier <see cref="Type"/>.
    /// </summary>
    public interface IIdentifierGenerator
    {
        /// <summary>
        /// Generate a new identifier value.
        /// </summary>
        /// <typeparam name="TId">The identifier <see cref="System.Type"/>.</typeparam>
        /// <typeparam name="TFor">The <see cref="System.Type"/> to generate for.</typeparam>
        /// <returns>The newly generated identifier.</returns>
        /// <remarks>The <typeparamref name="TFor"/> allows for the likes of different identity sequences per <see cref="System.Type"/> for example.</remarks>
        Task<TId> GenerateIdentifierAsync<TId, TFor>();

        /// <summary>
        /// Assigns a generated identifier to the <paramref name="value"/> where the <see cref="IIdentifier.Id"/> has a default value.
        /// </summary>
        /// <typeparam name="TFor">The <see cref="System.Type"/> to generate for.</typeparam>
        /// <param name="value">The value to assign an identifier for.</param>
        Task AssignIdentifierAsync<TFor>(TFor value);
    }
}