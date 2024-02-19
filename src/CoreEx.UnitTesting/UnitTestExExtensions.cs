// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Azure.Core.Amqp;
using Azure.Messaging.ServiceBus;
using CoreEx;
using CoreEx.AspNetCore.Http;
using CoreEx.AspNetCore.WebApis;
using CoreEx.Azure.ServiceBus;
using CoreEx.Events;
using CoreEx.Http;
using CoreEx.Mapping.Converters;
using CoreEx.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq.Expressions;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;
using UnitTestEx.Abstractions;
using UnitTestEx.AspNetCore;
using UnitTestEx.Assertors;
using UnitTestEx.Functions;
using UnitTestEx.Generic;
using UnitTestEx.Json;
using Ceh = CoreEx.Http;

namespace UnitTestEx
{
    /// <summary>
    /// Provides extension methods to the core <see href="https://github.com/Avanade/unittestex"/>.
    /// </summary>
    public static class UnitTestExExtensions
    {
        #region IJsonSerializer

        /// <summary>
        /// Map (convert) the <see cref="CoreEx.Json.IJsonSerializer"/> to a <see cref="UnitTestEx.Json.IJsonSerializer"/>.
        /// </summary>
        /// <param name="jsonSerializer">The <see cref="CoreEx.Json.IJsonSerializer"/>.</param>
        /// <returns>The <see cref="UnitTestEx.Json.IJsonSerializer"/> (see <see cref="ToUnitTestExJsonSerializerMapper"/>).</returns>
        public static IJsonSerializer ToUnitTestEx(this CoreEx.Json.IJsonSerializer jsonSerializer) => new ToUnitTestExJsonSerializerMapper(jsonSerializer);

        /// <summary>
        /// Map (convert) the <see cref="UnitTestEx.Json.IJsonSerializer"/> to a <see cref="CoreEx.Json.IJsonSerializer"/>.
        /// </summary>
        /// <param name="jsonSerializer">The <see cref="UnitTestEx.Json.IJsonSerializer"/>.</param>
        /// <returns>The <see cref="CoreEx.Json.IJsonSerializer"/> (see <see cref="ToCoreExJsonSerializerMapper"/>).</returns>
        public static CoreEx.Json.IJsonSerializer ToCoreEx(this IJsonSerializer jsonSerializer) => new ToCoreExJsonSerializerMapper(jsonSerializer);

        /// <summary>
        /// Updates the <see cref="TesterBase.JsonSerializer"/> used by the <see cref="TesterBase{TSelf}"/> itself, not the underlying executing host which should be configured separately.
        /// </summary>
        /// <typeparam name="TSelf">The <typeparamref name="TSelf"/> to support fluent-style method-chaining.</typeparam>
        /// <param name="tester">The <see cref="TesterBase{TSelf}"/>.</param>
        /// <param name="jsonSerializer">The <see cref="JsonSerializer"/>.</param>
        /// <returns>The <typeparamref name="TSelf"/> to support fluent-style method-chaining.</returns>
        public static TSelf UseJsonSerializer<TSelf>(this TesterBase<TSelf> tester, CoreEx.Json.IJsonSerializer jsonSerializer) where TSelf : TesterBase<TSelf>
            => tester.UseJsonSerializer((jsonSerializer.ThrowIfNull(nameof(jsonSerializer))).ToUnitTestEx());

        #endregion

        #region CreateClientScope

        /// <summary>
        /// Creates a client-side <see cref="IServiceScope"/> from the <see cref="HttpTesterBase"/> enabling the <typeparamref name="TAgent"/>.
        /// </summary>
        /// <typeparam name="TAgent">The Agent (inherits from <see cref="TypedHttpClientBase"/>) <see cref="Type"/>.</typeparam>
        /// <param name="tester">The <see cref="HttpTesterBase"/>.</param>
        /// <returns>The <see cref="IServiceScope"/>.</returns>
        public static IServiceScope CreateClientScope<TAgent>(this HttpTesterBase tester) where TAgent : TypedHttpClientBase
        {
            var sc = new ServiceCollection();
            sc.AddExecutionContext(sp => new CoreEx.ExecutionContext { UserName = tester.UserName ?? tester.Owner.SetUp.DefaultUserName });

            if (tester.JsonSerializer is CoreEx.Json.IJsonSerializer cjs)
                sc.AddSingleton(cjs);
            else
                throw new InvalidOperationException($"The {nameof(HttpTesterBase)} must use a {nameof(IJsonSerializer)} that implements CoreEx.Json.IJsonSerializer to leverage Agent {typeof(TAgent).Name}.");

            sc.AddLogging(lb => { lb.SetMinimumLevel(tester.Owner.SetUp.MinimumLogLevel); lb.ClearProviders(); lb.AddProvider(tester.Owner.LoggerProvider); });
            sc.AddSingleton(new HttpClient(new HttpTesterBase.HttpDelegatingHandler(tester, tester.TestServer.CreateHandler())) { BaseAddress = tester.TestServer.BaseAddress });
            sc.AddSingleton(tester.Owner.SharedState);
            sc.AddSingleton(tester.Owner.Configuration);
            sc.AddDefaultSettings();
            sc.AddScoped<TAgent>();
            return sc.BuildServiceProvider().CreateScope();
        }

