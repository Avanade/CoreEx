// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Hosting
{
    /// <summary>
    /// An <see cref="IServiceSynchronizer"/> that performs <b>no</b> synchronization in that <see cref="Enter"/> will always return <c>true</c> resulting in concurrent execution.
    /// </summary>
    /// <remarks>This should be used in scenarios where synchronization is not required as this is handled externally or is not needed.</remarks>
    public sealed class ConcurrentSynchronizer : IServiceSynchronizer
    {
        /// <inheritdoc/>
        public bool Enter<T>(string? name = null) => true;

        /// <inheritdoc/>
        public void Exit<T>(string? name) { }

        /// <inheritdoc/>
        public void Dispose() { }
    }
}