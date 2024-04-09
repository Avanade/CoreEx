// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using CoreEx.Hosting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;

namespace CoreEx.Azure.Storage
{
    /// <summary>
    /// An <see cref="IServiceSynchronizer"/> that performs synchronization by acquiring a lease (see <see href="https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-lease"/>) on a blob.
    /// </summary>
    /// <remarks>A blob is created per <see cref="Type"/> with a name of <see cref="Type.FullName"/> and extension of '.lock'; e.g. '<c>Namespace.Class.lock</c>'. For this to function correctly all running instances must be referencing the same blob container.
    /// <para>The duration to acquire the lease is generally unknown and an <see cref="Enter{T}(string?)"/> and <see cref="Exit{T}(string?)"/> can not be guaranteed in the case of failure. Therefore, an infinite value can not be used as the lease
    /// would then need to be released manually under a failure. To mitigate this a lease is taken for the specified <see cref="LeaseDuration"/>; with an internal <see cref="Timer"/> automatically renewing on <see cref="AutoRenewLeaseDuration"/>.
    /// This will result in worst case a lease of <see cref="LeaseDuration"/> on failure.</para></remarks>
    public class BlobLeaseSynchronizer : IServiceSynchronizer
    {
        private readonly BlobContainerClient _client;
        private readonly string _leaseId = Guid.NewGuid().ToString();
        private readonly ConcurrentDictionary<string, BlobLeaseClient> _dict = new();
        private readonly Lazy<Timer> _timer;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobLeaseSynchronizer"/> class.
        /// </summary>
        /// <param name="client">The <see cref="BlobContainerClient"/>.</param>
        /// <remarks>Performs a <see cref="BlobContainerClient.CreateIfNotExists(PublicAccessType, IDictionary{string, string}, BlobContainerEncryptionScopeOptions, CancellationToken)"/> to ensure the container exists.</remarks>
        public BlobLeaseSynchronizer(BlobContainerClient client)
        { 
            _client = client.ThrowIfNull(nameof(client));
            _timer = new Lazy<Timer>(() => new Timer(_ =>
            {
                foreach (var kvp in _dict.ToArray())
                {
                    try
                    {
                        kvp.Value.RenewAsync();
                    }
                    catch { } // Swallow and carry on. 
                }
            }, null, AutoRenewLeaseDuration, AutoRenewLeaseDuration), isThreadSafe: true);
        }

        /// <summary>
        /// Gets the <see cref="BlobLease"/> duration.
        /// </summary>
        /// <remarks>The value must be greater than the <see cref="AutoRenewLeaseDuration"/> to function correctly.</remarks>
        public virtual TimeSpan LeaseDuration => TimeSpan.FromSeconds(60);

        /// <summary>
        /// Gets the <see cref="BlobLeaseClient.Renew(RequestConditions, CancellationToken)"/> duration.
        /// </summary>
        /// <remarks>The value must be less than the <see cref="LeaseDuration"/> to function correctly.</remarks>
        public virtual TimeSpan AutoRenewLeaseDuration => TimeSpan.FromSeconds(30);

        /// <inheritdoc/>
        public bool Enter<T>(string? name = null)
        {
            try
            {
                // Is exclusive for this invocation only where genuinely creating.
                bool exclusiveLock = false;

                _dict.GetOrAdd(GetName<T>(name), fn =>
                {
                    _client.CreateIfNotExists();

                    var blob = _client.GetBlobClient(GetName<T>(name));
                    try
                    {
                        var bp = blob.GetProperties();
                        switch (bp.Value.LeaseState)
                        {
                            case LeaseState.Available:
                            case LeaseState.Expired:
                            case LeaseState.Broken:
                                break;

                            default:
                                throw new RequestFailedException((int)HttpStatusCode.Conflict, "Invalid lease state.");
                        }
                    }
                    catch (RequestFailedException rfex) when (rfex.Status == (int)HttpStatusCode.NotFound)
                    {
                        // Does not exist, so create.
                        using var s = blob.OpenWrite(true);
                    }

                    var lease = blob.GetBlobLeaseClient(_leaseId);
                    lease.Acquire(LeaseDuration);
                    exclusiveLock = true;
                    return lease;
                });

                // Start timer on first enter.
                if (!_timer.IsValueCreated)
                    _ = _timer.Value;

                return exclusiveLock;
            }
            catch (RequestFailedException rfex) when (rfex.Status == (int)HttpStatusCode.PreconditionFailed || rfex.Status == (int)HttpStatusCode.Conflict) { return false; } // Already exists and locked!
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected exception whilst attempting to get an exclusive lease on blob '{GetName<T>(name)}': {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public void Exit<T>(string? name = null)
        {
            if (_dict.TryRemove(GetName<T>(name), out var lease))
                ReleaseLease(lease);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                if (_timer.IsValueCreated)
                    _timer.Value.Dispose();

                _dict.Values.ForEach(ReleaseLease);
            }

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="BlobLeaseSynchronizer"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing) { }

        /// <summary>
        /// Gets the full name.
        /// </summary>
        private static string GetName<T>(string? name) => $"{typeof(T).FullName}{(name == null ? "" : $".{name}")}.lock";

        /// <summary>
        /// Release the lease swallowing any and all exceptions.
        /// </summary>
        private static void ReleaseLease(BlobLeaseClient lease)
        {
            try { lease.Release(); }
            catch (Exception) { /* Swallow and carry on. */ }
        }
    }
}