        #endregion

        #region FunctionTesterBase

        /// <summary>
        /// Creates a new <see cref="HttpRequest"/> with no body.
        /// </summary>
        /// <param name="tester">The tester.</param>
        /// <param name="httpMethod">The <see cref="HttpMethod"/>.</param>
        /// <param name="requestUri">The requuest uri.</param>
        /// <param name="requestOptions">The optional <see cref="Ceh.HttpRequestOptions"/>.</param>
        /// <returns>The <see cref="HttpRequest"/>.</returns>
        public static HttpRequest CreateHttpRequest<TEntryPoint, TSelf>(this FunctionTesterBase<TEntryPoint, TSelf> tester, HttpMethod httpMethod, string? requestUri, Ceh.HttpRequestOptions? requestOptions = null)
            where TEntryPoint : class, new() where TSelf : FunctionTesterBase<TEntryPoint, TSelf>
            => tester.CreateHttpRequest(httpMethod, requestUri).ApplyRequestOptions(requestOptions);

        /// <summary>
        /// Creates a new <see cref="HttpRequest"/> with no body.
        /// </summary>
        /// <param name="tester">The tester.</param>
        /// <param name="httpMethod">The <see cref="HttpMethod"/>.</param>
        /// <param name="requestUri">The requuest uri.</param>
        /// <param name="requestOptions">The optional <see cref="Ceh.HttpRequestOptions"/>.</param>
        /// <param name="requestModifier">The optional <see cref="HttpRequest"/> modifier.</param>
        /// <returns>The <see cref="HttpRequest"/>.</returns>
        public static HttpRequest CreateHttpRequest<TEntryPoint, TSelf>(this FunctionTesterBase<TEntryPoint, TSelf> tester, HttpMethod httpMethod, string? requestUri, Ceh.HttpRequestOptions? requestOptions = null, Action<HttpRequest>? requestModifier = null)
            where TEntryPoint : class, new() where TSelf : FunctionTesterBase<TEntryPoint, TSelf>
            => tester.CreateHttpRequest(httpMethod, requestUri, requestModifier).ApplyRequestOptions(requestOptions);

        /// <summary>
        /// Creates a new <see cref="HttpRequest"/> with <paramref name="body"/> (defaults <see cref="HttpRequest.ContentType"/> to <see cref="MediaTypeNames.Text.Plain"/>).
        /// </summary>
        /// <param name="tester">The tester.</param>
        /// <param name="httpMethod">The <see cref="HttpMethod"/>.</param>
        /// <param name="requestUri">The requuest uri.</param>
        /// <param name="body">The optional body content.</param>
        /// <param name="requestOptions">The optional <see cref="Ceh.HttpRequestOptions"/>.</param>
        /// <returns>The <see cref="HttpRequest"/>.</returns>
        public static HttpRequest CreateHttpRequest<TEntryPoint, TSelf>(this FunctionTesterBase<TEntryPoint, TSelf> tester, HttpMethod httpMethod, string? requestUri, string? body, Ceh.HttpRequestOptions? requestOptions = null)
            where TEntryPoint : class, new() where TSelf : FunctionTesterBase<TEntryPoint, TSelf>
            => tester.CreateHttpRequest(httpMethod, requestUri, body, null, null).ApplyRequestOptions(requestOptions);

