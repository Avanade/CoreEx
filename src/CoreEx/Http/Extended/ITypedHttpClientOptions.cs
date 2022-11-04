// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Http.Extended
{
    /// <summary>
    /// Provides access to the <see cref="DefaultOptions"/> and <see cref="SendOptions"/>.
    /// </summary>
    internal interface ITypedHttpClientOptions
    {
        /// <summary>
        /// Gets the default <see cref="TypedHttpClientOptions"/> used by all invocations.
        /// </summary>
        TypedHttpClientOptions DefaultOptions { get; }

        /// <summary>
        /// Gets the <see cref="TypedHttpClientOptions"/> used <i>per</i> <see cref="TypedHttpClientBase.SendAsync(System.Net.Http.HttpRequestMessage, System.Threading.CancellationToken)"/> invocation.
        /// </summary>
        TypedHttpClientOptions SendOptions { get; }

        /// <summary>
        /// Indicates whether the <see cref="SendOptions"/> are being configured.
        /// </summary>
        bool HasSendOptions { get; } 
    }
}