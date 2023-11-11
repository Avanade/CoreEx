// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using CoreEx.Events;
using CoreEx.Json;
using CoreEx.Mapping;
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
    /// Provides the core <see cref="IEventPublisher"/> Web API execution encapsulation.
    /// </summary>
    /// <remarks>Support to change/map request into a different published event type is also enabled where required (see also <seealso cref="Mapper"/>).</remarks>
    public class WebApiPublisher : WebApiBase
    {
        private IMapper? _mapper;

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

        /// <summary>
        /// Gets or sets the <see cref="IMapper"/>.
        /// </summary>
        /// <remarks>Where not explicity set will attempt to use the <see cref="ExecutionContext.GetRequiredService{T}()"/> on first use; will throw an exception where not configured.
        /// <para>This is required where one of the underlying publishing methods is invoked that enables mapping between request and event types and the corresponding <c>beforeEvent</c> parameter is <c>null</c>; 
        /// i.e. the default behaviour is to perform a <see cref="IMapper.Map{TDestination}(object?, OperationTypes)"/> to enable.</para></remarks>
        public IMapper Mapper
        {
            get => _mapper ??= ExecutionContext.GetRequiredService<IMapper>();
            set => _mapper = value;
        }

        #region PublishAsync

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> that is to be published using the <see cref="EventPublisher"/>.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="eventName">The optional event destintion name (e.g. Queue or Topic name).</param>
        /// <param name="beforeEvent">A function that enables the value to be processed/validated before the underlying event publishing logic is enacted (invoked after the <paramref name="validator"/>).</param>
        /// <param name="eventModifier">An action to enable the <see cref="EventData"/> instance to be modified prior to publish.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> of <see cref="HttpMethods.Post"/>.</remarks>
        public Task<IActionResult> PublishAsync<TValue>(HttpRequest request, string? eventName, Func<WebApiParam<TValue>, CancellationToken, Task>? beforeEvent = null, Action<EventData>? eventModifier = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TValue>? validator = null, CancellationToken cancellationToken = default)
            => PublishAsync(request, eventName, (wapv, ct) => PublishBeforeEventAsync<TValue, TValue>(wapv, beforeEvent, ct), eventModifier, statusCode, operationType, validator, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> that is to be published using the <see cref="EventPublisher"/>.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The The value (already deserialized).</param>
        /// <param name="eventName">The optional event destintion name (e.g. Queue or Topic name).</param>
        /// <param name="beforeEvent">A function that enables the value to be processed/validated before the underlying event publishing logic is enacted (invoked after the <paramref name="validator"/>).</param>
        /// <param name="eventModifier">An action to enable the <see cref="EventData"/> instance to be modified prior to publish.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> of <see cref="HttpMethods.Post"/>.</remarks>
        public Task<IActionResult> PublishAsync<TValue>(HttpRequest request, TValue value, string? eventName = null, Func<WebApiParam<TValue>, CancellationToken, Task>? beforeEvent = null, Action<EventData>? eventModifier = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TValue>? validator = null, CancellationToken cancellationToken = default)
            => PublishAsync(request, value, eventName, (wapv, ct) => PublishBeforeEventAsync<TValue, TValue>(wapv, beforeEvent, ct), eventModifier, statusCode, operationType, validator, cancellationToken);

        /// <summary>
        /// Simulates a before event execution; where it in turn maps to itself.
        /// </summary>
        private static async Task<TEventValue> PublishBeforeEventAsync<TValue, TEventValue>(WebApiParam<TValue> wapv, Func<WebApiParam<TValue>, CancellationToken, Task>? beforeEvent, CancellationToken cancellationToken)
        {
            if (beforeEvent is not null)
                await beforeEvent(wapv, cancellationToken).ConfigureAwait(false);

            return wapv.Value is TEventValue tev ? tev : throw new InvalidCastException("The TValue and TEventValue must be the same Type.");
        }

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> that is to be published using the <see cref="EventPublisher"/>.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TEventValue">The <see cref="EventData.Value"/> <see cref="Type"/> (where different to the request).</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="beforeEvent">A function that enables the value to be processed/validated before the underlying event publishing logic is enacted (invoked after the <paramref name="validator"/>).</param>
        /// <param name="eventModifier">An action to enable the <see cref="EventData"/> instance to be modified prior to publish.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> of <see cref="HttpMethods.Post"/>.
        /// <para>Where the <paramref name="beforeEvent"/> is <c>null</c> then the <see cref="Mapper"/> will be used to map between the <typeparamref name="TValue"/> and <typeparamref name="TEventValue"/> types.</para></remarks>
        public Task<IActionResult> PublishAsync<TValue, TEventValue>(HttpRequest request, Func<WebApiParam<TValue>, CancellationToken, Task<TEventValue>>? beforeEvent = null, Action<EventData>? eventModifier = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TValue>? validator = null, CancellationToken cancellationToken = default)
            => PublishAsync(request, null, beforeEvent, eventModifier, statusCode, operationType, validator, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> that is to be published using the <see cref="EventPublisher"/>.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TEventValue">The <see cref="EventData.Value"/> <see cref="Type"/> (where different to the request).</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="eventName">The optional event destintion name (e.g. Queue or Topic name).</param>
        /// <param name="beforeEvent">A function that enables the value to be processed/validated before the underlying event publishing logic is enacted (invoked after the <paramref name="validator"/>).</param>
        /// <param name="eventModifier">An action to enable the <see cref="EventData"/> instance to be modified prior to publish.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> of <see cref="HttpMethods.Post"/>.
        /// <para>Where the <paramref name="beforeEvent"/> is <c>null</c> then the <see cref="Mapper"/> will be used to map between the <typeparamref name="TValue"/> and <typeparamref name="TEventValue"/> types.</para></remarks>
        public Task<IActionResult> PublishAsync<TValue, TEventValue>(HttpRequest request, string? eventName, Func<WebApiParam<TValue>, CancellationToken, Task<TEventValue>>? beforeEvent = null, Action<EventData>? eventModifier = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TValue>? validator = null, CancellationToken cancellationToken = default)
            => PublishInternalAsync(request, false, default!, eventName, beforeEvent, eventModifier, statusCode, operationType, validator, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> that is to be published using the <see cref="EventPublisher"/>.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TEventValue">The <see cref="EventData.Value"/> <see cref="Type"/> (where different to the request).</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The The value (already deserialized).</param>
        /// <param name="beforeEvent">A function that enables the value to be processed/validated before the underlying event publishing logic is enacted (invoked after the <paramref name="validator"/>).</param>
        /// <param name="eventModifier">An action to enable the <see cref="EventData"/> instance to be modified prior to publish.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> of <see cref="HttpMethods.Post"/>.
        /// <para>Where the <paramref name="beforeEvent"/> is <c>null</c> then the <see cref="Mapper"/> will be used to map between the <typeparamref name="TValue"/> and <typeparamref name="TEventValue"/> types.</para></remarks>
        public Task<IActionResult> PublishAsync<TValue, TEventValue>(HttpRequest request, TValue value, Func<WebApiParam<TValue>, CancellationToken, Task<TEventValue>>? beforeEvent = null, Action<EventData>? eventModifier = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TValue>? validator = null, CancellationToken cancellationToken = default)
            => PublishAsync(request, value, null, beforeEvent, eventModifier, statusCode, operationType, validator, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> that is to be published using the <see cref="EventPublisher"/>.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TEventValue">The <see cref="EventData.Value"/> <see cref="Type"/> (where different to the request).</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The The value (already deserialized).</param>
        /// <param name="eventName">The optional event destintion name (e.g. Queue or Topic name).</param>
        /// <param name="beforeEvent">A function that enables the value to be processed/validated before the underlying event publishing logic is enacted (invoked after the <paramref name="validator"/>).</param>
        /// <param name="eventModifier">An action to enable the <see cref="EventData"/> instance to be modified prior to publish.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> of <see cref="HttpMethods.Post"/>.
        /// <para>Where the <paramref name="beforeEvent"/> is <c>null</c> then the <see cref="Mapper"/> will be used to map between the <typeparamref name="TValue"/> and <typeparamref name="TEventValue"/> types.</para></remarks>
        public Task<IActionResult> PublishAsync<TValue, TEventValue>(HttpRequest request, TValue value, string? eventName, Func<WebApiParam<TValue>, CancellationToken, Task<TEventValue>>? beforeEvent = null, Action<EventData>? eventModifier = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TValue>? validator = null, CancellationToken cancellationToken = default)
            => PublishInternalAsync(request, true, value, eventName, beforeEvent, eventModifier, statusCode, operationType, validator, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> that is to be published using the <see cref="EventPublisher"/>.
        /// </summary>
        private async Task<IActionResult> PublishInternalAsync<TValue, TEventValue>(HttpRequest request, bool useValue, TValue value, string? eventName, Func<WebApiParam<TValue>, CancellationToken, Task<TEventValue>>? beforeEvent, Action<EventData>? eventModifier = null,
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

                var @event = new EventData
                {
                    Value = beforeEvent is null ? Mapper.Map<TValue, TEventValue>(wapv!.Value) : await beforeEvent(wapv!, ct).ConfigureAwait(false)
                };

                eventModifier?.Invoke(@event);

                if (eventName == null)
                    EventPublisher.Publish(@event);
                else
                    EventPublisher.PublishNamed(eventName, @event);

                await EventPublisher.SendAsync(cancellationToken).ConfigureAwait(false);

                return new ExtendedStatusCodeResult(statusCode);
            }, operationType, cancellationToken, nameof(PublishAsync)).ConfigureAwait(false);
        }

        #endregion

        #region PublishCollectionAsync

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TColl"/> where each item is to be published using the <see cref="EventPublisher"/>.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/></typeparam>
        /// <typeparam name="TItem">The collection item <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="eventName">The optional event destintion name (e.g. Queue or Topic name).</param>
        /// <param name="beforeEvents">A function that enables the collection to be processed/validated before the underlying event publishing logic is enacted (invoked after the <paramref name="validator"/>).</param>
        /// <param name="eventModifier">An action to enable each item <see cref="EventData"/> instance to be modified prior to publish.</param>
        /// <param name="maxCollSize">Overrides the default (see <see cref="SettingsBase.MaxPublishCollSize"/>) maximum publish collection size.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> of <see cref="HttpMethods.Post"/>.</remarks>
        public Task<IActionResult> PublishCollectionAsync<TColl, TItem>(HttpRequest request, string? eventName = null, Func<WebApiParam<TColl>, CancellationToken, Task>? beforeEvents = null, Action<EventData>? eventModifier = null, int? maxCollSize = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TColl>? validator = null, CancellationToken cancellationToken = default) where TColl : IEnumerable<TItem>
            => PublishCollectionAsync<TColl, TItem, TItem>(request, eventName, (wapc, ct) => PublishCollectionBeforeEventAsync<TColl, TItem, TItem>(wapc, beforeEvents, ct), eventModifier, maxCollSize, statusCode, operationType, validator, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TColl"/> where each item is to be published using the <see cref="EventPublisher"/>.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/></typeparam>
        /// <typeparam name="TItem">The collection item <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The The value (already deserialized).</param>
        /// <param name="eventName">The optional event destintion name (e.g. Queue or Topic name).</param>
        /// <param name="beforeEvents">A function that enables the collection to be processed/validated before the underlying event publishing logic is enacted (invoked after the <paramref name="validator"/>).</param>
        /// <param name="eventModifier">An action to enable each item <see cref="EventData"/> instance to be modified prior to publish.</param>
        /// <param name="maxCollSize">Overrides the default (see <see cref="SettingsBase.MaxPublishCollSize"/>) maximum publish collection size.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> of <see cref="HttpMethods.Post"/>.</remarks>
        public Task<IActionResult> PublishCollectionAsync<TColl, TItem>(HttpRequest request, TColl value, string? eventName = null, Func<WebApiParam<TColl>, CancellationToken, Task>? beforeEvents = null, Action<EventData>? eventModifier = null, int? maxCollSize = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TColl>? validator = null, CancellationToken cancellationToken = default) where TColl : IEnumerable<TItem>
            => PublishCollectionAsync<TColl, TItem, TItem>(request, value, eventName, (wapc, ct) => PublishCollectionBeforeEventAsync<TColl, TItem, TItem>(wapc, beforeEvents, ct), eventModifier, maxCollSize, statusCode, operationType, validator, cancellationToken);

        /// <summary>
        /// Simulates a before event execution; where it in turn maps to itself.
        /// </summary>
        private static async Task<IEnumerable<TEventItem>> PublishCollectionBeforeEventAsync<TColl, TItem, TEventItem>(WebApiParam<TColl> wapc, Func<WebApiParam<TColl>, CancellationToken, Task>? beforeEvent, CancellationToken cancellationToken) where TColl : IEnumerable<TItem>
        {
            if (beforeEvent is not null)
                await beforeEvent(wapc, cancellationToken).ConfigureAwait(false);

            var items = new List<TEventItem>();
            foreach (var item in wapc.Value!)
            {
                if (item is TEventItem tei)
                    items.Add(tei);
                else
                    throw new InvalidCastException("The TItem and TEventItem must be the same Type.");
            }

            return items;
        }

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TColl"/> where each item is to be published using the <see cref="EventPublisher"/>.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/></typeparam>
        /// <typeparam name="TItem">The collection item <see cref="Type"/>.</typeparam>
        /// <typeparam name="TEventItem">The <see cref="EventData.Value"/> item <see cref="Type"/> (where different to the request).</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="beforeEvents">A function that enables the collection to be processed/validated before the underlying event publishing logic is enacted (invoked after the <paramref name="validator"/>).</param>
        /// <param name="eventModifier">An action to enable each item <see cref="EventData"/> instance to be modified prior to publish.</param>
        /// <param name="maxCollSize">Overrides the default (see <see cref="SettingsBase.MaxPublishCollSize"/>) maximum publish collection size.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> of <see cref="HttpMethods.Post"/>.
        /// <para>Where the <paramref name="beforeEvents"/> is <c>null</c> then the <see cref="Mapper"/> will be used to map between the <typeparamref name="TItem"/> and <typeparamref name="TEventItem"/> types.</para></remarks>
        public Task<IActionResult> PublishCollectionAsync<TColl, TItem, TEventItem>(HttpRequest request, Func<WebApiParam<TColl>, CancellationToken, Task<IEnumerable<TEventItem>>>? beforeEvents = null, Action<EventData>? eventModifier = null, int? maxCollSize = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TColl>? validator = null, CancellationToken cancellationToken = default) where TColl : IEnumerable<TItem>
            => PublishCollectionAsync<TColl, TItem, TEventItem>(request, null, beforeEvents, eventModifier, maxCollSize, statusCode, operationType, validator, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TColl"/> where each item is to be published using the <see cref="EventPublisher"/>.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/></typeparam>
        /// <typeparam name="TItem">The collection item <see cref="Type"/>.</typeparam>
        /// <typeparam name="TEventItem">The <see cref="EventData.Value"/> item <see cref="Type"/> (where different to the request).</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="eventName">The optional event destintion name (e.g. Queue or Topic name).</param>
        /// <param name="beforeEvents">A function that enables the collection to be processed/validated before the underlying event publishing logic is enacted (invoked after the <paramref name="validator"/>).</param>
        /// <param name="eventModifier">An action to enable each item <see cref="EventData"/> instance to be modified prior to publish.</param>
        /// <param name="maxCollSize">Overrides the default (see <see cref="SettingsBase.MaxPublishCollSize"/>) maximum publish collection size.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> of <see cref="HttpMethods.Post"/>.
        /// <para>Where the <paramref name="beforeEvents"/> is <c>null</c> then the <see cref="Mapper"/> will be used to map between the <typeparamref name="TItem"/> and <typeparamref name="TEventItem"/> types.</para></remarks>
        public Task<IActionResult> PublishCollectionAsync<TColl, TItem, TEventItem>(HttpRequest request, string? eventName, Func<WebApiParam<TColl>, CancellationToken, Task<IEnumerable<TEventItem>>>? beforeEvents = null, Action<EventData>? eventModifier = null, int? maxCollSize = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TColl>? validator = null, CancellationToken cancellationToken = default) where TColl : IEnumerable<TItem>
            => PublishCollectionInternalAsync<TColl, TItem, TEventItem>(request, false, default!, eventName, beforeEvents, eventModifier, maxCollSize, statusCode, operationType, validator, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TColl"/> where each item is to be published using the <see cref="EventPublisher"/>.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/></typeparam>
        /// <typeparam name="TItem">The collection item <see cref="Type"/>.</typeparam>
        /// <typeparam name="TEventItem">The <see cref="EventData.Value"/> item <see cref="Type"/> (where different to the request).</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The The value (already deserialized).</param>
        /// <param name="beforeEvents">A function that enables the collection to be processed/validated before the underlying event publishing logic is enacted (invoked after the <paramref name="validator"/>).</param>
        /// <param name="eventModifier">An action to enable each item <see cref="EventData"/> instance to be modified prior to publish.</param>
        /// <param name="maxCollSize">Overrides the default (see <see cref="SettingsBase.MaxPublishCollSize"/>) maximum publish collection size.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> of <see cref="HttpMethods.Post"/>.
        /// <para>Where the <paramref name="beforeEvents"/> is <c>null</c> then the <see cref="Mapper"/> will be used to map between the <typeparamref name="TItem"/> and <typeparamref name="TEventItem"/> types.</para></remarks>
        public Task<IActionResult> PublishCollectionAsync<TColl, TItem, TEventItem>(HttpRequest request, TColl value, Func<WebApiParam<TColl>, CancellationToken, Task<IEnumerable<TEventItem>>>? beforeEvents = null, Action<EventData>? eventModifier = null, int? maxCollSize = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TColl>? validator = null, CancellationToken cancellationToken = default) where TColl : IEnumerable<TItem>
            => PublishCollectionAsync<TColl, TItem, TEventItem>(request, value, null, beforeEvents, eventModifier, maxCollSize, statusCode, operationType, validator, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TColl"/> where each item is to be published using the <see cref="EventPublisher"/>.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/></typeparam>
        /// <typeparam name="TItem">The collection item <see cref="Type"/>.</typeparam>
        /// <typeparam name="TEventItem">The <see cref="EventData.Value"/> item <see cref="Type"/> (where different to the request).</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The The value (already deserialized).</param>
        /// <param name="eventName">The optional event destintion name (e.g. Queue or Topic name).</param>
        /// <param name="beforeEvents">A function that enables the collection to be processed/validated before the underlying event publishing logic is enacted (invoked after the <paramref name="validator"/>).</param>
        /// <param name="eventModifier">An action to enable each item <see cref="EventData"/> instance to be modified prior to publish.</param>
        /// <param name="maxCollSize">Overrides the default (see <see cref="SettingsBase.MaxPublishCollSize"/>) maximum publish collection size.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> of <see cref="HttpMethods.Post"/>.
        /// <para>Where the <paramref name="beforeEvents"/> is <c>null</c> then the <see cref="Mapper"/> will be used to map between the <typeparamref name="TItem"/> and <typeparamref name="TEventItem"/> types.</para></remarks>
        public Task<IActionResult> PublishCollectionAsync<TColl, TItem, TEventItem>(HttpRequest request, TColl value, string? eventName, Func<WebApiParam<TColl>, CancellationToken, Task<IEnumerable<TEventItem>>>? beforeEvents = null, Action<EventData>? eventModifier = null, int? maxCollSize = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TColl>? validator = null, CancellationToken cancellationToken = default) where TColl : IEnumerable<TItem>
            => PublishCollectionInternalAsync<TColl, TItem, TEventItem>(request, true, value, eventName, beforeEvents, eventModifier, maxCollSize, statusCode, operationType, validator, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TColl"/> where each item is to be published using the <see cref="EventPublisher"/>.
        /// </summary>
        private async Task<IActionResult> PublishCollectionInternalAsync<TColl, TItem, TEventItem>(HttpRequest request, bool useValue, TColl value, string? eventName, Func<WebApiParam<TColl>, CancellationToken, Task<IEnumerable<TEventItem>>>? beforeEvents, Action<EventData>? eventModifier = null, int? maxCollSize = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TColl>? validator = null, CancellationToken cancellationToken = default) where TColl : IEnumerable<TItem>
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Method != HttpMethods.Post)
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Post}' to use {nameof(PublishAsync)}.", nameof(request));

            // Fall back to a mapper where no explicit beforeEvents is specified.
            return await RunAsync(request, async (wap, ct) =>
            {
                var (wapc, vex) = await ValidateValueAsync(wap, useValue, value, true, validator, cancellationToken).ConfigureAwait(false);
                if (vex != null)
                    return await CreateActionResultFromExceptionAsync(this, request.HttpContext, vex, Settings, Logger, OnUnhandledExceptionAsync, cancellationToken).ConfigureAwait(false);

                var max = maxCollSize ?? Settings.MaxPublishCollSize;
                var count = wapc!.Value?.Count() ?? 0;
                if (count > max)
                {
                    Logger.LogWarning("The publish collection contains {EventsCount} items where only a maximum size of {MaxCollSize} is supported; request has been rejected.", count, max);
                    var bex = new BusinessException($"The publish collection contains {count} items where only a maximum size of {max} is supported.");
                    return await CreateActionResultFromExceptionAsync(this, request.HttpContext, bex, Settings, Logger, OnUnhandledExceptionAsync, cancellationToken).ConfigureAwait(false);
                }

                if (count == 0)
                    return new AcceptedResult();

                IEnumerable<TEventItem> items;
                if (beforeEvents is null)
                {
                    var coll = new List<TEventItem>();
                    {
                        foreach (var item in wapc.Value!)
                        {
                            coll.Add(Mapper.Map<TItem, TEventItem>(item)!);
                        }
                    }

                    items = coll.AsEnumerable();
                }
                else
                    items = await beforeEvents(wapc!, ct).ConfigureAwait(false);

                foreach (var item in items)
                {
                    var @event = new EventData { Value = item };

                    eventModifier?.Invoke(@event);

                    if (eventName == null)
                        EventPublisher.Publish(@event);
                    else
                        EventPublisher.PublishNamed(eventName, @event);
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
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> of <see cref="HttpMethods.Post"/>.</remarks>
        public Task<IActionResult> PublishWithResultAsync<TValue>(HttpRequest request, string? eventName = null, Func<WebApiParam<TValue>, CancellationToken, Task<Result>>? beforeEvent = null, Action<EventData>? eventModifier = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TValue>? validator = null, CancellationToken cancellationToken = default)
            => PublishWithResultAsync(request, eventName, (wapv, ct) => PublishBeforeEventWithResultAsync<TValue, TValue>(wapv, beforeEvent, ct), eventModifier, statusCode, operationType, validator, cancellationToken);

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
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> of <see cref="HttpMethods.Post"/>.</remarks>
        public Task<IActionResult> PublishWithResultAsync<TValue>(HttpRequest request, TValue value, string? eventName = null, Func<WebApiParam<TValue>, CancellationToken, Task<Result>>? beforeEvent = null, Action<EventData>? eventModifier = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TValue>? validator = null, CancellationToken cancellationToken = default)
            => PublishWithResultAsync(request, value, eventName, (wapv, ct) => PublishBeforeEventWithResultAsync<TValue, TValue>(wapv, beforeEvent, ct), eventModifier, statusCode, operationType, validator, cancellationToken);

        /// <summary>
        /// Simulates a before event execution; where it in turn maps to itself.
        /// </summary>
        private static async Task<Result<TEventValue>> PublishBeforeEventWithResultAsync<TValue, TEventValue>(WebApiParam<TValue> wapv, Func<WebApiParam<TValue>, CancellationToken, Task<Result>>? beforeEvent, CancellationToken cancellationToken)
        {
            var result = beforeEvent is null ? Result.Success : await beforeEvent(wapv, cancellationToken).ConfigureAwait(false);
            return result.ThenAs(() => wapv.Value is TEventValue tev ? tev : throw new InvalidCastException("The TValue and TEventValue must be the same Type."));
        }

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> that is to be published using the <see cref="EventPublisher"/> (with a <see cref="Result"/>).
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TEventValue">The <see cref="EventData.Value"/> <see cref="Type"/> (where different to the request).</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="beforeEvent">A function that enables the value to be processed/validated before the underlying event publishing logic is enacted.</param>
        /// <param name="eventModifier">An action to enable the <see cref="EventData"/> instance to be modified prior to publish.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> of <see cref="HttpMethods.Post"/>.</remarks>
        public Task<IActionResult> PublishWithResultAsync<TValue, TEventValue>(HttpRequest request, Func<WebApiParam<TValue>, CancellationToken, Task<Result<TEventValue>>> beforeEvent, Action<EventData>? eventModifier = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TValue>? validator = null, CancellationToken cancellationToken = default)
            => PublishWithResultAsync(request, null, beforeEvent, eventModifier, statusCode, operationType, validator, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> that is to be published using the <see cref="EventPublisher"/> (with a <see cref="Result"/>).
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TEventValue">The <see cref="EventData.Value"/> <see cref="Type"/> (where different to the request).</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="eventName">The optional event destintion name (e.g. Queue or Topic name).</param>
        /// <param name="beforeEvent">A function that enables the value to be processed/validated before the underlying event publishing logic is enacted.</param>
        /// <param name="eventModifier">An action to enable the <see cref="EventData"/> instance to be modified prior to publish.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> of <see cref="HttpMethods.Post"/>.</remarks>
        public Task<IActionResult> PublishWithResultAsync<TValue, TEventValue>(HttpRequest request, string? eventName, Func<WebApiParam<TValue>, CancellationToken, Task<Result<TEventValue>>> beforeEvent, Action<EventData>? eventModifier = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TValue>? validator = null, CancellationToken cancellationToken = default)
            => PublishWithResultInternalAsync(request, false, default!, eventName, beforeEvent, eventModifier, statusCode, operationType, validator, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> that is to be published using the <see cref="EventPublisher"/> (with a <see cref="Result"/>).
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TEventValue">The <see cref="EventData.Value"/> <see cref="Type"/> (where different to the request).</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The The value (already deserialized).</param>
        /// <param name="beforeEvent">A function that enables the value to be processed/validated before the underlying event publishing logic is enacted.</param>
        /// <param name="eventModifier">An action to enable the <see cref="EventData"/> instance to be modified prior to publish.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> of <see cref="HttpMethods.Post"/>.</remarks>
        public Task<IActionResult> PublishWithResultAsync<TValue, TEventValue>(HttpRequest request, TValue value, Func<WebApiParam<TValue>, CancellationToken, Task<Result<TEventValue>>> beforeEvent, Action<EventData>? eventModifier = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TValue>? validator = null, CancellationToken cancellationToken = default)
            => PublishWithResultAsync(request, value, null, beforeEvent, eventModifier, statusCode, operationType, validator, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> that is to be published using the <see cref="EventPublisher"/> (with a <see cref="Result"/>).
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TEventValue">The <see cref="EventData.Value"/> <see cref="Type"/> (where different to the request).</typeparam>
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
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> of <see cref="HttpMethods.Post"/>.</remarks>
        public Task<IActionResult> PublishWithResultAsync<TValue, TEventValue>(HttpRequest request, TValue value, string? eventName, Func<WebApiParam<TValue>, CancellationToken, Task<Result<TEventValue>>> beforeEvent, Action<EventData>? eventModifier = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TValue>? validator = null, CancellationToken cancellationToken = default)
            => PublishWithResultInternalAsync(request, true, value, eventName, beforeEvent, eventModifier, statusCode, operationType, validator, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> that is to be published using the <see cref="EventPublisher"/> (with a <see cref="Result"/>).
        /// </summary>
        private async Task<IActionResult> PublishWithResultInternalAsync<TValue, TEventValue>(HttpRequest request, bool useValue, TValue value, string? eventName, Func<WebApiParam<TValue>, CancellationToken, Task<Result<TEventValue>>>? beforeEvent = null, Action<EventData>? eventModifier = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TValue>? validator = null, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (beforeEvent is null)
                throw new ArgumentNullException(nameof(beforeEvent));

            if (request.Method != HttpMethods.Post)
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Post}' to use {nameof(PublishWithResultAsync)}.", nameof(request));

            return await RunAsync(request, async (wap, ct) =>
            {
                var (wapv, vex) = await ValidateValueAsync(wap, useValue, value, true, validator, cancellationToken).ConfigureAwait(false);
                if (vex != null)
                    return await CreateActionResultFromExceptionAsync(this, request.HttpContext, vex, Settings, Logger, OnUnhandledExceptionAsync, cancellationToken).ConfigureAwait(false);

                var result = await beforeEvent(wapv!, ct).ConfigureAwait(false);
                if (result.IsFailure)
                    return await CreateActionResultFromExceptionAsync(this, request.HttpContext, result.Error, Settings, Logger, OnUnhandledExceptionAsync, cancellationToken).ConfigureAwait(false);

                var @event = new EventData { Value = result.Value };
                eventModifier?.Invoke(@event);

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
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> of <see cref="HttpMethods.Post"/>.</remarks>
        public Task<IActionResult> PublishCollectionWithResultAsync<TColl, TItem>(HttpRequest request, string? eventName = null, Func<WebApiParam<TColl>, CancellationToken, Task<Result>>? beforeEvents = null, Action<EventData>? eventModifier = null, int? maxCollSize = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TColl>? validator = null, CancellationToken cancellationToken = default) where TColl : IEnumerable<TItem>
            => PublishCollectionWithResultAsync<TColl, TItem, TItem>(request, eventName, (wapc, ct) => PublishCollectionBeforeEventWithResultAsync<TColl, TItem, TItem>(wapc, beforeEvents, ct), eventModifier, maxCollSize, statusCode, operationType, validator, cancellationToken);

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
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> of <see cref="HttpMethods.Post"/>.</remarks>
        public Task<IActionResult> PublishCollectionWithResultAsync<TColl, TItem>(HttpRequest request, TColl value, string? eventName = null, Func<WebApiParam<TColl>, CancellationToken, Task<Result>>? beforeEvents = null, Action<EventData>? eventModifier = null, int? maxCollSize = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TColl>? validator = null, CancellationToken cancellationToken = default) where TColl : IEnumerable<TItem>
            => PublishCollectionWithResultAsync<TColl, TItem, TItem>(request, value, eventName, (wapc, ct) => PublishCollectionBeforeEventWithResultAsync<TColl, TItem, TItem>(wapc, beforeEvents, ct), eventModifier, maxCollSize, statusCode, operationType, validator, cancellationToken);

        /// <summary>
        /// Simulates a before event execution; where it in turn maps to itself.
        /// </summary>
        private static async Task<Result<IEnumerable<TEventItem>>> PublishCollectionBeforeEventWithResultAsync<TColl, TItem, TEventItem>(WebApiParam<TColl> wapc, Func<WebApiParam<TColl>, CancellationToken, Task<Result>>? beforeEvent, CancellationToken cancellationToken) where TColl : IEnumerable<TItem>
        {
            var result = beforeEvent is null ? Result.Success : await beforeEvent(wapc, cancellationToken).ConfigureAwait(false);
            return result.ThenAs(() =>
            {
                var items = new List<TEventItem>();
                foreach (var item in wapc.Value!)
                {
                    if (item is TEventItem tei)
                        items.Add(tei);
                    else
                        throw new InvalidCastException("The TItem and TEventItem must be the same Type.");
                }

                return items.AsEnumerable();
            });
        }

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TColl"/> where each item is to be published using the <see cref="EventPublisher"/> (with a <see cref="Result"/>).
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/></typeparam>
        /// <typeparam name="TItem">The collection item <see cref="Type"/>.</typeparam>
        /// <typeparam name="TEventItem">The <see cref="EventData.Value"/> item <see cref="Type"/> (where different to the request).</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="beforeEvents">A function that enables the collection to be processed/validated before the underlying event publishing logic is enacted.</param>
        /// <param name="eventModifier">An action to enable each item <see cref="EventData"/> instance to be modified prior to publish.</param>
        /// <param name="maxCollSize">Overrides the default (see <see cref="SettingsBase.MaxPublishCollSize"/>) maximum publish collection size.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> of <see cref="HttpMethods.Post"/>.</remarks>
        public Task<IActionResult> PublishCollectionWithResultAsync<TColl, TItem, TEventItem>(HttpRequest request, Func<WebApiParam<TColl>, CancellationToken, Task<Result<IEnumerable<TEventItem>>>> beforeEvents, Action<EventData>? eventModifier = null, int? maxCollSize = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TColl>? validator = null, CancellationToken cancellationToken = default) where TColl : IEnumerable<TItem>
            => PublishCollectionWithResultAsync<TColl, TItem, TEventItem>(request, null, beforeEvents, eventModifier, maxCollSize, statusCode, operationType, validator, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TColl"/> where each item is to be published using the <see cref="EventPublisher"/> (with a <see cref="Result"/>).
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/></typeparam>
        /// <typeparam name="TItem">The collection item <see cref="Type"/>.</typeparam>
        /// <typeparam name="TEventItem">The <see cref="EventData.Value"/> item <see cref="Type"/> (where different to the request).</typeparam>
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
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> of <see cref="HttpMethods.Post"/>.</remarks>
        public Task<IActionResult> PublishCollectionWithResultAsync<TColl, TItem, TEventItem>(HttpRequest request, string? eventName, Func<WebApiParam<TColl>, CancellationToken, Task<Result<IEnumerable<TEventItem>>>> beforeEvents, Action<EventData>? eventModifier = null, int? maxCollSize = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TColl>? validator = null, CancellationToken cancellationToken = default) where TColl : IEnumerable<TItem>
            => PublishCollectionWithResultInternalAsync<TColl, TItem, TEventItem>(request, false, default!, eventName, beforeEvents, eventModifier, maxCollSize, statusCode, operationType, validator, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TColl"/> where each item is to be published using the <see cref="EventPublisher"/> (with a <see cref="Result"/>).
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/></typeparam>
        /// <typeparam name="TItem">The collection item <see cref="Type"/>.</typeparam>
        /// <typeparam name="TEventItem">The <see cref="EventData.Value"/> item <see cref="Type"/> (where different to the request).</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The The value (already deserialized).</param>
        /// <param name="beforeEvents">A function that enables the collection to be processed/validated before the underlying event publishing logic is enacted.</param>
        /// <param name="eventModifier">An action to enable each item <see cref="EventData"/> instance to be modified prior to publish.</param>
        /// <param name="maxCollSize">Overrides the default (see <see cref="SettingsBase.MaxPublishCollSize"/>) maximum publish collection size.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> of <see cref="HttpMethods.Post"/>.</remarks>
        public Task<IActionResult> PublishCollectionWithResultAsync<TColl, TItem, TEventItem>(HttpRequest request, TColl value, Func<WebApiParam<TColl>, CancellationToken, Task<Result<IEnumerable<TEventItem>>>> beforeEvents, Action<EventData>? eventModifier = null, int? maxCollSize = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TColl>? validator = null, CancellationToken cancellationToken = default) where TColl : IEnumerable<TItem>
            => PublishCollectionWithResultAsync<TColl, TItem, TEventItem>(request, value, null, beforeEvents, eventModifier, maxCollSize, statusCode, operationType, validator, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TColl"/> where each item is to be published using the <see cref="EventPublisher"/> (with a <see cref="Result"/>).
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/></typeparam>
        /// <typeparam name="TItem">The collection item <see cref="Type"/>.</typeparam>
        /// <typeparam name="TEventItem">The <see cref="EventData.Value"/> item <see cref="Type"/> (where different to the request).</typeparam>
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
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> of <see cref="HttpMethods.Post"/>.</remarks>
        public Task<IActionResult> PublishCollectionWithResultAsync<TColl, TItem, TEventItem>(HttpRequest request, TColl value, string? eventName, Func<WebApiParam<TColl>, CancellationToken, Task<Result<IEnumerable<TEventItem>>>> beforeEvents, Action<EventData>? eventModifier = null, int? maxCollSize = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TColl>? validator = null, CancellationToken cancellationToken = default) where TColl : IEnumerable<TItem>
            => PublishCollectionWithResultInternalAsync<TColl, TItem, TEventItem>(request, true, value, eventName, beforeEvents, eventModifier, maxCollSize, statusCode, operationType, validator, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TColl"/> where each item is to be published using the <see cref="EventPublisher"/> (with a <see cref="Result"/>).
        /// </summary>
        private async Task<IActionResult> PublishCollectionWithResultInternalAsync<TColl, TItem, TEventItem>(HttpRequest request, bool useValue, TColl value, string? eventName, Func<WebApiParam<TColl>, CancellationToken, Task<Result<IEnumerable<TEventItem>>>>? beforeEvents = null, Action<EventData>? eventModifier = null, int? maxCollSize = null,
            HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified, IValidator<TColl>? validator = null, CancellationToken cancellationToken = default) where TColl : IEnumerable<TItem>
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (beforeEvents is null)
                throw new ArgumentNullException(nameof(beforeEvents));

            if (request.Method != HttpMethods.Post)
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Post}' to use {nameof(PublishWithResultAsync)}.", nameof(request));

            return await RunAsync(request, async (wap, ct) =>
            {
                var (wapv, vex) = await ValidateValueAsync(wap, useValue, value, true, validator, cancellationToken).ConfigureAwait(false);
                if (vex != null)
                    return await CreateActionResultFromExceptionAsync(this, request.HttpContext, vex, Settings, Logger, OnUnhandledExceptionAsync, cancellationToken).ConfigureAwait(false);

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

                var result = await beforeEvents(wapv!, ct).ConfigureAwait(false);
                if (result.IsFailure)
                    return await CreateActionResultFromExceptionAsync(this, request.HttpContext, result.Error, Settings, Logger, OnUnhandledExceptionAsync, cancellationToken).ConfigureAwait(false);

                foreach (var item in result.Value)
                {
                    var @event = new EventData { Value = item };
                    eventModifier?.Invoke(@event);

                    if (eventName == null)
                        EventPublisher.Publish(@event);
                    else
                        EventPublisher.PublishNamed(eventName, @event);
                }

                await EventPublisher.SendAsync(ct).ConfigureAwait(false);

                return new ExtendedStatusCodeResult(statusCode);
            }, operationType, cancellationToken, nameof(PublishWithResultAsync)).ConfigureAwait(false);
        }

        #endregion
    }
}