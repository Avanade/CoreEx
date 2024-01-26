// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Events;
using CoreEx.Mapping;
using CoreEx.Results;
using CoreEx.Validation;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.AspNetCore.WebApis
{
    /// <summary>
    /// Represents the <see cref="WebApiPublisher"/> arguments; being the opportunity to further configure the standard processing.
    /// </summary>
    /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
    /// <typeparam name="TEventValue"><see cref="EventData.Value"/> <see cref="Type"/> (where different to the request).</typeparam>
    /// <param name="validator">The optional validator.</param>
    public class WebApiPublisherArgs<TValue, TEventValue>(IValidator<TValue>? validator = null) : IWebApiPublisherArgs<TValue, TEventValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebApiPublisherArgs{TValue}"/> class.
        /// </summary>
        /// <param name="eventName">The event destination name (e.g. Queue or Topic name).</param>
        /// <param name="validator">The optional validator.</param>
        public WebApiPublisherArgs(string eventName, IValidator<TValue>? validator = null) : this(validator) => EventName = eventName;

        /// <inheritdoc/>
        public string? EventName { get; set; } = default!;

        /// <inheritdoc/>
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.Accepted;

        /// <inheritdoc/>
        public IValidator<TValue>? Validator { get; set; } = validator;

        /// <inheritdoc/>
        public OperationType OperationType { get; set; } = OperationType.Unspecified;

        /// <inheritdoc/>
        public Func<WebApiParam<TValue>, CancellationToken, Task<Result>>? OnBeforeValidationAsync { get; set; }

        /// <inheritdoc/>
        public Func<WebApiParam<TValue>, CancellationToken, Task<Result>>? OnBeforeEventAsync { get; set; }

        /// <inheritdoc/>
        public Action<EventData>? OnEvent { get; set; }

        /// <inheritdoc/>
        public IMapper<TValue, TEventValue>? Mapper { get; set; }

        /// <inheritdoc/>
        public Func<IActionResult>? CreateSuccessResult { get; set; }
    }
}