        /// <summary>
        /// Creates a new <see cref="HttpRequest"/> with <paramref name="body"/> and <paramref name="contentType"/>.
        /// </summary>
        /// <param name="tester">The tester.</param>
        /// <param name="httpMethod">The <see cref="HttpMethod"/>.</param>
        /// <param name="requestUri">The requuest uri.</param>
        /// <param name="body">The optional body content.</param>
        /// <param name="contentType">The content type. Defaults to <see cref="MediaTypeNames.Text.Plain"/>.</param>
        /// <param name="requestOptions">The optional <see cref="Ceh.HttpRequestOptions"/>.</param>
        /// <returns>The <see cref="HttpRequest"/>.</returns>
        public static HttpRequest CreateHttpRequest<TEntryPoint, TSelf>(this FunctionTesterBase<TEntryPoint, TSelf> tester, HttpMethod httpMethod, string? requestUri, string? body, string? contentType, Ceh.HttpRequestOptions? requestOptions = null)
            where TEntryPoint : class, new() where TSelf : FunctionTesterBase<TEntryPoint, TSelf>
            => tester.CreateHttpRequest(httpMethod, requestUri, body, contentType, null).ApplyRequestOptions(requestOptions);

        /// <summary>
        /// Creates a new <see cref="HttpRequest"/> with the <paramref name="value"/> JSON serialized as <see cref="HttpRequest.ContentType"/> of <see cref="MediaTypeNames.Application.Json"/>.
        /// </summary>
        /// <param name="tester">The tester.</param>
        /// <param name="httpMethod">The <see cref="HttpMethod"/>.</param>
        /// <param name="requestUri">The requuest uri.</param>
        /// <param name="value">The value to JSON serialize.</param>
        /// <param name="requestOptions">The optional <see cref="Ceh.HttpRequestOptions"/> modifier.</param>
        /// <returns>The <see cref="HttpRequest"/>.</returns>
        public static HttpRequest CreateJsonHttpRequest<TEntryPoint, TSelf>(this FunctionTesterBase<TEntryPoint, TSelf> tester, HttpMethod httpMethod, string? requestUri, object? value, Ceh.HttpRequestOptions? requestOptions)
            where TEntryPoint : class, new() where TSelf : FunctionTesterBase<TEntryPoint, TSelf>
            => tester.CreateJsonHttpRequest(httpMethod, requestUri, value).ApplyRequestOptions(requestOptions);

        /// <summary>
        /// Creates a new <see cref="HttpRequest"/> with the <paramref name="value"/> JSON serialized as <see cref="HttpRequest.ContentType"/> of <see cref="MediaTypeNames.Application.Json"/>.
        /// </summary>
        /// <param name="tester">The tester.</param>
        /// <param name="httpMethod">The <see cref="HttpMethod"/>.</param>
        /// <param name="requestUri">The requuest uri.</param>
        /// <param name="value">The value to JSON serialize.</param>
        /// <param name="requestOptions">The optional <see cref="Ceh.HttpRequestOptions"/> modifier.</param>
        /// <param name="requestModifier">The optional <see cref="HttpRequest"/> modifier.</param>
        /// <returns>The <see cref="HttpRequest"/>.</returns>
        public static HttpRequest CreateJsonHttpRequest<TEntryPoint, TSelf>(this FunctionTesterBase<TEntryPoint, TSelf> tester, HttpMethod httpMethod, string? requestUri, object? value, Ceh.HttpRequestOptions? requestOptions, Action<HttpRequest>? requestModifier = null)
            where TEntryPoint : class, new() where TSelf : FunctionTesterBase<TEntryPoint, TSelf>
            => tester.CreateJsonHttpRequest(httpMethod, requestUri, value, requestModifier).ApplyRequestOptions(requestOptions);

        #endregion

        #region ActionResultAssertor

        /// <summary>
        /// Asserts that the <see cref="ValueContentResult.ETag"/> matches the <paramref name="expectedETag"/>.
        /// </summary>
        /// <param name="assertor">The assertor.</param>
        /// <param name="expectedETag">The expected ETag value.</param>
        /// <returns>The <see cref="ActionResultAssertor"/> to support fluent-style method-chaining.</returns>
        public static ActionResultAssertor AssertETagHeader(this ActionResultAssertor assertor, string expectedETag)
        {
            if (assertor.Result != null && assertor.Result is ValueContentResult vcr)
                assertor.Owner.Implementor.AssertAreEqual(expectedETag, vcr.ETag, $"Expected and Actual {nameof(ValueContentResult.ETag)} values are not equal.");
            else
                assertor.Owner.Implementor.AssertFail($"The Result must be of Type {typeof(ValueContentResult).FullName} to use {nameof(AssertETagHeader)}.");

            return assertor;
        }

