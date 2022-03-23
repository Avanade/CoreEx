// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using CoreEx.Events;
using CoreEx.Http;
using CoreEx.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CoreEx.WebApis
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
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        public WebApiPublisher(IEventPublisher eventPublisher, ExecutionContext executionContext, SettingsBase settings, IJsonSerializer jsonSerializer, ILogger<WebApiPublisher> logger)
            : base(executionContext, settings, jsonSerializer, logger) => EventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));

        /// <summary>
        /// Gets the <see cref="IEventPublisher"/>.
        /// </summary>
        public IEventPublisher EventPublisher { get; }

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
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> or <see cref="HttpMethods.Post"/>.</remarks>
        public async Task<IActionResult> PublishAsync<TValue>(HttpRequest request, string? eventName, Func<WebApiParam<TValue>, Task>? beforeEvent = null, Action<EventData, TValue>? eventModifier = null, HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Method != HttpMethods.Post)
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Post}' to use {nameof(PublishAsync)}.", nameof(request));

            return await RunAsync(request, async wap =>
            {
                var vr = await request.ReadAsJsonValueAsync<TValue>(JsonSerializer).ConfigureAwait(false);
                if (vr.IsInvalid)
                    return vr.ToBadRequestResult();

                if (beforeEvent != null)
                    await beforeEvent(new WebApiParam<TValue>(wap, vr.Value)).ConfigureAwait(false);

                var @event = new EventData { Value = vr.Value };
                eventModifier?.Invoke(@event, vr.Value);

                if (eventName == null)
                    EventPublisher.Publish(@event);
                else
                    EventPublisher.Publish(eventName, @event);

                await EventPublisher.SendAsync().ConfigureAwait(false);

                return new ExtendedStatusCodeResult(statusCode);
            }, operationType).ConfigureAwait(false);
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
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>The <paramref name="request"/> must have an <see cref="HttpRequest.Method"/> or <see cref="HttpMethods.Post"/>.</remarks>
        public async Task<IActionResult> PublishAsync<TColl, TItem>(HttpRequest request, string? eventName, Func<WebApiParam<TColl>, Task>? beforeEvents = null, Action<EventData, TItem>? eventModifier = null, int? maxCollSize = null, HttpStatusCode statusCode = HttpStatusCode.Accepted, OperationType operationType = OperationType.Unspecified)
            where TColl : IEnumerable<TItem>
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Method != HttpMethods.Post)
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Post}' to use {nameof(PublishAsync)}.", nameof(request));

            return await RunAsync(request, async wap =>
            {
                var vr = await request.ReadAsJsonValueAsync<TColl>(JsonSerializer).ConfigureAwait(false);
                if (vr.IsInvalid)
                    return vr.ToBadRequestResult();

                if (beforeEvents != null)
                    await beforeEvents(new WebApiParam<TColl>(wap, vr.Value)).ConfigureAwait(false);

                var max = maxCollSize ?? Settings.MaxPublishCollSize;
                var count = vr.Value.Count();
                if (count > max)
                {
                    Logger.LogWarning("The publish collection contains {EventsCount} items where only a maximum size of {MaxCollSize} is supported; request has been rejected.", count, max);
                    return new BadRequestObjectResult($"The publish collection contains {count} items where only a maximum size of {max} is supported.");
                }

                if (count == 0)
                    return new AcceptedResult();

                foreach (var item in vr.Value)
                {
                    var ed = new EventData { Value = item };
                    eventModifier?.Invoke(ed, item);
                    if (eventName == null)
                        EventPublisher.Publish(ed);
                    else
                        EventPublisher.Publish(eventName, ed);
                }

                await EventPublisher.SendAsync().ConfigureAwait(false);

                return new ExtendedStatusCodeResult(statusCode);
            }, operationType).ConfigureAwait(false);
        }
    }
}