// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Events;
using CoreEx.Mapping;
using CoreEx.Results;
using CoreEx.Validation;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.AspNetCore.WebApis
{
    /// <summary>
    /// Represents the <see cref="WebApiPublisher"/> collection-based arguments; being the opportunity to further configure the standard processing.
    /// </summary>
    /// <typeparam name="TColl">The request JSON collection <see cref="Type"/>.</typeparam>
    /// <typeparam name="TItem">The collection item <see cref="Type"/>.</typeparam>
    /// <typeparam name="TEventItem">The <typeparamref name="TItem"/>-equivalent <see cref="EventData.Value"/> <see cref="Type"/> (where different then a <see cref="Mapper"/> will be required).</typeparam>
    /// <param name="validator">The optional validator.</param>
    public class WebApiPublisherCollectionArgs<TColl, TItem, TEventItem>(IValidator<TColl>? validator = null) : IWebApiPublisherCollectionArgs<TColl, TItem, TEventItem> where TColl : IEnumerable<TItem>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebApiPublisherCollectionArgs{TColl, TItem}"/> class.
        /// </summary>
        /// <param name="eventName">The event destination name (e.g. Queue or Topic name).</param>
        /// <param name="validator">The optional validator.</param>
        public WebApiPublisherCollectionArgs(string eventName, IValidator<TColl>? validator = null) : this(validator) => EventName = eventName;

        /// <inheritdoc/>
        public string? EventName { get; set; }

        /// <inheritdoc/>
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.Accepted;

        /// <inheritdoc/>
        public int? MaxCollectionSize { get; set; }

        /// <inheritdoc/>
        public IValidator<TColl>? Validator { get; set; } = validator;

        /// <inheritdoc/>
        public OperationType OperationType { get; set; } = OperationType.Unspecified;

        /// <inheritdoc/>
        public Func<WebApiParam<TColl>, CancellationToken, Task<Result>>? OnBeforeValidationAsync { get; set; }

        /// <inheritdoc/>
        public Func<WebApiParam<TColl>, CancellationToken, Task<Result>>? OnBeforeEventAsync { get; set; }

        /// <inheritdoc/>
        public Action<EventData>? OnEvent { get; set; }

        /// <inheritdoc/>
        public IMapper<TItem, TEventItem>? Mapper { get; set; }

        /// <inheritdoc/>
        public Func<IActionResult>? CreateSuccessResult { get; set; }
    }
}