        /// <summary>
        /// Asserts that the <see cref="ValueContentResult.Location"/> matches the <paramref name="expectedUri"/>.
        /// </summary>
        /// <param name="assertor">The assertor.</param>
        /// <param name="expectedUri">The expected <see cref="Uri"/>.</param>
        /// <returns>The <see cref="ActionResultAssertor"/> to support fluent-style method-chaining.</returns>
        public static ActionResultAssertor AssertLocationHeader(this ActionResultAssertor assertor, Uri expectedUri)
        {
            if (assertor.Result != null && assertor.Result is ValueContentResult vcr)
                assertor.Owner.Implementor.AssertAreEqual(expectedUri, vcr.Location, $"Expected and Actual {nameof(ValueContentResult.Location)} values are not equal.");
            else if (assertor.Result != null && assertor.Result is ExtendedStatusCodeResult escr)
                assertor.Owner.Implementor.AssertAreEqual(expectedUri, escr.Location, $"Expected and Actual {nameof(ExtendedStatusCodeResult.Location)} values are not equal.");
            else
                assertor.Owner.Implementor.AssertFail($"The Result must be of Type {typeof(ValueContentResult).FullName} or {typeof(ExtendedStatusCodeResult).FullName} to use {nameof(AssertLocationHeader)}.");

            return assertor;
        }

        /// <summary>
        /// Asserts that the <see cref="ValueContentResult.Location"/> matches the <paramref name="expectedUri"/> function.
        /// </summary>
        /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
        /// <param name="assertor">The assertor.</param>
        /// <param name="expectedUri">The expected <see cref="Uri"/> function.</param>
        /// <returns>The <see cref="ActionResultAssertor"/> to support fluent-style method-chaining.</returns>
        public static ActionResultAssertor AssertLocationHeader<TValue>(this ActionResultAssertor assertor, Func<TValue, Uri> expectedUri)
            => assertor.AssertLocationHeader(expectedUri.Invoke(assertor.GetValue<TValue>()!));

        /// <summary>
        /// Asserts that the <see cref="ValueContentResult.Location"/> contains the <paramref name="expected"/> string.
        /// </summary>
        /// <param name="assertor">The assertor.</param>
        /// <param name="expected">The expected string.</param>
        /// <returns>The <see cref="ActionResultAssertor"/> to support fluent-style method-chaining.</returns>
        public static ActionResultAssertor AssertLocationHeaderContains(this ActionResultAssertor assertor, string expected)
        {
            Uri? actual = null;
            if (assertor.Result != null && assertor.Result is ValueContentResult vcr)
                actual = vcr.Location;
            else if (assertor.Result != null && assertor.Result is ExtendedStatusCodeResult escr)
                actual = escr.Location;
            else
                assertor.Owner.Implementor.AssertFail($"The Result must be of Type {typeof(ValueContentResult).FullName} or {typeof(ExtendedStatusCodeResult).FullName} to use {nameof(AssertLocationHeader)}.");

            if (actual == null)
                assertor.Owner.Implementor.AssertFail($"The actual {nameof(ValueContentResult.Location)} must not be null.");

            if (!actual!.ToString().Contains(expected))
                assertor.Owner.Implementor.AssertFail($"The {nameof(ValueContentResult.Location)} '{actual}' must contain {expected}.");

            return assertor;
        }

        /// <summary>
        /// Asserts that the <see cref="ValueContentResult.Location"/> contains the <paramref name="expected"/> string function.
        /// </summary>
        /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
        /// <param name="assertor">The assertor.</param>
        /// <param name="expected">The expected string function.</param>
        /// <returns>The <see cref="ActionResultAssertor"/> to support fluent-style method-chaining.</returns>
        public static ActionResultAssertor AssertLocationHeader<TValue>(this ActionResultAssertor assertor, Func<TValue, string> expected)
            => assertor.AssertLocationHeaderContains(expected.Invoke(assertor.GetValue<TValue>()!));

        #endregion

        #region GenericTesterBase

        /// <summary>
        /// Enables the validation <see cref="GenericTesterBaseWith{TEntryPoint, TSelf}">with</see> a specified validation.
        /// </summary>
        /// <typeparam name="TEntryPoint">The API startup <see cref="Type"/>.</typeparam>
        /// <typeparam name="TSelf">The <see cref="ApiTesterBase{TEntryPoint, TSelf}"/>.</typeparam>
        /// <param name="tester">The tester.</param>
        /// <param name="operationType">The optional <see cref="OperationType"/> for the test (updates the <see cref="ExecutionContext.Current"/>).</param>
        public static GenericTesterBaseWith<TEntryPoint, TSelf> Validation<TEntryPoint, TSelf>(this GenericTesterBase<TEntryPoint, TSelf> tester, OperationType operationType = OperationType.Unspecified) where TEntryPoint : class, new() where TSelf : GenericTesterBase<TEntryPoint, TSelf>
            => new(tester, operationType);

