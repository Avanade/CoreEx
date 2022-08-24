// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading.Tasks;

namespace CoreEx.Entities
{
    /// <summary>
    /// Provides an <see cref="IIdentifierGenerator{T}"/> for both a <see cref="string"/> and <see cref="Guid"/> where each is created using <see cref="Guid.NewGuid()"/>.
    /// </summary>
    public class IdentifierGenerator : IIdentifierGenerator, IIdentifierGenerator<string>, IIdentifierGenerator<Guid>
    {
        /// <inheritdoc/>
        public async Task<TId> GenerateIdentifierAsync<TId, TFor>() => typeof(TId) switch
        {
            Type _ when typeof(TId) == typeof(string) => (TId)Convert.ChangeType(await ((IIdentifierGenerator<string>)this).GenerateIdentifierAsync<TFor>().ConfigureAwait(false), typeof(TId)),
            Type _ when typeof(TId) == typeof(Guid) => (TId)Convert.ChangeType(await ((IIdentifierGenerator<Guid>)this).GenerateIdentifierAsync<TFor>().ConfigureAwait(false), typeof(TId)),
            _ => throw new NotSupportedException($"Identifier Type '{typeof(TId).Name}' is not supported; only String or Guid.")
        };

        /// <inheritdoc/>
        public async Task AssignIdentifierAsync<TFor>(TFor value)
        {
            if (value is not IIdentifier ii)
                return;

            if (value is IIdentifier<string> iis)
            {
                if (iis.Id == null)
                    iis.Id = await ((IIdentifierGenerator<string>)this).GenerateIdentifierAsync<TFor>().ConfigureAwait(false);
            }
            else if (value is IIdentifier<Guid> iig)
            {
                if (iig.Id == Guid.Empty)
                    iig.Id = await ((IIdentifierGenerator<Guid>)this).GenerateIdentifierAsync<TFor>().ConfigureAwait(false);
            }
            else
                throw new NotSupportedException($"Identifier Type '{ii.IdType.Name}' is not supported; only String or Guid.");
        }

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