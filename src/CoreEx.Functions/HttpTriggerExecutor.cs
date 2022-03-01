// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Configuration;
using CoreEx.Events;
using CoreEx.Http;
using CoreEx.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;

namespace CoreEx.Functions
{
    /// <summary>
    /// Provides the standard <see cref="HttpTriggerAttribute"/> execution encapsulation to run the underlying function logic in a consistent manner.
    /// </summary>
    /// <remarks>Each <c>Run</c> is wrapped with the same logic. The correlation identifier is set using the <see cref="Executor.GetCorrelationIdNames"/> (names in priority order) to retrieve
    /// the corresponding HTTP Header value; where not found a <see cref="Guid.NewGuid"/> is used as the default. A <see cref="ILogger.BeginScope{TState}(TState)"/> with the <see cref="ExecutionContext.CorrelationId"/> is performed to wrap the logic
    /// with the correlation identifier. The following exceptions are caught and handled as follows: <see cref="ValidationException"/> results in <see cref="HttpStatusCode.BadRequest"/>, <see cref="TransientException"/> results in
    /// <see cref="HttpStatusCode.ServiceUnavailable"/>, <see cref="EventPublisherException"/> results in <see cref="HttpStatusCode.InternalServerError"/>; and finally, any unhandled exception results in <see cref="HttpStatusCode.InternalServerError"/>.</remarks>
    public class HttpTriggerExecutor : Executor, IHttpTriggerExecutor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpTriggerExecutor"/> class.
        /// </summary>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/></param>
        /// <param name="executionContext">The <see cref="ExecutionContext"/>.</param>
        /// <param name="settings">The <see cref="SettingsBase"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public HttpTriggerExecutor(IJsonSerializer jsonSerializer, ExecutionContext executionContext, SettingsBase settings, ILogger<HttpTriggerExecutor> logger) : base(executionContext, settings, logger)
            => JsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));

        /// <summary>
        /// Gets the <see cref="IJsonSerializer"/>.
        /// </summary>
        public IJsonSerializer JsonSerializer { get; }

        /// <inheritdoc/>
        public async Task<IActionResult> RunAsync(HttpRequest request, Func<Task<IActionResult>> function)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            foreach (var name in GetCorrelationIdNames())
            {
                if (request.Headers.TryGetValue(name, out var values))
                {
                    ExecutionContext.Current.CorrelationId = values.First();
                    break;
                }
            }

            request.HttpContext.Response.Headers.Add(CorrelationIdName, ExecutionContext.Current.CorrelationId);

            var scope = Logger.BeginScope(new Dictionary<string, object>() { { CorrelationIdName, ExecutionContext.Current.CorrelationId } });

            try
            {
                return await function().ConfigureAwait(false);
            }
            catch (EventPublisherException epex)
            {
                Logger.LogCritical(epex, epex.Message);
                return epex.ToResult();
            }
            catch (Exception ex)
            {
                if (ex is IExtendedException eex)
                {
                    if (eex.ShouldBeLogged)
                        Logger.LogError(ex, "{Error}", ex.Message);

                    return eex.ToResult();
                }

                Logger.LogCritical(ex, "Executor encountered an Unhandled Exception: {Error}", ex.Message);
                return (ex is IExceptionResult rex) ? rex.ToResult() : ex.ToUnexpectedResult(Settings.IncludeExceptionInResult);
            }
            finally
            {
                scope.Dispose();
            }
        }

        /// <inheritdoc/>
        public async Task<IActionResult> RunAsync<TRequest>(HttpRequest request, Func<TRequest, Task> function, bool valueIsRequired = true, HttpStatusCode successStatusCode = HttpStatusCode.NoContent)
        {
            if (function == null)
                throw new ArgumentNullException(nameof(function));

            return await RunAsync(request, async () =>
            {
                var vr = await request.ReadAsJsonValueAsync<TRequest>(JsonSerializer, valueIsRequired).ConfigureAwait(false);
                if (vr.IsInvalid)
                    return vr.ToBadRequestResult();

                await function(vr.Value!).ConfigureAwait(false);
                return new StatusCodeResult((int)successStatusCode);
            }).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<IActionResult> RunWithResultAsync<TResult>(HttpRequest request, Func<Task<TResult>> function, HttpStatusCode successStatusCode = HttpStatusCode.OK)
        {
            if (function == null)
                throw new ArgumentNullException(nameof(function));

            return await RunAsync(request, async () =>
            {
                var result = await function().ConfigureAwait(false);
                return new ContentResult { Content = JsonSerializer.Serialize(result), ContentType = MediaTypeNames.Application.Json, StatusCode = (int)successStatusCode };
            }).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<IActionResult> RunWithResultAsync<TRequest, TResult>(HttpRequest request, Func<TRequest, Task<TResult>> function, bool valueIsRequired = true, HttpStatusCode successStatusCode = HttpStatusCode.OK)
        {
            if (function == null)
                throw new ArgumentNullException(nameof(function));

            return await RunAsync(request, async () =>
            {
                var vr = await request.ReadAsJsonValueAsync<TRequest>(JsonSerializer, valueIsRequired).ConfigureAwait(false);
                if (vr.IsInvalid)
                    return vr.ToBadRequestResult();

                var result = await function(vr.Value!).ConfigureAwait(false);
                return new ContentResult { Content = JsonSerializer.Serialize(result), ContentType = MediaTypeNames.Application.Json, StatusCode = (int)successStatusCode };
            }).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<IActionResult> RunPublishAsync<TRequest>(HttpRequest request, IEventPublisher eventPublisher, string? eventName, Action<EventData, TRequest>? eventModifier = null, bool valueIsRequired = true, HttpStatusCode successStatusCode = HttpStatusCode.Accepted, Func<TRequest, Task>? beforeEvent = null)
        {
            if (eventPublisher == null)
                throw new ArgumentNullException(nameof(eventPublisher));

            return await RunAsync(request, async () =>
            {
                var vr = await request.ReadAsJsonValueAsync<TRequest>(JsonSerializer, valueIsRequired).ConfigureAwait(false);
                if (vr.IsInvalid)
                    return vr.ToBadRequestResult();

                if (beforeEvent != null)
                    await beforeEvent(vr.Value).ConfigureAwait(false);

                var @event = new EventData { Value = vr.Value };
                eventModifier?.Invoke(@event, vr.Value);

                if (eventName == null)
                    await eventPublisher.SendAsync(@event).ConfigureAwait(false);
                else
                    await eventPublisher.SendAsync(eventName, @event).ConfigureAwait(false);

                return new StatusCodeResult((int)successStatusCode);
            }).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<IActionResult> RunPublishCollAsync<TColl, TItem>(HttpRequest request, IEventPublisher eventPublisher, string? eventName, Action<EventData, TItem>? eventModifier = null, bool valueIsRequired = true, int? maxListSize = null, HttpStatusCode successStatusCode = HttpStatusCode.Accepted, Func<TColl, Task>? beforeEvents = null) where TColl : IEnumerable<TItem>
        {
            if (eventPublisher == null)
                throw new ArgumentNullException(nameof(eventPublisher));

            return await RunAsync(request, async () =>
            {
                var vr = await request.ReadAsJsonValueAsync<TColl>(JsonSerializer, valueIsRequired).ConfigureAwait(false);
                if (vr.IsInvalid)
                    return vr.ToBadRequestResult();

                if (beforeEvents != null)
                    await beforeEvents(vr.Value).ConfigureAwait(false);

                var events = new List<EventData>();
                foreach (var item in vr.Value)
                {
                    var ed = new EventData { Value = item };
                    eventModifier?.Invoke(ed, item);
                    events.Add(ed);
                }

                if (events.Count == 0)
                    return new AcceptedResult();

                var max = maxListSize ?? Settings.GetMaxPublishCollSize();
                if (events.Count > max)
                {
                    Logger.LogDebug("The publish collection contains {EventsCount} items where only a maximum size of {MaxCollSize} is supported; request has been rejected.", events.Count, max);
                    return new BadRequestObjectResult($"The publish collection contains {events.Count} items where only a maximum size of {max} is supported.");
                }

                if (eventName == null)
                    await eventPublisher.SendAsync(events.ToArray()).ConfigureAwait(false);
                else
                    await eventPublisher.SendAsync(eventName, events.ToArray()).ConfigureAwait(false);

                return new StatusCodeResult((int)successStatusCode);
            }).ConfigureAwait(false);
        }
    }
}