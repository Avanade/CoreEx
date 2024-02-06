// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
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
    /// Enables the <see cref="WebApiPublisher"/> collection-based arguments.
    /// </summary>
    /// <typeparam name="TColl">The request JSON collection <see cref="Type"/>.</typeparam>
    /// <typeparam name="TItem">The collection item <see cref="Type"/>.</typeparam>
    /// <typeparam name="TEventItem">The <typeparamref name="TItem"/>-equivalent <see cref="EventData.Value"/> <see cref="Type"/> (where different then a <see cref="Mapper"/> will be required).</typeparam>
    public interface IWebApiPublisherCollectionArgs<TColl, TItem, TEventItem> where TColl : IEnumerable<TItem>
    {
        /// <summary>
        /// Indicates whether the <typeparamref name="TItem"/> and <typeparamref name="TEventItem"/> are the same <see cref="Type"/>.
        /// </summary>
        internal bool AreSameType => typeof(TItem) == typeof(TEventItem);

        /// <summary>
        /// Gets or sets the optional event destintion name (e.g. Queue or Topic name).
        /// </summary>
        /// <remarks>Will leverage either <see cref="IEventPublisher.Publish(EventData[])"/> or <see cref="IEventPublisher.PublishNamed(string, EventData[])"/> depending on whether name is specified or not.</remarks>
        string? EventName { get; }

        /// <summary>
        /// Gets or sets the optional <see cref="EventData"/> to use as a template when instantiating the <see cref="EventData"/> for publishing.
        /// </summary>
        /// <remarks>Will use the <see cref="EventData(EventDataBase)"/> constructor to copy from the template.</remarks>
        EventData? EventTemplate { get; }

        /// <summary>
        /// Gets or sets the <see cref="HttpStatusCode"/> where successful.
        /// </summary>
        /// <remarks>Defaults to <see cref="HttpStatusCode.Accepted"/>.</remarks>
        HttpStatusCode StatusCode { get; }

        /// <summary>
        /// Gets or sets the maximum collection size.
        /// </summary>
        /// <remarks>Defaults to <see cref="SettingsBase.MaxPublishCollSize"/>.</remarks>
        int? MaxCollectionSize { get; }

        /// <summary>
        /// Gets or sets the optional validator
        /// </summary>
        IValidator<TColl>? Validator { get; }

        /// <summary>
        /// Gets or sets the <see cref="CoreEx.OperationType"/>.
        /// </summary>
        /// <remarks>Defaults to <see cref="OperationType.Unspecified"/>.</remarks>
        OperationType OperationType { get; }

        /// <summary>
        /// Gets or sets the on before validation <typeparamref name="TColl"/> modifier function.
        /// </summary>
        /// <remarks>Enables the value to be modified before validation. The <see cref="Result"/> will allow failures and alike to be returned where applicable.</remarks>
        Func<WebApiParam<TColl>, CancellationToken, Task<Result>>? OnBeforeValidationAsync { get; }

        /// <summary>
        /// Gets or sets the after validation / on before event <typeparamref name="TColl"/> modifier function.
        /// </summary>
        /// <remarks>Enables the value to be modified after validation. The <see cref="Result"/> will allow failures and alike to be returned where applicable.</remarks>
        Func<WebApiParam<TColl>, CancellationToken, Task<Result>>? OnBeforeEventAsync { get; }

        /// <summary>
        /// Gets or sets the <see cref="EventData"/> modifier function.
        /// </summary>
        /// <remarks>Enables the corresponding <see cref="EventData"/> to be modified prior to publish.</remarks>
        Action<EventData>? OnEvent { get; }

        /// <summary>
        /// Gets or sets the <typeparamref name="TItem"/> to <typeparamref name="TEventItem"/> <see cref="IMapper{TSource, TDestination}"/> override.
        /// </summary>
        /// <remarks>Where <c>null</c> the <see cref="WebApiPublisher.Mapper"/> will be used to get the corresponding <see cref="IMapper{TSource, TDestination}"/> instance to perform the underlying mapping.</remarks>
        IMapper<TItem, TEventItem>? Mapper { get; }

        /// <summary>
        /// Gets or sets the function to override the creation of the success <see cref="IActionResult"/>.
        /// </summary>
        /// <remarks>Defaults to a <see cref="ExtendedStatusCodeResult"/> using the defined <see cref="StatusCode"/>.</remarks>
        Func<Task<IActionResult>>? CreateSuccessResultAsync { get; }
    }
}