        /// <summary>
        /// Enables the validation.
        /// </summary>
        /// <typeparam name="TEntryPoint">The API startup <see cref="Type"/>.</typeparam>
        /// <typeparam name="TSelf">The <see cref="ApiTesterBase{TEntryPoint, TSelf}"/>.</typeparam>
        public class GenericTesterBaseWith<TEntryPoint, TSelf> where TEntryPoint : class, new() where TSelf : GenericTesterBase<TEntryPoint, TSelf>
        {
            private readonly GenericTesterBase<TEntryPoint, TSelf> _tester;
            private readonly OperationType _operationType;

            /// <summary>
            /// Initializes a new instance of the <see cref="AgentTesterWith{TEntryPoint, TSelf}"/>.
            /// </summary>
            internal GenericTesterBaseWith(GenericTesterBase<TEntryPoint, TSelf> tester, OperationType operationType)
            {
                _tester = tester.ThrowIfNull(nameof(tester));
                _operationType = operationType;
            }

            /// <summary>
            /// Creates (instantiates) the <typeparamref name="TValidator"/> using Dependency Injection (DI) and validates the <typeparamref name="TValue"/> <paramref name="value"/>.
            /// </summary>
            /// <typeparam name="TValidator">The validator <see cref="Type"/>.</typeparam>
            /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
            /// <param name="value">The value to validate.</param>
            /// <returns>The <see cref="ValueAssertor{TValue}"/> with the resulting <see cref="IValidationResult"/>.</returns>
            public ValueAssertor<IValidationResult> With<TValidator, TValue>(TValue value) where TValue : class where TValidator : class, IValidator<TValue>
                => WithAsync<TValidator, TValue>(value).GetAwaiter().GetResult();

            /// <summary>
            /// Validates the <typeparamref name="TValue"/> <paramref name="value"/> using the <paramref name="validator"/>.
            /// </summary>
            /// <typeparam name="TValidator">The validator <see cref="Type"/>.</typeparam>
            /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
            /// <param name="validator">The validator.</param>
            /// <param name="value">The value to validate.</param>
            /// <returns>The <see cref="ValueAssertor{TValue}"/> with the resulting <see cref="IValidationResult"/>.</returns>
            public ValueAssertor<IValidationResult> With<TValidator, TValue>(TValidator validator, TValue value) where TValue : class where TValidator : class, IValidator<TValue>
                => WithAsync(validator, value).GetAwaiter().GetResult();

            /// <summary>
            /// Executes the <paramref name="validation"/> function.
            /// </summary>
            /// <param name="validation">The validation function.</param>
            /// <returns>The <see cref="ValueAssertor{TValue}"/> with the resulting <see cref="IValidationResult"/>.</returns>
            public ValueAssertor<IValidationResult> With(Func<IValidationResult> validation) => WithAsync(() => Task.FromResult(validation())).GetAwaiter().GetResult();

            /// <summary>
            /// Executes the <paramref name="validation"/> function.
            /// </summary>
            /// <param name="validation">The validation function.</param>
            /// <returns>The <see cref="ValueAssertor{TValue}"/> with the resulting <see cref="IValidationResult"/>.</returns>
            public ValueAssertor<IValidationResult> With(Func<Task<IValidationResult>> validation) => WithAsync(validation).GetAwaiter().GetResult();

            /// <summary>
            /// Creates (instantiates) the <typeparamref name="TValidator"/> using Dependency Injection (DI) and validates the <typeparamref name="TValue"/> <paramref name="value"/>.
            /// </summary>
            /// <typeparam name="TValidator">The validator <see cref="Type"/>.</typeparam>
            /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
            /// <param name="value">The value to validate.</param>
            /// <returns>The <see cref="ValueAssertor{TValue}"/> with the resulting <see cref="IValidationResult"/>.</returns>
            public Task<ValueAssertor<IValidationResult>> WithAsync<TValidator, TValue>(TValue value) where TValue : class where TValidator : class, IValidator<TValue>
                => WithAsync(_tester.Services.GetService<TValidator>() ?? throw new InvalidOperationException($"Validator '{typeof(TValidator).FullName}' not configured using Dependency Injection (DI) and therefore unable to be instantiated for testing."), value);

