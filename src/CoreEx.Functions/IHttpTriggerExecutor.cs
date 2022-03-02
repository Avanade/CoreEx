// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Events;
using CoreEx.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace CoreEx.Functions
{
    /// <summary>
    /// Defines the <see cref="HttpTriggerAttribute"/> executor.
    /// </summary>
    public interface IHttpTriggerExecutor : IExecutor
    {
        /// <summary>
        /// Gets the <see cref="IJsonSerializer"/>.
        /// </summary>
        IJsonSerializer JsonSerializer { get; }

        /// <summary>
        /// Encapsulates the execution of an <see cref="HttpRequest"/> <paramref name="function"/> returning a corresponding <see cref="IActionResult"/>.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function logic to invoke.</param>
        /// <returns>The resulting <see cref="IActionResult"/>.</returns>
        Task<IActionResult> RunAsync(HttpRequest request, Func<Task<IActionResult>> function);

        /// <summary>
        /// Encapsulates the execution of an <see cref="HttpRequest"/> <paramref name="function"/> including converting the HTTP JSON <see cref="HttpRequest.Body"/> to <see cref="Type"/> <typeparamref name="TRequest"/> returning the <paramref name="successStatusCode"/>.
        /// </summary>
        /// <typeparam name="TRequest">The request value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function logic to invoke that is passed the converted HTTP JSON <see cref="HttpRequest.Body"/> for use.</param>
        /// <param name="valueIsRequired">Indicates whether the value is required; will consider invalid where null.</param>
        /// <param name="successStatusCode">The success <see cref="HttpStatusCode"/>; defaults to <see cref="HttpStatusCode.NoContent"/>.</param>
        /// <returns>The resulting <see cref="IActionResult"/>.</returns>
        Task<IActionResult> RunAsync<TRequest>(HttpRequest request, Func<TRequest, Task> function, bool valueIsRequired = true, HttpStatusCode successStatusCode = HttpStatusCode.NoContent);

        /// <summary>
        /// Encapsulates the execution of an <see cref="HttpRequest"/> <paramref name="function"/> returning the JSON-serialized <typeparamref name="TResult"/> value and <paramref name="successStatusCode"/>. 
        /// </summary>
        /// <typeparam name="TResult">The result value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function logic to invoke that is passed the converted HTTP JSON <see cref="HttpRequest.Body"/> for use.</param>
        /// <param name="successStatusCode">The success <see cref="HttpStatusCode"/>; defaults to <see cref="HttpStatusCode.OK"/>.</param>
        /// <returns>The resulting <see cref="IActionResult"/>.</returns>
        Task<IActionResult> RunWithResultAsync<TResult>(HttpRequest request, Func<Task<TResult>> function, HttpStatusCode successStatusCode = HttpStatusCode.OK);

        /// <summary>
        /// Encapsulates the execution of an <see cref="HttpRequest"/> <paramref name="function"/> including converting the HTTP JSON <see cref="HttpRequest.Body"/> to <see cref="Type"/> <typeparamref name="TRequest"/> returning the JSON-serialized <typeparamref name="TResult"/> value and <paramref name="successStatusCode"/>. 
        /// </summary>
        /// <typeparam name="TRequest">The request value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResult">The result value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function logic to invoke that is passed the converted HTTP JSON <see cref="HttpRequest.Body"/> for use.</param>
        /// <param name="valueIsRequired">Indicates whether the value is required; will consider invalid where null.</param>
        /// <param name="successStatusCode">The success <see cref="HttpStatusCode"/>; defaults to <see cref="HttpStatusCode.OK"/>.</param>
        /// <returns>The resulting <see cref="IActionResult"/>.</returns>
        Task<IActionResult> RunWithResultAsync<TRequest, TResult>(HttpRequest request, Func<TRequest, Task<TResult>> function, bool valueIsRequired = true, HttpStatusCode successStatusCode = HttpStatusCode.OK);

        /// <summary>
        /// Encapsulates the execution of an <see cref="HttpRequest"/> that contains an item to be published (<paramref name="eventPublisher"/>) returning an <see cref="AcceptedResult"/> where successful.
        /// </summary>
        /// <typeparam name="TRequest">The request value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/></param>
        /// <param name="eventPublisher">The <see cref="IEventPublisherBase"/>.</param>
        /// <param name="eventName">The optional event destintion name.</param>
        /// <param name="eventModifier">An action to enable the <see cref="EventData"/> instance to be updated prior to publish.</param>
        /// <param name="valueIsRequired">Indicates whether the value is required; will consider invalid where null.</param>
        /// <param name="beforeEvent">A function that enables the item to be processed before the underlying event publishing logic is enacted.</param>
        /// <param name="successStatusCode">The success <see cref="HttpStatusCode"/>; defaults to <see cref="HttpStatusCode.Accepted"/>.</param>
        /// <returns>The resulting <see cref="IActionResult"/>.</returns>
        Task<IActionResult> RunPublishAsync<TRequest>(HttpRequest request, IEventPublisherBase eventPublisher, string? eventName, Action<EventData, TRequest>? eventModifier = null, bool valueIsRequired = true, HttpStatusCode successStatusCode = HttpStatusCode.Accepted, Func<TRequest, Task>? beforeEvent = null);

        /// <summary>
        /// Encapsulates the execution of an <see cref="HttpRequest"/> that contains a collection of items to be published (<paramref name="eventPublisher"/>) returning an <see cref="AcceptedResult"/> where successful.
        /// </summary>
        /// <typeparam name="TColl">The batch list <see cref="Type"/>.</typeparam>
        /// <typeparam name="TItem">The list item <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/></param>
        /// <param name="eventPublisher">The <see cref="IEventPublisherBase"/>.</param>
        /// <param name="eventName">The optional event destintion name.</param>
        /// <param name="eventModifier">An action to enable each <see cref="EventData"/> instance to be updated prior to publish.</param>
        /// <param name="valueIsRequired">Indicates whether the value is required; will consider invalid where null.</param>
        /// <param name="maxListSize">Overrides the default (<see cref="SettingsBaseExtensions.GetMaxPublishCollSize"/>) maximum publish collection size.</param>
        /// <param name="beforeEvents">A function that enables the list to be processed before the underlying event publishing logic is enacted.</param>
        /// <param name="successStatusCode">The success <see cref="HttpStatusCode"/>; defaults to <see cref="HttpStatusCode.Accepted"/>.</param>
        /// <returns>The resulting <see cref="IActionResult"/>.</returns>
        Task<IActionResult> RunPublishCollAsync<TColl, TItem>(HttpRequest request, IEventPublisherBase eventPublisher, string? eventName, Action<EventData, TItem>? eventModifier = null, bool valueIsRequired = true, int? maxListSize = null, HttpStatusCode successStatusCode = HttpStatusCode.Accepted, Func<TColl, Task>? beforeEvents = null)
            where TColl : IEnumerable<TItem>;
    }
}