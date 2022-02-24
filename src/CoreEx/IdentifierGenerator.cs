// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading.Tasks;

namespace CoreEx
{
    /// <summary>
    /// Provides an <see cref="IIdentifierGenerator{T}"/> for both a <see cref="string"/> and <see cref="Guid"/> where each is created using <see cref="Guid.NewGuid()"/>.
    /// </summary>
    public class IdentifierGenerator : IIdentifierGenerator<string>, IIdentifierGenerator<Guid>
    {
        /// <summary>
        /// Generate a new identifier value being a <see cref="Guid.NewGuid()"/> formatted as a <see cref="string"/>.
        /// </summary>
        /// <typeparam name="TFor">The <see cref="System.Type"/> to generate for.</typeparam>
        /// <returns>The newly generated identifier.</returns>
        /// <remarks>The <typeparamref name="TFor"/> allows for the likes of different identity sequences per <see cref="System.Type"/> for example.</remarks>
        public Task<string> GenerateIdentifierAsync<TFor>() => Task.FromResult(Guid.NewGuid().ToString());

        /// <summary>
        /// Generate a new identifier value being a <see cref="Guid.NewGuid()"/>
        /// </summary>
        /// <typeparam name="TFor">The <see cref="System.Type"/> to generate for.</typeparam>
        /// <returns>The newly generated identifier.</returns>
        /// <remarks>The <typeparamref name="TFor"/> allows for the likes of different identity sequences per <see cref="System.Type"/> for example.</remarks>
        Task<Guid> IIdentifierGenerator<Guid>.GenerateIdentifierAsync<TFor>() => Task.FromResult(Guid.NewGuid());
    }
}