            /// <summary>
            /// Validates the <typeparamref name="TValue"/> <paramref name="value"/> using the <paramref name="validator"/>.
            /// </summary>
            /// <typeparam name="TValidator">The validator <see cref="Type"/>.</typeparam>
            /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
            /// <param name="validator">The validator.</param>
            /// <param name="value">The value to validate.</param>
            /// <returns>The <see cref="ValueAssertor{TValue}"/> with the resulting <see cref="IValidationResult"/>.</returns>
            public Task<ValueAssertor<IValidationResult>> WithAsync<TValidator, TValue>(TValidator validator, TValue value) where TValue : class where TValidator : class, IValidator<TValue>
                => WithAsync(async () => await validator.ThrowIfNull(nameof(validator)).ValidateAsync(value).ConfigureAwait(false));

            /// <summary>
            /// Executes the <paramref name="validation"/> function.
            /// </summary>
            /// <param name="validation">The validation function.</param>
            /// <returns>The <see cref="ValueAssertor{TValue}"/> with the resulting <see cref="IValidationResult"/>.</returns>
            public Task<ValueAssertor<IValidationResult>> WithAsync(Func<Task<IValidationResult>> validation)
                => _tester.RunAsync(async () =>
                {
                    // Build out an execution context.
                    OperationType? existingOperationType = null;
                    if (ExecutionContext.HasCurrent)
                        existingOperationType = ExecutionContext.Current.OperationType;
                    else
                    {
                        var ec = _tester.Services.GetService<ExecutionContext>();
                        if (ec is null)
                            _ = ExecutionContext.Current;
                        else if (!ExecutionContext.HasCurrent)
                            ExecutionContext.SetCurrent(ec);

                        // Update service provider where null.
                        ExecutionContext.Current.ServiceProvider ??= _tester.Services;
                    }

                    // Set/override the operation type.
                    ExecutionContext.Current.OperationType = _operationType;

                    // Perform the validation.
                    try
                    {
                        var vr = await validation().ConfigureAwait(false);
                        vr.ThrowOnError();
                        return vr;
                    }
                    finally
                    {
                        // Reset where finished.
                        if (existingOperationType.HasValue)
                            ExecutionContext.Current.OperationType = existingOperationType.Value;
                        else
                            ExecutionContext.Reset();
                    }
                });
        }

        #endregion

        #region ApiTesterBase

        /// <summary>
        /// Enables a test <see cref="HttpRequestMessage"/> to be sent to the underlying <see cref="TestServer"/> <see cref="AgentTesterWith{TEntryPoint, TSelf}">with</see> a specified <see cref="TypedHttpClientBase">agent</see>.
        /// </summary>
        /// <typeparam name="TEntryPoint">The API startup <see cref="Type"/>.</typeparam>
        /// <typeparam name="TSelf">The <see cref="ApiTesterBase{TEntryPoint, TSelf}"/>.</typeparam>
        /// <param name="tester">The tester.</param>
        /// <returns>The <see cref="AgentTesterWith{TEntryPoint, TSelf}"/> to allow <b>Agent</b> type specification.</returns>
        public static AgentTesterWith<TEntryPoint, TSelf> Agent<TEntryPoint, TSelf>(this ApiTesterBase<TEntryPoint, TSelf> tester)
            where TEntryPoint : class where TSelf : ApiTesterBase<TEntryPoint, TSelf>
            => new(tester);

        /// <summary>
        /// Enables the <see cref="TypedHttpClientBase">agent</see> <see cref="Type"/> specification.
        /// </summary>
        /// <typeparam name="TEntryPoint">The API startup <see cref="Type"/>.</typeparam>
        /// <typeparam name="TSelf">The <see cref="ApiTesterBase{TEntryPoint, TSelf}"/>.</typeparam>
        public class AgentTesterWith<TEntryPoint, TSelf> where TEntryPoint : class where TSelf : ApiTesterBase<TEntryPoint, TSelf>
        {
            private readonly ApiTesterBase<TEntryPoint, TSelf> _tester;

            /// <summary>
            /// Initializes a new instance of the <see cref="AgentTesterWith{TEntryPoint, TSelf}"/>.
            /// </summary>
            internal AgentTesterWith(ApiTesterBase<TEntryPoint, TSelf> tester) => _tester = tester.ThrowIfNull(nameof(tester));

            /// <summary>
            /// Enables a test <see cref="HttpRequestMessage"/> to be sent to the underlying <see cref="TestServer"/> leveraging the specified <see cref="TypedHttpClientBase">agent</see>.
            /// </summary>
            /// <typeparam name="TAgent">The <see cref="AgentTester{TAgent}"/>.</typeparam>
            /// <returns>The <see cref="AgentTester{TAgent}"/>.</returns>
            public AgentTester<TAgent> With<TAgent>() where TAgent : TypedHttpClientBase => new(_tester, _tester.GetTestServer());

