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
    /// Enables the <see cref="WebApiPublisher"/> arguments.
    /// </summary>
    /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
    /// <typeparam name="TEventValue">The <see cref="EventData.Value"/> <see cref="Type"/> (where different then a <see cref="Mapper"/> will be required).</typeparam>
    public interface IWebApiPublisherArgs<TValue, TEventValue>
    {
        /// <summary>
        /// Indicates whether the <typeparamref name="TValue"/> and <typeparamref name="TEventValue"/> are the same <see cref="Type"/>.
        /// </summary>
        internal bool AreSameType => typeof(TValue) == typeof(TEventValue);

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
        /// Indicates whether the <typeparamref name="TValue"/> is required.
        /// </summary>
        bool ValueIsRequired { get; }

        /// <summary>
        /// Gets or sets the optional <typeparamref name="TValue"/> validator.
        /// </summary>
        IValidator<TValue>? Validator { get; }

        /// <summary>
        /// Gets or sets the <see cref="CoreEx.OperationType"/>.
        /// </summary>
        /// <remarks>Defaults to <see cref="OperationType.Unspecified"/>.</remarks>
        OperationType OperationType { get; }

        /// <summary>
        /// Gets or sets the on before validation <typeparamref name="TValue"/> function.
        /// </summary>
        /// <remarks>Enables the likes of security, value modification, etc., before validation. The <see cref="Result"/> will allow failures and alike to be returned where applicable.</remarks>
        Func<WebApiParam<TValue>, CancellationToken, Task<Result>>? OnBeforeValidationAsync { get; }

        /// <summary>
        /// Gets or sets the after validation / on before event <typeparamref name="TValue"/> function.
        /// </summary>
        /// <remarks>Enables the likes of security, value modification, etc., after validation. The <see cref="Result"/> will allow failures and alike to be returned where applicable.</remarks>
        Func<WebApiParam<TValue>, CancellationToken, Task<Result>>? OnBeforeEventAsync { get; }

        /// <summary>
        /// Gets or sets the <see cref="EventData"/> modifier function.
        /// </summary>
        /// <remarks>Enables the corresponding <see cref="EventData"/> to be modified prior to publish beyond the <see cref="EventTemplate"/> application.</remarks>
        Action<EventData>? OnEvent { get; }

        /// <summary>
        /// Gets or sets the <typeparamref name="TValue"/> to <typeparamref name="TEventValue"/> <see cref="IMapper{TSource, TDestination}"/> override.
        /// </summary>
        /// <remarks>Where <c>null</c> the <see cref="WebApiPublisher.Mapper"/> will be used to get the corresponding <see cref="IMapper{TSource, TDestination}"/> instance to perform the underlying mapping.</remarks>
        IMapper<TValue, TEventValue>? Mapper { get; }
        
        /// <summary>
        /// Gets or sets the function to override the creation of the success <see cref="IActionResult"/>.
        /// </summary>
        /// <remarks>Defaults to a <see cref="ExtendedStatusCodeResult"/> using the defined <see cref="StatusCode"/>.</remarks>
        Func<Task<IActionResult>>? CreateSuccessResultAsync { get; }

        /// <summary>
        /// Gets or sets the function to create the <see cref="Uri"/> for the <see cref="Microsoft.AspNetCore.Http.Headers.ResponseHeaders.Location"/>.
        /// </summary>
        /// <remarks>
        /// Where enabling the likes of the <i>asynchronous request-response</i> pattern then the <see cref="EventDataBase.Id"/> represents the <see cref="WorkState.Id"/> which is the unique identifier for the work instance and is therefore required.
        /// <para>This will not be invoked automatically where the <see cref="CreateSuccessResultAsync"/> is overridden.</para></remarks>
        Func<WebApiParam<TValue>, EventData, Uri>? CreateLocation { get; }

        /// <summary>
        /// Gets or sets the function to create the <see cref="WorkStateArgs"/>.
        /// </summary>
        /// <remarks><para>The <see cref="WorkStateArgs.Id"/> and <see cref="WorkStateArgs.CorrelationId"/> will be overridden by the <see cref="EventData"/> equivalents after creation to ensure consistencey; therefore, these properties need
        /// not be set during create. The <see cref="WorkStateArgs.Key"/> will be set to the <see cref="EventDataBase.Key"/> where <c>null</c>, so also does not need to be explicitly set.</para>
        /// An <see cref="InvalidOperationException"/> will occur where this is set and the corresponding <see cref="WebApiPublisher.WorkStateOrchestrator"/> is <c>null</c>. The combination of the two enables
        /// the automatic create (<see cref="WorkStateOrchestrator.CreateAsync(WorkStateArgs, CancellationToken)"/>) of <see cref="WorkState"/> tracking to enable the likes of the <i>asynchronous request-response</i> pattern.</remarks>
        Func<WorkStateArgs>? CreateWorkStateArgs { get; }
    }
}