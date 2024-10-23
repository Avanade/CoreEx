// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using UnitTestEx.Abstractions;
using UnitTestEx.Assertors;
using Ceh = CoreEx.Http;

namespace UnitTestEx.AspNetCore
{
    /// <summary>
    /// Provides <b>HTTP Agent</b> <see cref="Ceh.TypedHttpClientBase"/> testing.
    /// </summary>
    /// <param name="owner">The owning <see cref="TesterBase"/>.</param>
    /// <param name="testServer">The <see cref="TestServer"/>.</param>
    public class AgentTester<TAgent>(TesterBase owner, TestServer testServer) : HttpTesterBase<AgentTester<TAgent>>(owner, testServer) where TAgent : Ceh.TypedHttpClientBase
    {
        /// <summary>
        /// Runs the test by executing a <typeparamref name="TAgent"/> method.
        /// </summary>
        /// <param name="func">The function to execution.</param>
        /// <returns>An <see cref="HttpResultAssertor"/>.</returns>
        public HttpResultAssertor Run(Func<TAgent, Task<Ceh.HttpResult>> func) => RunAsync(func).GetAwaiter().GetResult();

        /// <summary>
        /// Runs the test by executing a <typeparamref name="TAgent"/> method.
        /// </summary>
        /// <param name="func">The function to execution.</param>
        /// <returns>An <see cref="HttpResultAssertor"/>.</returns>
        public HttpResultAssertor<TValue> Run<TValue>(Func<TAgent, Task<Ceh.HttpResult<TValue>>> func) => RunAsync(func).GetAwaiter().GetResult();

        /// <summary>
        /// Runs the test by executing a <typeparamref name="TAgent"/> method.
        /// </summary>
        /// <param name="func">The function to execution.</param>
        /// <returns>An <see cref="HttpResponseMessageAssertor"/>.</returns>
        public HttpResponseMessageAssertor Run(Func<TAgent, Task<HttpResponseMessage>> func) => RunAsync(func).GetAwaiter().GetResult();

        /// <summary>
        /// Runs the test by executing a <typeparamref name="TAgent"/> method.
        /// </summary>
        /// <param name="func">The function to execution.</param>
        /// <returns>An <see cref="HttpResultAssertor"/>.</returns>
        public async Task<HttpResultAssertor> RunAsync(Func<TAgent, Task<Ceh.HttpResult>> func)
        {
            func.ThrowIfNull(nameof(func));

            using var scope = this.CreateClientScope<TAgent>();
            var agent = scope.ServiceProvider.GetRequiredService<TAgent>();
            var res = await func(agent).ConfigureAwait(false);

            await Task.Delay(TestSetUp.TaskDelayMilliseconds).ConfigureAwait(false);

            var result = res.ToResult();
            await ExpectationsArranger.AssertAsync(ExpectationsArranger.CreateArgs(LastLogs, result.IsFailure ? result.Error : null).AddExtra(res.Response)).ConfigureAwait(false);

            return new HttpResultAssertor(Owner, res);
        }

        /// <summary>
        /// Runs the test by executing a <typeparamref name="TAgent"/> method.
        /// </summary>
        /// <param name="func">The function to execution.</param>
        /// <returns>An <see cref="HttpResultAssertor"/>.</returns>
        public async Task<HttpResultAssertor<TValue>> RunAsync<TValue>(Func<TAgent, Task<Ceh.HttpResult<TValue>>> func)
        {
            func.ThrowIfNull(nameof(func));

            using var scope = this.CreateClientScope<TAgent>();
            var agent = scope.ServiceProvider.GetRequiredService<TAgent>();
            var res = await func(agent).ConfigureAwait(false);

            await Task.Delay(TestSetUp.TaskDelayMilliseconds).ConfigureAwait(false);

            var result = res.ToResult();
            if (res.IsSuccess)
                await ExpectationsArranger.AssertAsync(ExpectationsArranger.CreateValueArgs(LastLogs, result.Value).AddExtra(res.Response)).ConfigureAwait(false);
            else
                await ExpectationsArranger.AssertAsync(ExpectationsArranger.CreateArgs(LastLogs, result.Error).AddExtra(res.Response)).ConfigureAwait(false);

            return res.IsSuccess ? new HttpResultAssertor<TValue>(Owner, res.Value, res) : new HttpResultAssertor<TValue>(Owner, res);
        }

        /// <summary>
        /// Runs the test by executing a <typeparamref name="TAgent"/> method.
        /// </summary>
        /// <param name="func">The function to execution.</param>
        /// <returns>An <see cref="HttpResponseMessageAssertor"/>.</returns>
        public async Task<HttpResponseMessageAssertor> RunAsync(Func<TAgent, Task<HttpResponseMessage>> func)
        {
            func.ThrowIfNull(nameof(func));

            using var scope = this.CreateClientScope<TAgent>();
            var agent = scope.ServiceProvider.GetRequiredService<TAgent>();
            var res = await func(agent).ConfigureAwait(false);

            await Task.Delay(TestSetUp.TaskDelayMilliseconds).ConfigureAwait(false);
            await ExpectationsArranger.AssertAsync(ExpectationsArranger.CreateArgs(LastLogs).AddExtra(res)).ConfigureAwait(false);

            return new HttpResponseMessageAssertor(Owner, res);
        }
        /// <summary>
        /// Perform the assertion of any expectations.
        /// </summary>
        /// <param name="res">The <see cref="HttpResponseMessage"/>/</param>
        protected override Task AssertExpectationsAsync(HttpResponseMessage res) => throw new NotImplementedException("This is performed internally; and therefore should not be invoked.");
    }
}