            /// <summary>
            /// Enables a test <see cref="HttpRequestMessage"/> to be sent to the underlying <see cref="TestServer"/> leveraging the specified <see cref="TypedHttpClientBase">agent</see>.
            /// </summary>
            /// <typeparam name="TAgent">The <see cref="AgentTester{TAgent}"/>.</typeparam>
            /// <typeparam name="TValue">The response value <see cref="Type"/>.</typeparam>
            /// <returns>The <see cref="AgentTester{TAgent, TValue}"/>.</returns>
            public AgentTester<TAgent, TValue> With<TAgent, TValue>() where TAgent : TypedHttpClientBase => new(_tester, _tester.GetTestServer());
        }

        #endregion

        #region ControllerTester

        /// <summary>
        /// Runs the controller using an <see cref="HttpRequestMessage"/> inferring the <see cref="HttpMethod"/>, operation name and request from the <paramref name="expression"/>.
        /// </summary>
        /// <typeparam name="TController">The <see cref="ControllerBase"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResult">The result value <see cref="Type"/>.</typeparam>
        /// <param name="tester">The tester.</param>
        /// <param name="expression">The controller operation invocation expression.</param>
        /// <param name="requestOptions">The optional <see cref="Ceh.HttpRequestOptions"/>.</param>
        /// <param name="requestModifier">The optional <see cref="HttpRequestMessage"/> modifier.</param>
        /// <returns>A <see cref="HttpResponseMessageAssertor"/>.</returns>
        public static HttpResponseMessageAssertor Run<TController, TResult>(this ControllerTester<TController> tester, Expression<Func<TController, TResult>> expression, Ceh.HttpRequestOptions? requestOptions = null, Action<HttpRequestMessage>? requestModifier = null)
            where TController : ControllerBase
            => RunAsync(tester, expression, requestOptions, requestModifier).GetAwaiter().GetResult();

        /// <summary>
        /// Runs the controller using an <see cref="HttpRequestMessage"/> inferring the <see cref="HttpMethod"/>, operation name and request from the <paramref name="expression"/>.
        /// </summary>
        /// <typeparam name="TController">The <see cref="ControllerBase"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResult">The result value <see cref="Type"/>.</typeparam>
        /// <param name="tester">The tester.</param>
        /// <param name="expression">The controller operation invocation expression.</param>
        /// <param name="requestOptions">The optional <see cref="Ceh.HttpRequestOptions"/>.</param>
        /// <param name="requestModifier">The optional <see cref="HttpRequestMessage"/> modifier.</param>
        /// <returns>A <see cref="HttpResponseMessageAssertor"/>.</returns>
        public static Task<HttpResponseMessageAssertor> RunAsync<TController, TResult>(this ControllerTester<TController> tester, Expression<Func<TController, TResult>> expression, Ceh.HttpRequestOptions? requestOptions = null, Action<HttpRequestMessage>? requestModifier = null)
            where TController : ControllerBase
        {
            void rm(HttpRequestMessage hr)
            {
                requestModifier?.Invoke(hr);
                hr.ApplyRequestOptions(requestOptions);
            }

            return tester.RunAsync(expression, rm);
        }

        /// <summary>
        /// Runs the controller using an <see cref="HttpRequestMessage"/> inferring the <see cref="HttpMethod"/>, operation name and request from the <paramref name="expression"/>.
        /// </summary>
        /// <typeparam name="TController">The <see cref="ControllerBase"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResult">The result value <see cref="Type"/>.</typeparam>
        /// <param name="tester">The tester.</param>
        /// <param name="expression">The controller operation invocation expression.</param>
        /// <param name="content">The body content.</param>
        /// <param name="contentType">The body content type. Defaults to <see cref="MediaTypeNames.Text.Plain"/>.</param>
        /// <param name="requestOptions">The optional <see cref="Ceh.HttpRequestOptions"/>.</param>
        /// <param name="requestModifier">The optional <see cref="HttpRequestMessage"/> modifier.</param>
        /// <returns>A <see cref="HttpResponseMessageAssertor"/>.</returns>
        public static HttpResponseMessageAssertor RunContent<TController, TResult>(this ControllerTester<TController> tester, Expression<Func<TController, TResult>> expression, string? content, string? contentType = MediaTypeNames.Text.Plain, Ceh.HttpRequestOptions? requestOptions = null, Action<HttpRequestMessage>? requestModifier = null)
            where TController : ControllerBase
            => RunContentAsync(tester, expression, content, contentType, requestOptions, requestModifier).GetAwaiter().GetResult();

