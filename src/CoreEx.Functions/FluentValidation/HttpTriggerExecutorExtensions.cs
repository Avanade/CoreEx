// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Events;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;

namespace CoreEx.Functions.FluentValidation
{
    /// <summary>
    /// Extension methods for <see cref="IHttpTriggerExecutor"/>.
    /// </summary>
    public static class HttpTriggerExecutorExtensions
    {
        /// <summary>
        /// Encapsulates the execution of an <see cref="HttpRequest"/> <paramref name="function"/> including converting the HTTP JSON <see cref="HttpRequest.Body"/> to <see cref="Type"/> <typeparamref name="TRequest"/> returning the <paramref name="successStatusCode"/>.
        /// </summary>
        /// <typeparam name="TRequest">The request value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValidator">The <typeparamref name="TRequest"/> validator <see cref="Type"/>.</typeparam>
        /// <param name="executor">The <see cref="IHttpTriggerExecutor"/>.</param>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function logic to invoke that is passed the converted HTTP JSON <see cref="HttpRequest.Body"/> for use.</param>
        /// <param name="valueIsRequired">Indicates whether the value is required; will consider invalid where null.</param>
        /// <param name="successStatusCode">The success <see cref="HttpStatusCode"/>; defaults to <see cref="HttpStatusCode.NoContent"/>.</param>
        /// <returns>The resulting <see cref="IActionResult"/>.</returns>
        public static Task<IActionResult> RunAsync<TRequest, TValidator>(this IHttpTriggerExecutor executor, HttpRequest request, Func<TRequest, Task> function, bool valueIsRequired = true, HttpStatusCode successStatusCode = HttpStatusCode.NoContent)
            where TValidator : AbstractValidator<TRequest>, new()
            => RunAsync(executor, request, new TValidator(), function, valueIsRequired, successStatusCode);

