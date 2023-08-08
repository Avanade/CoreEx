// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using CoreEx.Events;
using CoreEx.Json;
using CoreEx.Results;
using CoreEx.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.AspNetCore.WebApis
{
    /// <summary>
    /// Provides the core <see cref="IEventSender"/> Web API execution encapsulation.
    /// </summary>
    public class WebApiPublisher : WebApiBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebApiPublisher"/> class.
        /// </summary>
        /// <param name="eventPublisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="executionContext">The <see cref="ExecutionContext"/>.</param>
        /// <param name="settings">The <see cref="SettingsBase"/>.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
        /// <param name="invoker">The <see cref="WebApiInvoker"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        public WebApiPublisher(IEventPublisher eventPublisher, ExecutionContext executionContext, SettingsBase settings, IJsonSerializer jsonSerializer, ILogger<WebApiPublisher> logger, WebApiInvoker? invoker = null)
            : base(executionContext, settings, jsonSerializer, logger, invoker) => EventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));

        /// <summary>
        /// Gets the <see cref="IEventPublisher"/>.
        /// </summary>
        public IEventPublisher EventPublisher { get; }

        #region PublishAsync

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> that is to be published using the <see cref="EventPublisher"/>.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="eventName">The optional event destintion name (e.g. Queue or Topic name).</param>
        /// <param name="beforeEvent">A function that enables the value to be processed/validated before the underlying event publishing logic is enacted.</param>
        /// <param name="eventModifier">An action to enable the <see cref="EventData"/> instance to be modified prior to publish.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> or <see cref="HttpMethods.Post"/>.</remarks>
        public Task<IActionResult> PublishAsync<TValue>(HttpRequest request, string? eventName, Func<WebApiParam<TValue>, CancellationToken, Task>? beforeEvent = null, Action<EventData, TValue>? eventModifier = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TValue>? validator = null, CancellationToken cancellationToken = default)
            => PublishInternalAsync(request, false, default!, eventName, beforeEvent, eventModifier, statusCode, operationType, validator, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> that is to be published using the <see cref="EventPublisher"/>.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The The value (already deserialized).</param>
        /// <param name="eventName">The optional event destintion name (e.g. Queue or Topic name).</param>
        /// <param name="beforeEvent">A function that enables the value to be processed/validated before the underlying event publishing logic is enacted.</param>
        /// <param name="eventModifier">An action to enable the <see cref="EventData"/> instance to be modified prior to publish.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> or <see cref="HttpMethods.Post"/>.</remarks>
        public Task<IActionResult> PublishAsync<TValue>(HttpRequest request, TValue value, string? eventName, Func<WebApiParam<TValue>, CancellationToken, Task>? beforeEvent = null, Action<EventData, TValue>? eventModifier = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TValue>? validator = null, CancellationToken cancellationToken = default)
            => PublishInternalAsync(request, true, value, eventName, beforeEvent, eventModifier, statusCode, operationType, validator, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> that is to be published using the <see cref="EventPublisher"/>.
        /// </summary>
        private async Task<IActionResult> PublishInternalAsync<TValue>(HttpRequest request, bool useValue, TValue value, string? eventName, Func<WebApiParam<TValue>, CancellationToken, Task>? beforeEvent = null, Action<EventData, TValue>? eventModifier = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TValue>? validator = null, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Method != HttpMethods.Post)
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Post}' to use {nameof(PublishAsync)}.", nameof(request));

            return await RunAsync(request, async (wap, ct) =>
            {
                var (wapv, vex) = await ValidateValueAsync(wap, useValue, value, true, validator, cancellationToken).ConfigureAwait(false);
                if (vex != null)
                    return await CreateActionResultFromExceptionAsync(this, request.HttpContext, vex, Settings, Logger, OnUnhandledExceptionAsync, cancellationToken).ConfigureAwait(false);

                if (beforeEvent != null)
                    await beforeEvent(wapv!, ct).ConfigureAwait(false);

                var @event = new EventData { Value = wapv!.Value };
                eventModifier?.Invoke(@event, wapv.Value!);

                if (eventName == null)
                    EventPublisher.Publish(@event);
                else
                    EventPublisher.PublishNamed(eventName, @event);

                await EventPublisher.SendAsync(cancellationToken).ConfigureAwait(false);

                return new ExtendedStatusCodeResult(statusCode);
            }, operationType, cancellationToken, nameof(PublishAsync)).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TColl"/> where each item is to be published using the <see cref="EventPublisher"/>.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/></typeparam>
        /// <typeparam name="TItem">The collection item <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="eventName">The optional event destintion name (e.g. Queue or Topic name).</param>
        /// <param name="beforeEvents">A function that enables the collection to be processed/validated before the underlying event publishing logic is enacted.</param>
        /// <param name="eventModifier">An action to enable each item <see cref="EventData"/> instance to be modified prior to publish.</param>
        /// <param name="maxCollSize">Overrides the default (see <see cref="SettingsBase.MaxPublishCollSize"/>) maximum publish collection size.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> or <see cref="HttpMethods.Post"/>.</remarks>
        public Task<IActionResult> PublishAsync<TColl, TItem>(HttpRequest request, string? eventName, Func<WebApiParam<TColl>, CancellationToken, Task>? beforeEvents = null, Action<EventData, TItem>? eventModifier = null, int? maxCollSize = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TColl>? validator = null, CancellationToken cancellationToken = default) where TColl : IEnumerable<TItem>
            => PublishInternalAsync(request, false, default!, eventName, beforeEvents, eventModifier, maxCollSize, statusCode, operationType, validator, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TColl"/> where each item is to be published using the <see cref="EventPublisher"/>.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/></typeparam>
        /// <typeparam name="TItem">The collection item <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The The value (already deserialized).</param>
        /// <param name="eventName">The optional event destintion name (e.g. Queue or Topic name).</param>
        /// <param name="beforeEvents">A function that enables the collection to be processed/validated before the underlying event publishing logic is enacted.</param>
        /// <param name="eventModifier">An action to enable each item <see cref="EventData"/> instance to be modified prior to publish.</param>
        /// <param name="maxCollSize">Overrides the default (see <see cref="SettingsBase.MaxPublishCollSize"/>) maximum publish collection size.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> or <see cref="HttpMethods.Post"/>.</remarks>
        public Task<IActionResult> PublishAsync<TColl, TItem>(HttpRequest request, TColl value, string? eventName, Func<WebApiParam<TColl>, CancellationToken, Task>? beforeEvents = null, Action<EventData, TItem>? eventModifier = null, int? maxCollSize = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TColl>? validator = null, CancellationToken cancellationToken = default) where TColl : IEnumerable<TItem>
            => PublishInternalAsync(request, true, value, eventName, beforeEvents, eventModifier, maxCollSize, statusCode, operationType, validator, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TColl"/> where each item is to be published using the <see cref="EventPublisher"/>.
        /// </summary>
        private async Task<IActionResult> PublishInternalAsync<TColl, TItem>(HttpRequest request, bool useValue, TColl value, string? eventName, Func<WebApiParam<TColl>, CancellationToken, Task>? beforeEvents = null, Action<EventData, TItem>? eventModifier = null, int? maxCollSize = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TColl>? validator = null, CancellationToken cancellationToken = default) where TColl : IEnumerable<TItem>
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Method != HttpMethods.Post)
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Post}' to use {nameof(PublishAsync)}.", nameof(request));

            return await RunAsync(request, async (wap, ct) =>
            {
                var (wapv, vex) = await ValidateValueAsync(wap, useValue, value, true, validator, cancellationToken).ConfigureAwait(false);
                if (vex != null)
                    return await CreateActionResultFromExceptionAsync(this, request.HttpContext, vex, Settings, Logger, OnUnhandledExceptionAsync, cancellationToken).ConfigureAwait(false);

                if (beforeEvents != null)
                    await beforeEvents(wapv!, ct).ConfigureAwait(false);

                var max = maxCollSize ?? Settings.MaxPublishCollSize;
                var count = wapv!.Value?.Count() ?? 0;
                if (count > max)
                {
                    Logger.LogWarning("The publish collection contains {EventsCount} items where only a maximum size of {MaxCollSize} is supported; request has been rejected.", count, max);
                    var bex = new BusinessException($"The publish collection contains {count} items where only a maximum size of {max} is supported.");
                    return await CreateActionResultFromExceptionAsync(this, request.HttpContext, bex, Settings, Logger, OnUnhandledExceptionAsync, cancellationToken).ConfigureAwait(false);
                }

                if (count == 0)
                    return new AcceptedResult();

                foreach (var item in wapv.Value!)
                {
                    var ed = new EventData { Value = item };
                    eventModifier?.Invoke(ed, item);
                    if (eventName == null)
                        EventPublisher.Publish(ed);
                    else
                        EventPublisher.PublishNamed(eventName, ed);
                }

                await EventPublisher.SendAsync(ct).ConfigureAwait(false);

                return new ExtendedStatusCodeResult(statusCode);
            }, operationType, cancellationToken, nameof(PublishAsync)).ConfigureAwait(false);
        }

        #endregion

        #region PublishWithResultAsync

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> that is to be published using the <see cref="EventPublisher"/> (with a <see cref="Result"/>).
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="eventName">The optional event destintion name (e.g. Queue or Topic name).</param>
        /// <param name="beforeEvent">A function that enables the value to be processed/validated before the underlying event publishing logic is enacted.</param>
        /// <param name="eventModifier">An action to enable the <see cref="EventData"/> instance to be modified prior to publish.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> or <see cref="HttpMethods.Post"/>.</remarks>
        public Task<IActionResult> PublishWithResultAsync<TValue>(HttpRequest request, string? eventName, Func<WebApiParam<TValue>, CancellationToken, Task<Result>>? beforeEvent = null, Action<EventData, TValue>? eventModifier = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TValue>? validator = null, CancellationToken cancellationToken = default)
            => PublishWithResultInternalAsync(request, false, default!, eventName, beforeEvent, eventModifier, statusCode, operationType, validator, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> that is to be published using the <see cref="EventPublisher"/> (with a <see cref="Result"/>).
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The The value (already deserialized).</param>
        /// <param name="eventName">The optional event destintion name (e.g. Queue or Topic name).</param>
        /// <param name="beforeEvent">A function that enables the value to be processed/validated before the underlying event publishing logic is enacted.</param>
        /// <param name="eventModifier">An action to enable the <see cref="EventData"/> instance to be modified prior to publish.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> or <see cref="HttpMethods.Post"/>.</remarks>
        public Task<IActionResult> PublishWithResultAsync<TValue>(HttpRequest request, TValue value, string? eventName, Func<WebApiParam<TValue>, CancellationToken, Task<Result>>? beforeEvent = null, Action<EventData, TValue>? eventModifier = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TValue>? validator = null, CancellationToken cancellationToken = default)
            => PublishWithResultInternalAsync(request, true, value, eventName, beforeEvent, eventModifier, statusCode, operationType, validator, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> that is to be published using the <see cref="EventPublisher"/> (with a <see cref="Result"/>).
        /// </summary>
        private async Task<IActionResult> PublishWithResultInternalAsync<TValue>(HttpRequest request, bool useValue, TValue value, string? eventName, Func<WebApiParam<TValue>, CancellationToken, Task<Result>>? beforeEvent = null, Action<EventData, TValue>? eventModifier = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TValue>? validator = null, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Method != HttpMethods.Post)
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Post}' to use {nameof(PublishWithResultAsync)}.", nameof(request));

            return await RunAsync(request, async (wap, ct) =>
            {
                var (wapv, vex) = await ValidateValueAsync(wap, useValue, value, true, validator, cancellationToken).ConfigureAwait(false);
                if (vex != null)
                    return await CreateActionResultFromExceptionAsync(this, request.HttpContext, vex, Settings, Logger, OnUnhandledExceptionAsync, cancellationToken).ConfigureAwait(false);

                if (beforeEvent != null)
                {
                    var result = await beforeEvent(wapv!, ct).ConfigureAwait(false);
                    if (result.IsFailure)
                        return await CreateActionResultFromExceptionAsync(this, request.HttpContext, result.Error, Settings, Logger, OnUnhandledExceptionAsync, cancellationToken).ConfigureAwait(false);
                }

                var @event = new EventData { Value = wapv!.Value };
                eventModifier?.Invoke(@event, wapv.Value!);

                if (eventName == null)
                    EventPublisher.Publish(@event);
                else
                    EventPublisher.PublishNamed(eventName, @event);

                await EventPublisher.SendAsync(cancellationToken).ConfigureAwait(false);

                return new ExtendedStatusCodeResult(statusCode);
            }, operationType, cancellationToken, nameof(PublishWithResultAsync)).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TColl"/> where each item is to be published using the <see cref="EventPublisher"/> (with a <see cref="Result"/>).
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/></typeparam>
        /// <typeparam name="TItem">The collection item <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="eventName">The optional event destintion name (e.g. Queue or Topic name).</param>
        /// <param name="beforeEvents">A function that enables the collection to be processed/validated before the underlying event publishing logic is enacted.</param>
        /// <param name="eventModifier">An action to enable each item <see cref="EventData"/> instance to be modified prior to publish.</param>
        /// <param name="maxCollSize">Overrides the default (see <see cref="SettingsBase.MaxPublishCollSize"/>) maximum publish collection size.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> or <see cref="HttpMethods.Post"/>.</remarks>
        public Task<IActionResult> PublishWithResultAsync<TColl, TItem>(HttpRequest request, string? eventName, Func<WebApiParam<TColl>, CancellationToken, Task<Result>>? beforeEvents = null, Action<EventData, TItem>? eventModifier = null, int? maxCollSize = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TColl>? validator = null, CancellationToken cancellationToken = default) where TColl : IEnumerable<TItem>
            => PublishWithResultInternalAsync(request, false, default!, eventName, beforeEvents, eventModifier, maxCollSize, statusCode, operationType, validator, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TColl"/> where each item is to be published using the <see cref="EventPublisher"/> (with a <see cref="Result"/>).
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/></typeparam>
        /// <typeparam name="TItem">The collection item <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The The value (already deserialized).</param>
        /// <param name="eventName">The optional event destintion name (e.g. Queue or Topic name).</param>
        /// <param name="beforeEvents">A function that enables the collection to be processed/validated before the underlying event publishing logic is enacted.</param>
        /// <param name="eventModifier">An action to enable each item <see cref="EventData"/> instance to be modified prior to publish.</param>
        /// <param name="maxCollSize">Overrides the default (see <see cref="SettingsBase.MaxPublishCollSize"/>) maximum publish collection size.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> or <see cref="HttpMethods.Post"/>.</remarks>
        public Task<IActionResult> PublishWithResultAsync<TColl, TItem>(HttpRequest request, TColl value, string? eventName, Func<WebApiParam<TColl>, CancellationToken, Task<Result>>? beforeEvents = null, Action<EventData, TItem>? eventModifier = null, int? maxCollSize = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TColl>? validator = null, CancellationToken cancellationToken = default) where TColl : IEnumerable<TItem>
            => PublishWithResultInternalAsync(request, true, value, eventName, beforeEvents, eventModifier, maxCollSize, statusCode, operationType, validator, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TColl"/> where each item is to be published using the <see cref="EventPublisher"/> (with a <see cref="Result"/>).
        /// </summary>
        private async Task<IActionResult> PublishWithResultInternalAsync<TColl, TItem>(HttpRequest request, bool useValue, TColl value, string? eventName, Func<WebApiParam<TColl>, CancellationToken, Task<Result>>? beforeEvents = null, Action<EventData, TItem>? eventModifier = null, int? maxCollSize = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TColl>? validator = null, CancellationToken cancellationToken = default) where TColl : IEnumerable<TItem>
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Method != HttpMethods.Post)
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Post}' to use {nameof(PublishWithResultAsync)}.", nameof(request));

            return await RunAsync(request, async (wap, ct) =>
            {
                var (wapv, vex) = await ValidateValueAsync(wap, useValue, value, true, validator, cancellationToken).ConfigureAwait(false);
                if (vex != null)
                    return await CreateActionResultFromExceptionAsync(this, request.HttpContext, vex, Settings, Logger, OnUnhandledExceptionAsync, cancellationToken).ConfigureAwait(false);

                if (beforeEvents != null)
                {
                    var result = await beforeEvents(wapv!, ct).ConfigureAwait(false);
                    if (result.IsFailure)
                        return await CreateActionResultFromExceptionAsync(this, request.HttpContext, result.Error, Settings, Logger, OnUnhandledExceptionAsync, cancellationToken).ConfigureAwait(false);
                }

                var max = maxCollSize ?? Settings.MaxPublishCollSize;
                var count = wapv!.Value?.Count() ?? 0;
                if (count > max)
                {
                    Logger.LogWarning("The publish collection contains {EventsCount} items where only a maximum size of {MaxCollSize} is supported; request has been rejected.", count, max);
                    var bex = new BusinessException($"The publish collection contains {count} items where only a maximum size of {max} is supported.");
                    return await CreateActionResultFromExceptionAsync(this, request.HttpContext, bex, Settings, Logger, OnUnhandledExceptionAsync, cancellationToken).ConfigureAwait(false);
                }

                if (count == 0)
                    return new AcceptedResult();

                foreach (var item in wapv.Value!)
                {
                    var ed = new EventData { Value = item };
                    eventModifier?.Invoke(ed, item);
                    if (eventName == null)
                        EventPublisher.Publish(ed);
                    else
                        EventPublisher.PublishNamed(eventName, ed);
                }

                await EventPublisher.SendAsync(ct).ConfigureAwait(false);

                return new ExtendedStatusCodeResult(statusCode);
            }, operationType, cancellationToken, nameof(PublishWithResultAsync)).ConfigureAwait(false);
        }

        #endregion
    }
}