        /// <summary>
        /// Runs the controller using an <see cref="HttpRequestMessage"/> inferring the <see cref="HttpMethod"/>, operation name and request from the <paramref name="expression"/>.
        /// </summary>
        /// <typeparam name="TController">The <see cref="ControllerBase"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResult">The result value <see cref="Type"/>.</typeparam>
        /// <param name="tester">The tester.</param>
        /// <param name="expression">The controller operation invocation expression.</param>
        /// <param name="content">The body content.</param>
        /// <param name="contentType">The body content type. Defaults to <see cref="MediaTypeNames.Text.Plain"/>.</param>
        /// <param name="requestOptions">The optional <see cref="Ceh.HttpRequestOptions"/>.</param>
        /// <param name="requestModifier">The optional <see cref="HttpRequestMessage"/> modifier.</param>
        /// <returns>A <see cref="HttpResponseMessageAssertor"/>.</returns>
        public static Task<HttpResponseMessageAssertor> RunContentAsync<TController, TResult>(this ControllerTester<TController> tester, Expression<Func<TController, TResult>> expression, string? content, string? contentType = MediaTypeNames.Text.Plain, Ceh.HttpRequestOptions? requestOptions = null, Action<HttpRequestMessage>? requestModifier = null)
            where TController : ControllerBase
        {
            void rm(HttpRequestMessage hr)
            {
                requestModifier?.Invoke(hr);
                hr.ApplyRequestOptions(requestOptions);
            }

            return tester.RunContentAsync(expression, content, contentType, rm);
        }

        #endregion

        #region TesterBase

        /// <summary>
        /// Creates a <see cref="ServiceBusReceivedMessage"/> from the <paramref name="event"/> leveraging the registered <see cref="EventDataToServiceBusConverter"/> to perform the underlying conversion.
        /// </summary>
        /// <typeparam name="TSelf">The <see cref="TesterBase{TSelf}"/>.</typeparam>
        /// <param name="tester">The tester.</param>
        /// <param name="event">The <see cref="EventData"/> or <see cref="EventData{T}"/> value.</param>
        /// <returns>The <see cref="ServiceBusReceivedMessage"/>.</returns>
        public static ServiceBusReceivedMessage CreateServiceBusMessage<TSelf>(this TesterBase<TSelf> tester, EventData @event) where TSelf : TesterBase<TSelf>
        {
            @event.ThrowIfNull(nameof(@event));
            var message = (tester.Services.GetService<EventDataToServiceBusConverter>() ?? new EventDataToServiceBusConverter(tester.Services.GetService<IEventSerializer>(), tester.Services.GetService<IValueConverter<EventSendData, ServiceBusMessage>>())).Convert(@event).GetRawAmqpMessage();
            tester.ResetHost(false);
            return tester.CreateServiceBusMessage(message);
        }

        /// <summary>
        /// Creates a <see cref="ServiceBusReceivedMessage"/> from the <paramref name="event"/> leveraging the registered <see cref="EventDataToServiceBusConverter"/> to perform the underlying conversion.
        /// </summary>
        /// <typeparam name="TSelf">The <see cref="TesterBase{TSelf}"/>.</typeparam>
        /// <param name="tester">The tester.</param>
        /// <param name="event">The <see cref="EventData"/> or <see cref="EventData{T}"/> value.</param>
        /// <param name="messageModify">Optional <see cref="AmqpAnnotatedMessage"/> modifier than enables the message to be further configured.</param>
        /// <returns>The <see cref="ServiceBusReceivedMessage"/>.</returns>
        public static ServiceBusReceivedMessage CreateServiceBusMessage<TSelf>(this TesterBase<TSelf> tester, EventData @event, Action<AmqpAnnotatedMessage>? messageModify) where TSelf : TesterBase<TSelf>
        {
            @event.ThrowIfNull(nameof(@event));
            var message = (tester.Services.GetService<EventDataToServiceBusConverter>() ?? new EventDataToServiceBusConverter(tester.Services.GetService<IEventSerializer>(), tester.Services.GetService<IValueConverter<EventSendData, ServiceBusMessage>>())).Convert(@event).GetRawAmqpMessage();
            tester.ResetHost(false);
            return tester.CreateServiceBusMessage(message, messageModify);
        }

        #endregion
    }
}