        /// <summary>
        /// Encapsulates the execution of an <see cref="HttpRequest"/> <paramref name="function"/> including converting the HTTP JSON <see cref="HttpRequest.Body"/> to <see cref="Type"/> <typeparamref name="TRequest"/> returning the <paramref name="successStatusCode"/>.
        /// </summary>
        /// <typeparam name="TRequest">The request value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValidator">The <typeparamref name="TRequest"/> validator <see cref="Type"/>.</typeparam>
        /// <param name="executor">The <see cref="IHttpTriggerExecutor"/>.</param>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="validator">The  <see cref="AbstractValidator{TRequest}"/>.</param>
        /// <param name="function">The function logic to invoke that is passed the converted HTTP JSON <see cref="HttpRequest.Body"/> for use.</param>
        /// <param name="valueIsRequired">Indicates whether the value is required; will consider invalid where null.</param>
        /// <param name="successStatusCode">The success <see cref="HttpStatusCode"/>; defaults to <see cref="HttpStatusCode.NoContent"/>.</param>
        /// <returns>The resulting <see cref="IActionResult"/>.</returns>
        public static async Task<IActionResult> RunAsync<TRequest, TValidator>(this IHttpTriggerExecutor executor, HttpRequest request, TValidator validator, Func<TRequest, Task> function, bool valueIsRequired = true, HttpStatusCode successStatusCode = HttpStatusCode.NoContent)
            where TValidator : AbstractValidator<TRequest>
        {
            if (executor == null)
                throw new ArgumentNullException(nameof(executor));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            return await executor.RunAsync(request, async () =>
            {
                var vr = await request.ReadAsJsonValidatedValueAsync<TRequest, TValidator>(executor.JsonSerializer, validator, valueIsRequired).ConfigureAwait(false);
                if (vr.IsInvalid)
                    return vr.ToBadRequestResult();

                await function(vr.Value!).ConfigureAwait(false);
                return new StatusCodeResult((int)successStatusCode);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Encapsulates the execution of an <see cref="HttpRequest"/> <paramref name="function"/> including converting the HTTP JSON <see cref="HttpRequest.Body"/> to <see cref="Type"/> <typeparamref name="TRequest"/> returning the JSON-serialized <typeparamref name="TResult"/> value and <paramref name="successStatusCode"/>.
        /// </summary>
        /// <typeparam name="TRequest">The request value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValidator">The <typeparamref name="TRequest"/> validator <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResult">The result value <see cref="Type"/>.</typeparam>
        /// <param name="executor">The <see cref="IHttpTriggerExecutor"/>.</param>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function logic to invoke that is passed the converted HTTP JSON <see cref="HttpRequest.Body"/> for use.</param>
        /// <param name="valueIsRequired">Indicates whether the value is required; will consider invalid where null.</param>
        /// <param name="successStatusCode">The success <see cref="HttpStatusCode"/>; defaults to <see cref="HttpStatusCode.OK"/>.</param>
        /// <returns>The resulting <see cref="IActionResult"/>.</returns>
        public static Task<IActionResult> RunWithResultAsync<TRequest, TValidator, TResult>(this IHttpTriggerExecutor executor, HttpRequest request, Func<TRequest, Task<TResult>> function, bool valueIsRequired = true, HttpStatusCode successStatusCode = HttpStatusCode.OK)
            where TValidator : AbstractValidator<TRequest>, new()
            => RunWithResultAsync(executor, request, new TValidator(), function, valueIsRequired, successStatusCode);

        /// <summary>
        /// Encapsulates the execution of an <see cref="HttpRequest"/> <paramref name="function"/> including converting the HTTP JSON <see cref="HttpRequest.Body"/> to <see cref="Type"/> <typeparamref name="TRequest"/> returning the JSON-serialized <typeparamref name="TResult"/> value and <paramref name="successStatusCode"/>.
        /// </summary>
        /// <typeparam name="TRequest">The request value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValidator">The <typeparamref name="TRequest"/> validator <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResult">The result value <see cref="Type"/>.</typeparam>
        /// <param name="executor">The <see cref="IHttpTriggerExecutor"/>.</param>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="validator">The  <see cref="AbstractValidator{TRequest}"/>.</param>
        /// <param name="function">The function logic to invoke that is passed the converted HTTP JSON <see cref="HttpRequest.Body"/> for use.</param>
        /// <param name="valueIsRequired">Indicates whether the value is required; will consider invalid where null.</param>
        /// <param name="successStatusCode">The success <see cref="HttpStatusCode"/>; defaults to <see cref="HttpStatusCode.OK"/>.</param>
        /// <returns>The resulting <see cref="IActionResult"/>.</returns>
        public static async Task<IActionResult> RunWithResultAsync<TRequest, TValidator, TResult>(this IHttpTriggerExecutor executor, HttpRequest request, TValidator validator, Func<TRequest, Task<TResult>> function, bool valueIsRequired = true, HttpStatusCode successStatusCode = HttpStatusCode.OK)
            where TValidator : AbstractValidator<TRequest>
        {
            if (executor == null)
                throw new ArgumentNullException(nameof(executor));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            return await executor.RunAsync(request, async () =>
            {
                var vr = await request.ReadAsJsonValidatedValueAsync<TRequest, TValidator>(executor.JsonSerializer, validator, valueIsRequired).ConfigureAwait(false);
                if (vr.IsInvalid)
                    return vr.ToBadRequestResult();

                var result = await function(vr.Value!).ConfigureAwait(false);
                return new ContentResult { Content = executor.JsonSerializer.Serialize(result), ContentType = MediaTypeNames.Application.Json, StatusCode = (int)successStatusCode };
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Encapsulates the execution of an <see cref="HttpRequest"/> that contains an item to be published (<paramref name="eventPublisher"/>) returning an <see cref="AcceptedResult"/> where successful.
        /// </summary>
        /// <typeparam name="TRequest">The request value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValidator">The <typeparamref name="TRequest"/> validator <see cref="Type"/>.</typeparam>
        /// <param name="executor">The <see cref="IHttpTriggerExecutor"/>.</param>
        /// <param name="request">The <see cref="HttpRequest"/></param>
        /// <param name="eventPublisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="eventName">The optional event destintion name.</param>
        /// <param name="eventModifier">An action to enable the <see cref="EventData"/> instance to be updated prior to publish.</param>
        /// <param name="valueIsRequired">Indicates whether the value is required; will consider invalid where null.</param>
        /// <param name="beforeEvent">A function that enables the item to be processed before the underlying event publishing logic is enacted.</param>
        /// <param name="successStatusCode">The success <see cref="HttpStatusCode"/>; defaults to <see cref="HttpStatusCode.Accepted"/>.</param>
        /// <returns>The resulting <see cref="IActionResult"/>.</returns>
        public static Task<IActionResult> RunPublishAsync<TRequest, TValidator>(this IHttpTriggerExecutor executor, HttpRequest request, IEventPublisher eventPublisher, string? eventName, Action<EventData, TRequest>? eventModifier = null, bool valueIsRequired = true, HttpStatusCode successStatusCode = HttpStatusCode.Accepted, Func<TRequest, Task>? beforeEvent = null)
            where TValidator : AbstractValidator<TRequest>, new ()
            => RunPublishAsync(executor, request, new TValidator(), eventPublisher, eventName, eventModifier, valueIsRequired, successStatusCode, beforeEvent);

        /// <summary>
        /// Encapsulates the execution of an <see cref="HttpRequest"/> that contains an item to be published (<paramref name="eventPublisher"/>) returning an <see cref="AcceptedResult"/> where successful.
        /// </summary>
        /// <typeparam name="TRequest">The request value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValidator">The <typeparamref name="TRequest"/> validator <see cref="Type"/>.</typeparam>
        /// <param name="executor">The <see cref="IHttpTriggerExecutor"/>.</param>
        /// <param name="request">The <see cref="HttpRequest"/></param>
        /// <param name="validator">The <see cref="AbstractValidator{TRequest}"/>.</param>
        /// <param name="eventPublisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="eventName">The optional event destintion name.</param>
        /// <param name="eventModifier">An action to enable the <see cref="EventData"/> instance to be updated prior to publish.</param>
        /// <param name="valueIsRequired">Indicates whether the value is required; will consider invalid where null.</param>
        /// <param name="beforeEvent">A function that enables the item to be processed before the underlying event publishing logic is enacted.</param>
        /// <param name="successStatusCode">The success <see cref="HttpStatusCode"/>; defaults to <see cref="HttpStatusCode.Accepted"/>.</param>
        /// <returns>The resulting <see cref="IActionResult"/>.</returns>
        public static async Task<IActionResult> RunPublishAsync<TRequest, TValidator>(this IHttpTriggerExecutor executor, HttpRequest request, TValidator validator, IEventPublisher eventPublisher, string? eventName, Action<EventData, TRequest>? eventModifier = null, bool valueIsRequired = true, HttpStatusCode successStatusCode = HttpStatusCode.Accepted, Func<TRequest, Task>? beforeEvent = null)
            where TValidator : AbstractValidator<TRequest>
            => await executor.RunPublishAsync<TRequest>(request, eventPublisher, eventName, eventModifier, valueIsRequired, successStatusCode, async (item) =>
            {
                var fvr = (validator ?? throw new ArgumentNullException(nameof(validator))).Validate(item);
                fvr.ThrowValidationException();
                if (beforeEvent != null)
                    await beforeEvent(item).ConfigureAwait(false);
            }).ConfigureAwait(false);

        /// <summary>
        /// Encapsulates the execution of an <see cref="HttpRequest"/> that contains a collection of items to be published (<paramref name="eventPublisher"/>) returning <paramref name="successStatusCode"/> where successful.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <typeparam name="TItem">The list item <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValidator">The <typeparamref name="TColl"/> validator <see cref="Type"/>.</typeparam>
        /// <param name="executor">The <see cref="IHttpTriggerExecutor"/>.</param>
        /// <param name="request">The <see cref="HttpRequest"/></param>
        /// <param name="eventPublisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="eventName">The optional event destintion name.</param>
        /// <param name="eventModifier">An action to enable each <see cref="EventData"/> instance to be updated prior to publish.</param>
        /// <param name="valueIsRequired">Indicates whether the value is required; will consider invalid where null.</param>
        /// <param name="maxListSize">Overrides the default (<see cref="SettingsBaseExtensions.GetMaxPublishCollSize"/>) maximum publish collection size.</param>
        /// <param name="successStatusCode">The success <see cref="HttpStatusCode"/>; defaults to <see cref="HttpStatusCode.Accepted"/>.</param>
        /// <param name="beforeEvents">A function that enables the list to be processed before the underlying event publishing logic is enacted.</param>
        /// <returns>The resulting <see cref="IActionResult"/>.</returns>
        public static Task<IActionResult> RunPublishCollAsync<TColl, TItem, TValidator>(this IHttpTriggerExecutor executor, HttpRequest request, IEventPublisher eventPublisher, string? eventName, Action<EventData, TItem>? eventModifier = null, bool valueIsRequired = true, int? maxListSize = null, HttpStatusCode successStatusCode = HttpStatusCode.Accepted, Func<TColl, Task>? beforeEvents = null)
            where TColl : IEnumerable<TItem>
            where TValidator : AbstractValidator<TColl>, new()
            => RunPublishCollAsync(executor, request, new TValidator(), eventPublisher, eventName, eventModifier, valueIsRequired, maxListSize, successStatusCode, beforeEvents);

        /// <summary>
        /// Encapsulates the execution of an <see cref="HttpRequest"/> that contains a collection of items to be published (<paramref name="eventPublisher"/>) returning <paramref name="successStatusCode"/> where successful.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <typeparam name="TItem">The list item <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValidator">The <typeparamref name="TColl"/> validator <see cref="Type"/>.</typeparam>
        /// <param name="executor">The <see cref="IHttpTriggerExecutor"/>.</param>
        /// <param name="request">The <see cref="HttpRequest"/></param>
        /// <param name="validator">The <see cref="AbstractValidator{TRequest}"/>.</param>
        /// <param name="eventPublisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="eventName">The optional event destintion name.</param>
        /// <param name="eventModifier">An action to enable each <see cref="EventData"/> instance to be updated prior to publish.</param>
        /// <param name="valueIsRequired">Indicates whether the value is required; will consider invalid where null.</param>
        /// <param name="maxListSize">Overrides the default (<see cref="SettingsBaseExtensions.GetMaxPublishCollSize"/>) maximum publish collection size.</param>
        /// <param name="successStatusCode">The success <see cref="HttpStatusCode"/>; defaults to <see cref="HttpStatusCode.Accepted"/>.</param>
        /// <param name="beforeEvents">A function that enables the list to be processed before the underlying event publishing logic is enacted.</param>
        /// <returns>The resulting <see cref="IActionResult"/>.</returns>
        public static async Task<IActionResult> RunPublishCollAsync<TColl, TItem, TValidator>(this IHttpTriggerExecutor executor, HttpRequest request, TValidator validator, IEventPublisher eventPublisher, string? eventName, Action<EventData, TItem>? eventModifier = null, bool valueIsRequired = true, int? maxListSize = null, HttpStatusCode successStatusCode = HttpStatusCode.Accepted, Func<TColl, Task>? beforeEvents = null)
            where TColl : IEnumerable<TItem>
            where TValidator : AbstractValidator<TColl>
            => await executor.RunPublishCollAsync<TColl, TItem>(request, eventPublisher, eventName, eventModifier, valueIsRequired, maxListSize, successStatusCode, async (list) =>
            {
                var fvr = (validator ?? throw new ArgumentNullException(nameof(validator))).Validate(list);
                fvr.ThrowValidationException();
                if (beforeEvents != null)
                    await beforeEvents(list).ConfigureAwait(false);
            }).ConfigureAwait(false);
    }
}