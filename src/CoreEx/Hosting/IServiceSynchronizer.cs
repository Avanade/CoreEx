// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Hosting
{
    /// <summary>
    /// Enables concurrency management to synchronize the underlying execution.
    /// </summary>
    /// <remarks>The <see cref="Enter{T}"/> must acquire and hold a lock until the corresponding <see cref="Exit"/> is invoked. Where a lock is unable to be acquired then a <c>false</c> must be returned to advise the caller
    /// that processing can not occur at this time as another process is currently executing. A result of <c>true</c> indicates the lock was acquired and will be held until the corresponding <see cref="Exit"/>.</remarks>
    public interface IServiceSynchronizer : IDisposable
    {
        /// <summary>
        /// Acquires a lock on the specified <typeparamref name="T"/> and optional <paramref name="name"/>.
        /// </summary>
        /// <returns><c>true</c> if the lock is aquired; otherwise, <c>false</c>.</returns>
        /// <typeparam name="T">The <see cref="Type"/> to lock.</typeparam>
        bool Enter<T>(string? name = null);

        /// <summary>
        /// Releases the lock on the specified <typeparamref name="T"/> and optional <paramref name="name"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> to unlock.</typeparam>
        void Exit<T>(string? name = null);
    }
}