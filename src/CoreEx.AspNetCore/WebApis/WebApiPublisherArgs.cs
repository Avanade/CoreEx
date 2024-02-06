// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Events;
using CoreEx.Hosting.Work;
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
    /// <remarks>
    /// Initializes a new instance of the <see cref="WebApiPublisherArgs{TValue}"/> class.
    /// </remarks>
    /// <param name="eventName">The event destination name (e.g. Queue or Topic name).</param>
    public class WebApiPublisherArgs(string eventName) : IWebApiPublisherArgs<object, object>
    {
        /// <inheritdoc/>
        public string? EventName { get; set; } = eventName.ThrowIfNull(nameof(eventName));

        /// <inheritdoc/>
        public EventData? EventTemplate { get; set; }

        /// <inheritdoc/>
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.Accepted;

        /// <inheritdoc/>
        bool IWebApiPublisherArgs<object, object>.ValueIsRequired => false;

        /// <inheritdoc/>
        IValidator<object>? IWebApiPublisherArgs<object, object>.Validator => null;

        /// <inheritdoc/>
        public OperationType OperationType { get; set; } = OperationType.Unspecified;

        /// <inheritdoc/>
        public Func<WebApiParam<object>, CancellationToken, Task<Result>>? OnBeforeValidationAsync { get; set; }

        /// <inheritdoc/>
        public Func<WebApiParam<object>, CancellationToken, Task<Result>>? OnBeforeEventAsync { get; set; }

        /// <inheritdoc/>
        public Action<EventData>? OnEvent { get; set; }

        /// <inheritdoc/>
        IMapper<object, object>? IWebApiPublisherArgs<object, object>.Mapper => null;

        /// <inheritdoc/>
        public Func<Task<IActionResult>>? CreateSuccessResultAsync { get; set; }

        /// <inheritdoc/>
        public Func<WebApiParam<object>, EventData, Uri>? CreateLocation { get; set; }

        /// <inheritdoc/>
        public Func<WorkStateArgs>? CreateWorkStateArgs { get; set; }
    }
}