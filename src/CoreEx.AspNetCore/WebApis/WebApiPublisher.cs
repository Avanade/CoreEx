// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using CoreEx.Events;
using CoreEx.Json;
using CoreEx.Mapping;
using CoreEx.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.AspNetCore.WebApis
{
    /// <summary>
    /// Provides the core <see cref="IEventPublisher"/> Web API execution encapsulation.
    /// </summary>
    /// <remarks>Support to change/map request into a different published event type is also enabled where required (see also <seealso cref="Mapper"/>).</remarks>
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

        #region PublishAsync

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> that is to be published using the <see cref="EventPublisher"/>.
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
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> that is to be published using the <see cref="EventPublisher"/>.
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

            return await RunAsync(request, async (wap, ct) =>
            {
                // Use specified value or get from the request. 
                var (wapv, vex) = await ValidateValueAsync(wap, useValue, value, true, null, cancellationToken).ConfigureAwait(false);
                if (vex is not null)
                    return await CreateActionResultFromExceptionAsync(this, request.HttpContext, vex, Settings, Logger, OnUnhandledExceptionAsync, cancellationToken).ConfigureAwait(false);

                // Process the publishing as configured.
                var r = await Result.Go()
                    .WhenAsAsync(() => args.OnBeforeValidationAsync is not null, () => args.OnBeforeValidationAsync!(wapv!, ct))
                    .WhenAsAsync(_ => args.Validator is not null, async _ => (await args.Validator!.ValidateAsync(wapv!.Value!, ct).ConfigureAwait(false)).ToResult<TValue>())
                    .WhenAsync(_ => args.OnBeforeEventAsync is not null, _ => args.OnBeforeEventAsync!(wapv!, ct))
                    .WhenAs(_ => args.AreSameType, _ => new EventData { Value = wapv!.Value }, _ => new EventData { Value = args.Mapper is not null ? args.Mapper.Map(wapv!.Value) : Mapper.Map<TValue, TEventValue>(wapv!.Value) })
                    .Then(e => args.OnEvent?.Invoke(e))
                    .ThenAs(e => args.EventName is null ? EventPublisher.Publish(e) : EventPublisher.PublishNamed(args.EventName, e))
                    .ThenAsync(_ => EventPublisher.SendAsync(ct)).ConfigureAwait(false);

                if (r.IsFailure)
                    return await CreateActionResultFromExceptionAsync(this, request.HttpContext, r.Error, Settings, Logger, OnUnhandledExceptionAsync, cancellationToken).ConfigureAwait(false);

                return args.CreateSuccessResult?.Invoke() ?? new ExtendedStatusCodeResult(args.StatusCode);
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
                    var @event = new EventData { Value = args.AreSameType ? item : (args.Mapper is not null ? args.Mapper.Map(item) : Mapper.Map<TItem, TEventValue>(item)) };
                    args.OnEvent?.Invoke(@event);

                    if (args.EventName is null)
                        EventPublisher.Publish(@event);
                    else
                        EventPublisher.PublishNamed(args.EventName, @event);
                }

                await EventPublisher.SendAsync(ct).ConfigureAwait(false);

                return args.CreateSuccessResult?.Invoke() ?? new ExtendedStatusCodeResult(args.StatusCode);
            }, args.OperationType, cancellationToken, nameof(PublishAsync)).ConfigureAwait(false);
        }

        #endregion
    }
}