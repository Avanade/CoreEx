// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.AspNetCore.Http;
using CoreEx.Configuration;
using CoreEx.Events;
using CoreEx.Hosting.Work;
using CoreEx.Json;
using CoreEx.Mapping;
using CoreEx.Results;
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
    /// <remarks>Support to change/map request into a different published event type is also enabled where required (see also <seealso cref="Mapper"/>).
    /// <para>By adding a <see cref="WorkStateOrchestrator"/> then the beginnings of the <i>asynchronous request-response</i> pattern implementation can be achieved.</para></remarks>
    /// <param name="eventPublisher">The <see cref="IEventPublisher"/>.</param>
    /// <param name="executionContext">The <see cref="ExecutionContext"/>.</param>
    /// <param name="settings">The <see cref="SettingsBase"/>.</param>
    /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
    /// <param name="invoker">The <see cref="WebApiInvoker"/>.</param>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    public class WebApiPublisher(IEventPublisher eventPublisher, ExecutionContext executionContext, SettingsBase settings, IJsonSerializer jsonSerializer, ILogger<WebApiPublisher> logger, WebApiInvoker? invoker = null) : WebApiBase(executionContext, settings, jsonSerializer, logger, invoker)
    {
        private IMapper? _mapper;

        /// <summary>
        /// Gets the <see cref="IEventPublisher"/>.
        /// </summary>
        public IEventPublisher EventPublisher { get; } = eventPublisher.ThrowIfNull(nameof(eventPublisher));

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

        /// <summary>
        /// Gets or sets the <see cref="Hosting.Work.WorkStateOrchestrator"/>.
        /// </summary>
        /// <remarks>See <see cref="IWebApiPublisherArgs{TValue, TEventValue}.CreateWorkStateArgs"/> for corresponding <i>asynchronous request-response</i> pattern implementation.</remarks>
        public WorkStateOrchestrator? WorkStateOrchestrator { get; set; }

        #region PublishAsync

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with no content body that is to be published using the <see cref="EventPublisher"/>.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="args">The <see cref="WebApiPublisherArgs{TValue}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> of <see cref="HttpMethods.Post"/>.</remarks>
        public Task<IActionResult> PublishAsync(HttpRequest request, WebApiPublisherArgs args, CancellationToken cancellationToken = default)
            => PublishOrchestrateAsync(request, false, default!, args.ThrowIfNull(nameof(args)), cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON body content value of <see cref="Type"/> <typeparamref name="TValue"/> that is to be published using the <see cref="EventPublisher"/>.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="args">The <see cref="WebApiPublisherArgs{TValue}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> of <see cref="HttpMethods.Post"/>.</remarks>
        public Task<IActionResult> PublishAsync<TValue>(HttpRequest request, WebApiPublisherArgs<TValue>? args = null, CancellationToken cancellationToken = default)
            => PublishOrchestrateAsync(request, false, default!, args ?? new WebApiPublisherArgs<TValue>(), cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> that is to be published using the <see cref="EventPublisher"/>.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The value (already deserialized).</param>
        /// <param name="args">The <see cref="WebApiPublisherArgs{TValue}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> of <see cref="HttpMethods.Post"/>.</remarks>
        public Task<IActionResult> PublishValueAsync<TValue>(HttpRequest request, TValue value, WebApiPublisherArgs<TValue>? args = null, CancellationToken cancellationToken = default)
            => PublishOrchestrateAsync(request, true, value, args ?? new WebApiPublisherArgs<TValue>(), cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON body content value of <see cref="Type"/> <typeparamref name="TValue"/> that is to be published using the <see cref="EventPublisher"/>.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TEventValue">The <see cref="EventData.Value"/> <see cref="Type"/> (where different to the request).</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="args">The <see cref="WebApiPublisherArgs{TValue}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> of <see cref="HttpMethods.Post"/>.</remarks>
        public Task<IActionResult> PublishAsync<TValue, TEventValue>(HttpRequest request, WebApiPublisherArgs<TValue, TEventValue>? args = null, CancellationToken cancellationToken = default)
            => PublishOrchestrateAsync(request, false, default!, args ?? new WebApiPublisherArgs<TValue, TEventValue>(), cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> that is to be published using the <see cref="EventPublisher"/>.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TEventValue">The <see cref="EventData.Value"/> <see cref="Type"/> (where different to the request).</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The value (already deserialized).</param>
        /// <param name="args">The <see cref="WebApiPublisherArgs{TValue}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> of <see cref="HttpMethods.Post"/>.</remarks>
        public Task<IActionResult> PublishValueAsync<TValue, TEventValue>(HttpRequest request, TValue value, WebApiPublisherArgs<TValue, TEventValue>? args = null, CancellationToken cancellationToken = default)
            => PublishOrchestrateAsync(request, true, value, args ?? new WebApiPublisherArgs<TValue, TEventValue>(), cancellationToken);

        /// <summary>
        /// Performs the publish orchestration.
        /// </summary>
        private async Task<IActionResult> PublishOrchestrateAsync<TValue, TEventValue>(HttpRequest request, bool useValue, TValue value, IWebApiPublisherArgs<TValue, TEventValue> args, CancellationToken cancellationToken = default)
        {
            request.ThrowIfNull();
            args.ThrowIfNull();

            if (request.Method != HttpMethods.Post)
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Post}' to use {nameof(PublishAsync)}.", nameof(request));

            if (args.CreateWorkStateArgs is not null && WorkStateOrchestrator is null)
                throw new InvalidOperationException($"The {nameof(WorkStateOrchestrator)} must be set to use {nameof(IWebApiPublisherArgs<TValue, TEventValue>)}.{nameof(IWebApiPublisherArgs<TValue, TEventValue>.CreateWorkStateArgs)}.");    

            return await RunAsync(request, async (wap, ct) =>
            {
                // Use specified value or get from the request. 
                var (wapv, vex) = await ValidateValueAsync(wap, useValue, value, args.ValueIsRequired, null, cancellationToken).ConfigureAwait(false);
                if (vex is not null)
                    return await CreateActionResultFromExceptionAsync(this, request.HttpContext, vex, Settings, Logger, OnUnhandledExceptionAsync, cancellationToken).ConfigureAwait(false);

                // Process the publishing as configured.
                WorkState? ws = null;
                var r = await Result.Go()
                    .WhenAsAsync(() => args.OnBeforeValidationAsync is not null, () => args.OnBeforeValidationAsync!(wapv!, ct))
                    .WhenAsAsync(_ => args.Validator is not null, async _ => (await args.Validator!.ValidateAsync(wapv!.Value!, ct).ConfigureAwait(false)).ToResult<TValue>())
                    .WhenAsync(_ => args.OnBeforeEventAsync is not null, _ => args.OnBeforeEventAsync!(wapv!, ct))
                    .ThenAs(_ => args.EventTemplate is null ? new EventData() : new EventData(args.EventTemplate))
                    .WhenAs(e => args.AreSameType, e => e.Adjust(x => x.Value = wapv!.Value), e => e.Adjust(x => x.Value = args.Mapper is not null ? args.Mapper.Map(wapv!.Value) : Mapper.Map<TValue, TEventValue>(wapv!.Value)))
                    .Then(e => args.OnEvent?.Invoke(e))
                    .Then(e =>
                    {
                        if (args.EventName is null)
                            EventPublisher.Publish(e);
                        else
                            EventPublisher.PublishNamed(args.EventName, e);
                    })
                    .ThenAsync(async e =>
                    {
                        await EventPublisher.SendAsync(ct).ConfigureAwait(false);
                    })
                    .WhenAsync(e => WorkStateOrchestrator is not null && args.CreateWorkStateArgs is not null, async e =>
                    {
                        var wsa = args.CreateWorkStateArgs!();
                        wsa.Id = e.Id;
                        wsa.CorrelationId = e.CorrelationId;
                        wsa.Key ??= e.Key;
                        ws = await WorkStateOrchestrator!.CreateAsync(wsa).ConfigureAwait(false);
                    });

                if (r.IsFailure)
                    return await CreateActionResultFromExceptionAsync(this, request.HttpContext, r.Error, Settings, Logger, OnUnhandledExceptionAsync, cancellationToken).ConfigureAwait(false);

                if (args.CreateSuccessResultAsync is not null)
                    return await args.CreateSuccessResultAsync().ConfigureAwait(false) ?? throw new InvalidOperationException($"The {nameof(IWebApiPublisherArgs<TValue, TEventValue>.CreateSuccessResultAsync)} must return a result.");

                return ValueContentResult.CreateResult(ws, HttpStatusCode.Accepted, HttpStatusCode.Accepted, JsonSerializer, request.GetRequestOptions(), false, args.CreateLocation?.Invoke(wapv!, r.Value));
            }, args.OperationType, cancellationToken, nameof(PublishAsync)).ConfigureAwait(false);
        }

        #endregion

        #region PublishCollectionAsync

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TColl"/> where each item is to be published using the <see cref="EventPublisher"/>.
        /// </summary>
        /// <typeparam name="TColl">The request JSON collection <see cref="Type"/>.</typeparam>
        /// <typeparam name="TItem">The collection item <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="args">The <see cref="WebApiPublisherCollectionArgs{TColl, TItem}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> of <see cref="HttpMethods.Post"/>.</remarks>
        public Task<IActionResult> PublishCollectionAsync<TColl, TItem>(HttpRequest request, WebApiPublisherCollectionArgs<TColl, TItem>? args = null, CancellationToken cancellationToken = default) where TColl : IEnumerable<TItem>
            => PublishCollectionOrchestrateAsync(request, false, default!, args ?? new WebApiPublisherCollectionArgs<TColl, TItem>(), cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TColl"/> where each item is to be published using the <see cref="EventPublisher"/>.
        /// </summary>
        /// <typeparam name="TColl">The request JSON collection <see cref="Type"/>.</typeparam>
        /// <typeparam name="TItem">The collection item <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The value (already deserialized).</param>
        /// <param name="args">The <see cref="WebApiPublisherCollectionArgs{TColl, TItem}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> of <see cref="HttpMethods.Post"/>.</remarks>
        public Task<IActionResult> PublishCollectionValueAsync<TColl, TItem>(HttpRequest request, TColl value, WebApiPublisherCollectionArgs<TColl, TItem>? args = null, CancellationToken cancellationToken = default) where TColl : IEnumerable<TItem>
            => PublishCollectionOrchestrateAsync(request, true, value, args ?? new WebApiPublisherCollectionArgs<TColl, TItem>(), cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TColl"/> where each item is to be published using the <see cref="EventPublisher"/>.
        /// </summary>
        /// <typeparam name="TColl">The request JSON collection <see cref="Type"/>.</typeparam>
        /// <typeparam name="TItem">The collection item <see cref="Type"/>.</typeparam>
        /// <typeparam name="TEventItem">The <typeparamref name="TItem"/>-equivalent <see cref="EventData.Value"/> <see cref="Type"/> (where different to the request).</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="args">The <see cref="IWebApiPublisherCollectionArgs{TColl, TItem, TEventItem}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> of <see cref="HttpMethods.Post"/>.</remarks>
        public Task<IActionResult> PublishCollectionAsync<TColl, TItem, TEventItem>(HttpRequest request, WebApiPublisherCollectionArgs<TColl, TItem, TEventItem>? args = null, CancellationToken cancellationToken = default) where TColl : IEnumerable<TItem>
            => PublishCollectionOrchestrateAsync(request, false, default!, args ?? new WebApiPublisherCollectionArgs<TColl, TItem, TEventItem>(), cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TColl"/> where each item is to be published using the <see cref="EventPublisher"/>.
        /// </summary>
        /// <typeparam name="TColl">The request JSON collection <see cref="Type"/>.</typeparam>
        /// <typeparam name="TItem">The collection item <see cref="Type"/>.</typeparam>
        /// <typeparam name="TEventItem">The <typeparamref name="TItem"/>-equivalent <see cref="EventData.Value"/> <see cref="Type"/> (where different to the request).</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The value (already deserialized).</param>
        /// <param name="args">The <see cref="IWebApiPublisherCollectionArgs{TColl, TItem, TEventItem}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> of <see cref="HttpMethods.Post"/>.</remarks>
        public Task<IActionResult> PublishCollectionValueAsync<TColl, TItem, TEventItem>(HttpRequest request, TColl value, WebApiPublisherCollectionArgs<TColl, TItem, TEventItem>? args = null, CancellationToken cancellationToken = default) where TColl : IEnumerable<TItem>
            => PublishCollectionOrchestrateAsync(request, true, value, args ?? new WebApiPublisherCollectionArgs<TColl, TItem, TEventItem>(), cancellationToken);

        /// <summary>
        /// Performs the publish orchestration.
        /// </summary>
        private async Task<IActionResult> PublishCollectionOrchestrateAsync<TColl, TItem, TEventValue>(HttpRequest request, bool useValue, TColl coll, IWebApiPublisherCollectionArgs<TColl, TItem, TEventValue> args, CancellationToken cancellationToken = default) where TColl : IEnumerable<TItem>
        {
            request.ThrowIfNull();
            args.ThrowIfNull();

            if (request.Method != HttpMethods.Post)
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Post}' to use {nameof(PublishAsync)}.", nameof(request));

            return await RunAsync(request, async (wap, ct) =>
            {
                // Use specified value or get from the request. 
                var (wapc, vex) = await ValidateValueAsync(wap, useValue, coll, true, null, cancellationToken).ConfigureAwait(false);
                if (vex is not null)
                    return await CreateActionResultFromExceptionAsync(this, request.HttpContext, vex, Settings, Logger, OnUnhandledExceptionAsync, cancellationToken).ConfigureAwait(false);

                // Check the collection size.
                var max = args.MaxCollectionSize ?? Settings.MaxPublishCollSize;
                if (max <= 0)
                    throw new InvalidOperationException($"The maximum collection size must be greater than zero.");

                var count = wapc!.Value?.Count() ?? 0;
                if (count > max)
                {
                    Logger.LogWarning("The publish collection contains {EventsCount} items where only a maximum size of {MaxCollSize} is supported; request has been rejected.", count, max);
                    var bex = new BusinessException($"The publish collection contains {count} items where only a maximum size of {max} is supported.");
                    return await CreateActionResultFromExceptionAsync(this, request.HttpContext, bex, Settings, Logger, OnUnhandledExceptionAsync, cancellationToken).ConfigureAwait(false);
                }

                if (count == 0)
                    return new AcceptedResult();

                // Process the publishing as configured.
                var r = await Result.Go()
                    .WhenAsAsync(() => args.OnBeforeValidationAsync is not null, () => args.OnBeforeValidationAsync!(wapc!, ct))
                    .WhenAsAsync(_ => args.Validator is not null, async _ => (await args.Validator!.ValidateAsync(wapc!.Value!, ct).ConfigureAwait(false)).ToResult<TColl>())
                    .WhenAsync(_ => args.OnBeforeEventAsync is not null, _ => args.OnBeforeEventAsync!(wapc!, ct));

                if (r.IsFailure)
                    return await CreateActionResultFromExceptionAsync(this, request.HttpContext, r.Error, Settings, Logger, OnUnhandledExceptionAsync, cancellationToken).ConfigureAwait(false);

                // Create the events (also performing mapping where applicable).
                foreach (var item in wapc!.Value!)
                {
                    var @event = args.EventTemplate is null ? new EventData() : new EventData(args.EventTemplate);
                    @event.Value = args.AreSameType ? item : (args.Mapper is not null ? args.Mapper.Map(item) : Mapper.Map<TItem, TEventValue>(item));
                    args.OnEvent?.Invoke(@event);

                    if (args.EventName is null)
                        EventPublisher.Publish(@event);
                    else
                        EventPublisher.PublishNamed(args.EventName, @event);
                }

                await EventPublisher.SendAsync(ct).ConfigureAwait(false);

                if (args.CreateSuccessResultAsync is not null)
                    return await args.CreateSuccessResultAsync().ConfigureAwait(false) ?? throw new InvalidOperationException($"The {nameof(IWebApiPublisherCollectionArgs<TColl, TItem, TEventValue>.CreateSuccessResultAsync)} must return a result.");

                return new ExtendedStatusCodeResult(args.StatusCode);
            }, args.OperationType, cancellationToken, nameof(PublishAsync)).ConfigureAwait(false);
        }

        #endregion

        #region Async Request-Response

        /// <summary>
        /// Performs a <see cref="HttpMethods.Get"/> operation to get the <see cref="WorkState"/> <see cref="WorkState.Status"/> to enable the likes of the <i>asynchronous request-response</i> pattern.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="args">The <see cref="WebApiPublisherStatusArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="IActionResult"/>.</returns>
        public Task<IActionResult> GetWorkStatusAsync(HttpRequest request, WebApiPublisherStatusArgs args, CancellationToken cancellationToken = default)
        {
            if (WorkStateOrchestrator is null)
                throw new InvalidOperationException($"The {nameof(GetWorkStatusAsync)} operation requires that the {nameof(WorkStateOrchestrator)} is not null.");

            request.ThrowIfNull(nameof(request));
            args.ThrowIfNull(nameof(args));

            if (request.Method != HttpMethods.Get)
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Get}' to use {nameof(GetWorkStatusAsync)}.", nameof(request));

            return RunAsync(request, async (wap, ct) =>
            {
                var ws = string.IsNullOrEmpty(args.Id) ? null : await WorkStateOrchestrator!.GetAsync(args.TypeName, args.Id, ct).ConfigureAwait(false);
                if (ws is null)
                    return new ExtendedStatusCodeResult(HttpStatusCode.NotFound);

                if (args.OnBeforeResponseAsync is not null)
                {
                    var r = await args.OnBeforeResponseAsync(ws).ConfigureAwait(false);
                    if (r.IsFailure)
                        return await CreateActionResultFromExceptionAsync(this, request.HttpContext, r.Error, Settings, Logger, OnUnhandledExceptionAsync, cancellationToken).ConfigureAwait(false);
                }

                // Check for the completed status and redirect to the result location where applicable.
                if (ws.Status == WorkStatus.Completed && args.CreateResultLocation is not null)
                    return new ExtendedStatusCodeResult(HttpStatusCode.Redirect) { Location = args.CreateResultLocation(ws) ?? throw new InvalidOperationException("A result location is ") };

                // Return the work status as either a BadRequest or OK.
                var res = ValueContentResult.CreateResult(ws, WorkStatus.Terminated.HasFlag(ws.Status) ? HttpStatusCode.BadRequest : HttpStatusCode.OK, null, JsonSerializer, request.GetRequestOptions(), true, null);
                if (res is ValueContentResult vcr && WorkStatus.Executing.HasFlag(ws.Status))
                    vcr.RetryAfter = args.ExecutingRetryAfter;

                return res;

            }, OperationType.Unspecified, cancellationToken, nameof(GetWorkStatusAsync));
        }

        /// <summary>
        /// Performs a <see cref="HttpMethods.Get"/> operation to get the <see cref="WorkState"/> result with no resulting value by default.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="args">The <see cref="WebApiPublisherResultArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="IActionResult"/>.</returns>
        public Task<IActionResult> GetWorkResultAsync(HttpRequest request, WebApiPublisherResultArgs args, CancellationToken cancellationToken = default)
        {
            if (WorkStateOrchestrator is null)
                throw new InvalidOperationException($"The {nameof(GetWorkResultAsync)} operation requires that the {nameof(WorkStateOrchestrator)} is not null.");

            request.ThrowIfNull(nameof(request));
            args.ThrowIfNull(nameof(args));

            if (request.Method != HttpMethods.Get)
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Get}' to use {nameof(GetWorkResultAsync)}.", nameof(request));

            return RunAsync(request, async (wap, ct) =>
            {
                var ws = string.IsNullOrEmpty(args.Id) ? null : await WorkStateOrchestrator!.GetAsync(args.TypeName, args.Id, ct).ConfigureAwait(false);
                if (ws is null || ws.Status != WorkStatus.Completed)
                    return new ExtendedStatusCodeResult(HttpStatusCode.NotFound);

                if (args.OnBeforeResponseAsync is not null)
                {
                    var r = await args.OnBeforeResponseAsync(ws).ConfigureAwait(false);
                    if (r.IsFailure)
                        return await CreateActionResultFromExceptionAsync(this, request.HttpContext, r.Error, Settings, Logger, OnUnhandledExceptionAsync, cancellationToken).ConfigureAwait(false);
                }

                if (args.CreateSuccessResultAsync is not null)
                    return await args.CreateSuccessResultAsync(ws).ConfigureAwait(false) ?? throw new InvalidOperationException($"The {nameof(WebApiPublisherResultArgs.CreateSuccessResultAsync)} must return a result.");

                return new ExtendedStatusCodeResult(HttpStatusCode.NoContent); 

            }, OperationType.Unspecified, cancellationToken, nameof(GetWorkResultAsync));
        }

        /// <summary>
        /// Performs a <see cref="HttpMethods.Get"/> operation to get the <see cref="WorkState"/> result with a result of <see cref="Type"/> <typeparamref name="TValue"/>.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="args">The <see cref="WebApiPublisherResultArgs{TValue}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="IActionResult"/>.</returns>
        public Task<IActionResult> GetWorkResultAsync<TValue>(HttpRequest request, WebApiPublisherResultArgs<TValue> args, CancellationToken cancellationToken = default)
        {
            if (WorkStateOrchestrator is null)
                throw new InvalidOperationException($"The {nameof(GetWorkResultAsync)} operation requires that the {nameof(WorkStateOrchestrator)} is not null.");

            request.ThrowIfNull(nameof(request));
            args.ThrowIfNull(nameof(args));

            if (request.Method != HttpMethods.Get)
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Get}' to use {nameof(GetWorkResultAsync)}.", nameof(request));

            return RunAsync(request, async (wap, ct) =>
            {
                var ws = string.IsNullOrEmpty(args.Id) ? null : await WorkStateOrchestrator!.GetAsync(args.TypeName, args.Id, ct).ConfigureAwait(false);
                if (ws is null || ws.Status != WorkStatus.Completed)
                    return new ExtendedStatusCodeResult(HttpStatusCode.NotFound);

                if (args.OnBeforeResponseAsync is not null)
                {
                    var r = await args.OnBeforeResponseAsync(ws).ConfigureAwait(false);
                    if (r.IsFailure)
                        return await CreateActionResultFromExceptionAsync(this, request.HttpContext, r.Error, Settings, Logger, OnUnhandledExceptionAsync, cancellationToken).ConfigureAwait(false);
                }

                var value = await WorkStateOrchestrator.GetDataAsync<TValue>(args.Id!, ct).ConfigureAwait(false);
                return ValueContentResult.CreateResult(value, HttpStatusCode.OK, HttpStatusCode.NoContent, JsonSerializer, request.GetRequestOptions(), true, null);
            }, OperationType.Unspecified, cancellationToken, nameof(GetWorkResultAsync));
        }

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation to cancel the <see cref="WorkState"/> <see cref="WorkState.Status"/> to enable the likes of the <i>asynchronous request-response</i> pattern.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="args">The <see cref="WebApiPublisherCancelArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="IActionResult"/>.</returns>
        public Task<IActionResult> CancelWorkStatusAsync(HttpRequest request, WebApiPublisherCancelArgs args, CancellationToken cancellationToken = default)
        {
            if (WorkStateOrchestrator is null)
                throw new InvalidOperationException($"The {nameof(CancelWorkStatusAsync)} operation requires that the {nameof(WorkStateOrchestrator)} is not null.");

            request.ThrowIfNull(nameof(request));
            args.ThrowIfNull(nameof(args));

            if (request.Method != HttpMethods.Get)
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Get}' to use {nameof(CancelWorkStatusAsync)}.", nameof(request));

            return RunAsync(request, async (wap, ct) =>
            {
                var ws = string.IsNullOrEmpty(args.Id) ? null : await WorkStateOrchestrator!.GetAsync(args.TypeName, args.Id, ct).ConfigureAwait(false);
                if (ws is null)
                    return new ExtendedStatusCodeResult(HttpStatusCode.NotFound);

                if (args.OnBeforeResponseAsync is not null)
                {
                    var r = await args.OnBeforeResponseAsync(ws).ConfigureAwait(false);
                    if (r.IsFailure)
                        return await CreateActionResultFromExceptionAsync(this, request.HttpContext, r.Error, Settings, Logger, OnUnhandledExceptionAsync, cancellationToken).ConfigureAwait(false);
                }

                // Check for the completed status and redirect to the result location where applicable.
                var cr = await WorkStateOrchestrator!.CancelAsync(args.Id!, args.Reason ?? WebApiPublisherCancelArgs.NotSpecifiedReason, ct).ConfigureAwait(false);
                if (cr.IsFailure)
                    return await CreateActionResultFromExceptionAsync(this, request.HttpContext, cr.Error, Settings, Logger, OnUnhandledExceptionAsync, cancellationToken).ConfigureAwait(false);

                return ValueContentResult.CreateResult(cr.Value, HttpStatusCode.OK, null, JsonSerializer, request.GetRequestOptions(), true, null);
            }, OperationType.Unspecified, cancellationToken, nameof(CancelWorkStatusAsync));
        }

        #endregion
    }
}