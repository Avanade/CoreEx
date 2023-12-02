// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Http;
using Microsoft.AspNetCore.TestHost;
using System;
using System.Net.Http;
using UnitTestEx.AspNetCore;

namespace UnitTestEx.NUnit
{
    /// <summary>
    /// Provides a shared <see cref="ApiTester"/> class to enable usage of the same underlying <see cref="TestServer"/> instance across multiple tests.
    /// </summary>
    /// <typeparam name="TEntryPoint">The API startup <see cref="Type"/>.</typeparam>
    /// <remarks>Implements <see cref="IDisposable"/> so should be automatically disposed off by the test framework host.</remarks>
    public abstract class UsingApiTester<TEntryPoint> : Internal.ApiTester<TEntryPoint> where TEntryPoint : class
    {
        /// <summary>
        /// Gets the <see cref="ApiTester"/>; i.e. itself.
        /// </summary>
        /// <remarks>This is provided for backwards compatibility.</remarks>
        public UsingApiTester<TEntryPoint> ApiTester => this;

        /// <summary>
        /// Enables a test <see cref="HttpRequestMessage"/> to be sent to the underlying <see cref="TestServer"/> with a specified <see cref="TypedHttpClientBase">agent</see>.
        /// </summary>
        /// <typeparam name="TAgent">The <see cref="AgentTester{TAgent}"/>.</typeparam>
        /// <returns>The <see cref="AgentTester{TAgent}"/>.</returns>
        public AgentTester<TAgent> Agent<TAgent>() where TAgent : CoreEx.Http.TypedHttpClientBase => new(this, GetTestServer());

        /// <summary>
        /// Enables a test <see cref="HttpRequestMessage"/> to be sent to the underlying <see cref="TestServer"/> with a specified <see cref="TypedHttpClientBase">agent</see>.
        /// </summary>
        /// <typeparam name="TAgent">The <see cref="AgentTester{TAgent}"/>.</typeparam>
        /// <typeparam name="TValue">The response value <see cref="Type"/>.</typeparam>
        /// <returns>The <see cref="AgentTester{TAgent, TValue}"/>.</returns>
        public AgentTester<TAgent, TValue> Agent<TAgent, TValue>() where TAgent : CoreEx.Http.TypedHttpClientBase => new(this, GetTestServer());
    }
}