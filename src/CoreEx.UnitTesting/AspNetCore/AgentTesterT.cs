// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using UnitTestEx.Abstractions;
using UnitTestEx.Assertors;

namespace UnitTestEx.AspNetCore
{
    /// <summary>
    /// Provides <b>HTTP Agent</b> <see cref="TypedHttpClientBase"/> testing.
    /// </summary>
    /// <typeparam name="TAgent">The Agent (inherits from <see cref="TypedHttpClientBase"/>) <see cref="Type"/>.</typeparam>
    /// <typeparam name="TValue">The response value <see cref="Type"/>.</typeparam>
    /// <param name="owner">The owning <see cref="TesterBase"/>.</param>
    /// <param name="testServer">The <see cref="TestServer"/>.</param>
    public class AgentTester<TAgent, TValue>(TesterBase owner, TestServer testServer) : HttpTesterBase<TValue, AgentTester<TAgent, TValue>>(owner, testServer) where TAgent : TypedHttpClientBase
    {
        /// <summary>
        /// Runs the test by executing a <typeparamref name="TAgent"/> method.
        /// </summary>
        /// <param name="func">The function to execution.</param>
        /// <returns>An <see cref="HttpResponseMessageAssertor{TValue}"/>.</returns>
        public HttpResponseMessageAssertor<TValue> Run(Func<TAgent, Task<HttpResult<TValue>>> func) => RunAsync(func).GetAwaiter().GetResult();

        /// <summary>
        /// Runs the test by executing a <typeparamref name="TAgent"/> method.
        /// </summary>
        /// <param name="func">The function to execution.</param>
        /// <returns>An <see cref="HttpResponseMessageAssertor{TValue}"/>.</returns>
        public HttpResponseMessageAssertor<TValue> Run(Func<TAgent, Task<HttpResponseMessage>> func) => RunAsync(func).GetAwaiter().GetResult();

        /// <summary>
        /// Runs the test by executing a <typeparamref name="TAgent"/> method.
        /// </summary>
        /// <param name="func">The function to execution.</param>
        /// <returns>An <see cref="HttpResponseMessageAssertor{TValue}"/>.</returns>
        public async Task<HttpResponseMessageAssertor<TValue>> RunAsync(Func<TAgent, Task<HttpResult<TValue>>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));

            using var scope = this.CreateClientScope<TAgent>();
            var agent = scope.ServiceProvider.GetRequiredService<TAgent>();
            var res = await func(agent).ConfigureAwait(false);

            await Task.Delay(TestSetUp.TaskDelayMilliseconds).ConfigureAwait(false);

            var result = res.ToResult();
            if (res.IsSuccess)
                await ExpectationsArranger.AssertAsync(ExpectationsArranger.CreateValueArgs(LastLogs, result.Value).AddExtra(res.Response)).ConfigureAwait(false);
            else
                await ExpectationsArranger.AssertAsync(ExpectationsArranger.CreateArgs(LastLogs, result.Error).AddExtra(res.Response)).ConfigureAwait(false);

            return res.IsSuccess ? new HttpResponseMessageAssertor<TValue>(Owner, res.Value, res.Response) : new HttpResponseMessageAssertor<TValue>(Owner, res.Response);
        }

        /// <summary>
        /// Runs the test by executing a <typeparamref name="TAgent"/> method.
        /// </summary>
        /// <param name="func">The function to execution.</param>
        /// <returns>An <see cref="HttpResponseMessageAssertor{TValue}"/>.</returns>
        public async Task<HttpResponseMessageAssertor<TValue>> RunAsync(Func<TAgent, Task<HttpResponseMessage>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));

            using var scope = this.CreateClientScope<TAgent>();
            var agent = scope.ServiceProvider.GetRequiredService<TAgent>();
            var res = await func(agent).ConfigureAwait(false);

            await Task.Delay(TestSetUp.TaskDelayMilliseconds).ConfigureAwait(false);
            await ExpectationsArranger.AssertAsync(ExpectationsArranger.CreateArgs(LastLogs).AddExtra(res)).ConfigureAwait(false);

            return new HttpResponseMessageAssertor<TValue>(Owner, res);
        }

        /// <summary>
        /// Perform the assertion of any expectations.
        /// </summary>
        /// <param name="res">The <see cref="HttpResponseMessage"/>/</param>
        protected override Task AssertExpectationsAsync(HttpResponseMessage res) => throw new NotImplementedException("This is performed internally; and therefore should not be invoked.");
    }
}