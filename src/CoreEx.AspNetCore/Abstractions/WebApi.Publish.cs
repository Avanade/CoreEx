namespace CoreEx.AspNetCore.Abstractions;

public abstract partial class WebApi<TResult>
{
    /// <summary>
    /// Validates, creates and publishes a message, optionally tracking its asynchronous processing.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <param name="options">The message preparation, publishing and optional operation-tracking options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The resulting accepted response.</returns>
    public async Task<TResult> PublishAsync(HttpRequest request, WebApiPublishOptionsBase options, CancellationToken cancellationToken = default)
    {
        CheckRequest(request, [HttpMethods.Post]);
        options.ThrowIfNull();

        return await InvokeWithResultAsync(request, async ct =>
        {
            var requestOptions = new WebApiOptions(request) { WebApi = this }.WithOperationType(OperationType.Create).WithStatusCode(HttpStatusCode.Accepted);
            if (requestOptions.IsInError(out var invalid))
                return new WebApiResult<TResult>(request.HttpContext.Response) { Exception = invalid.Error };

            var result = await _invoker.InvokeAsync(this, async (_, ct) => await options.PrepareAsync(this, requestOptions, ct).ConfigureAwait(false), ct, nameof(PublishAsync)).ConfigureAwait(false);
            if (result.IsFailure)
                return new WebApiResult<TResult>(request.HttpContext.Response) { Exception = result.Error };

            var message = options.ApplyEventTransform(result.Value.Message);
            return await PublishEventAsync(request, options, message, result.Value.Key, ct).ConfigureAwait(false);
        }, cancellationToken, nameof(PublishAsync)).ConfigureAwait(false);
    }

    /// <summary>
    /// Parses, validates and prepares a request-body message for Web API publishing.
    /// </summary>
    /// <typeparam name="TRequest">The request payload type.</typeparam>
    /// <param name="requestOptions">The options for the current request.</param>
    /// <param name="validator">The optional request validator.</param>
    /// <param name="createMessage">The function that creates the event envelope.</param>
    /// <param name="keyFactory">The optional request-aware validated-input submission-key selector.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The prepared message or a failure.</returns>
    internal async Task<Result<WebApiPreparedPublish>> PreparePublishRequestAsync<TRequest>(WebApiOptions requestOptions, IValidator<TRequest>? validator, Func<TRequest, EventData> createMessage, Func<WebApiOptions, TRequest, CompositeKey>? keyFactory, CancellationToken cancellationToken)
    {
        var parsed = await GetRequestValueAsync<TRequest>(requestOptions.Request, cancellationToken).ConfigureAwait(false);
        if (parsed.IsFailure)
            return parsed.AsResult<TRequest?, WebApiPreparedPublish>();

        return await PreparePublishValueAsync(requestOptions, parsed.Value, validator, createMessage, keyFactory, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Validates and prepares an already-deserialized request value for Web API publishing.
    /// </summary>
    /// <typeparam name="TRequest">The request payload type.</typeparam>
    /// <param name="requestOptions">The options for the current request.</param>
    /// <param name="value">The request value.</param>
    /// <param name="validator">The optional request validator.</param>
    /// <param name="createMessage">The function that creates the event envelope.</param>
    /// <param name="keyFactory">The optional request-aware validated-input submission-key selector.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The prepared message or a failure.</returns>
    internal async Task<Result<WebApiPreparedPublish>> PreparePublishValueAsync<TRequest>(WebApiOptions requestOptions, TRequest? value, IValidator<TRequest>? validator, Func<TRequest, EventData> createMessage, Func<WebApiOptions, TRequest, CompositeKey>? keyFactory, CancellationToken cancellationToken)
    {
        var typedOptions = new WebApiRequestOptions<TRequest>(requestOptions.Request, value) { WebApi = this }.WithOperationType(OperationType.Create).WithStatusCode(HttpStatusCode.Accepted);
        if (typedOptions.IsInError(out var invalid))
            return Result<WebApiPreparedPublish>.Fail(invalid.Error);

        var requestValue = typedOptions.Value;
        if (validator is not null)
            (await validator.ValidateAsync(requestValue, cancellationToken).ConfigureAwait(false)).ThrowOnError();

        var key = keyFactory?.Invoke(requestOptions, requestValue);
        return Result.Ok(new WebApiPreparedPublish(createMessage(requestValue), key));
    }

    /// <summary>
    /// Resolves the configured publisher and dispatches one message through the shared tracking and acceptance path.
    /// </summary>
    private async Task<WebApiResult<TResult>> PublishEventAsync(HttpRequest request, WebApiPublishOptionsBase options, EventData message, CompositeKey? preparedKey, CancellationToken cancellationToken)
    {
        message.ThrowIfNull();
        var endpoint = request.HttpContext.GetEndpoint();
        var isTrackingEndpoint = endpoint?.Metadata.GetMetadata<PublishTrackingAttribute>() is not null;
        if (endpoint is not null && options.Operation is not null && !isTrackingEndpoint)
            throw new InvalidOperationException("Operation publishing options can only be used on an endpoint marked with PublishTrackingAttribute.");

        var trackingOptions = endpoint is null ? options.Operation : isTrackingEndpoint ? options.Operation ?? new WebApiPublishOperationOptions() : null;
        if (trackingOptions is not null && options.Destination is not null)
            throw new InvalidOperationException("A tracked operation cannot override the publisher destination.");

        var services = GetServiceProvider(request);
        if (trackingOptions is null)
        {
            var publisher = ResolvePublisher(services, options);
            await PublishMessageAsync(publisher, options.Destination, message, cancellationToken).ConfigureAwait(false);
            return new WebApiResult<TResult>(request.HttpContext.Response) { StatusCode = HttpStatusCode.Accepted };
        }

        return await PublishTrackedAsync(request, options, trackingOptions, message, preparedKey, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates or finds an operation and enqueues its initiating command only when newly created.
    /// </summary>
    private async Task<WebApiResult<TResult>> PublishTrackedAsync(HttpRequest request, WebApiPublishOptionsBase publishOptions, WebApiPublishOperationOptions trackingOptions, EventData message, CompositeKey? preparedKey, CancellationToken cancellationToken)
    {
        var services = GetServiceProvider(request);
        var operationOptions = services.GetRequiredService<IOptions<OperationsOptions>>().Value;
        operationOptions.Validate();
        var definition = operationOptions.Resolve(publishOptions.PublishedType, trackingOptions.OperationName);
        var executionContext = ExecutionContext;
        EnsureOperationMessage(message, services.GetRequiredService<IHostSettings>().DomainName, executionContext.TenantId);
        if (!OperationsOptions.HasOwner(executionContext))
            return new WebApiResult<TResult>(request.HttpContext.Response) { Exception = new AuthenticationException() };

        // Fresh submissions must not deduplicate when a host substitutes its request identifier generator.
        var operationKey = trackingOptions.Key ?? preparedKey ?? CompositeKey.Create(Guid.NewGuid());
        operationKey.ToCanonicalString().ThrowIfNullOrWhiteSpace(nameof(trackingOptions.Key));
        var store = services.GetRequiredService<IOperationStore>();
        var unitOfWork = services.GetService<IUnitOfWork>();
        IEventPublisher? directPublisher = null;

        // Only acceptance and enqueueing belong in this transaction.
        async Task<OperationCreateResult> AcceptAsync(CancellationToken cancellationToken)
        {
            var accepted = await store.CreateAsync(definition.OperationName, operationKey, executionContext.User.UserName, null,
                new OperationCreateOptions { InactivityTimeout = trackingOptions.InactivityTimeout, TerminalRetention = trackingOptions.TerminalRetention }, cancellationToken).ConfigureAwait(false);
            if (!await OperationsOptions.IsAuthorizedAsync(services, executionContext, accepted.State, definition, cancellationToken).ConfigureAwait(false))
                throw new NotFoundException();

            if (!accepted.Created)
                return accepted;

            message.DomainName = accepted.State.DomainName;
            message.TenantId = accepted.State.TenantId;
            message.OperationId = accepted.State.OperationId.ToString("D");
            message.OperationName = accepted.State.OperationName;
            message.OperationStartBy = null;

            if (unitOfWork?.AreEventsSupported == true)
            {
                if (publishOptions.PublisherProvider is null)
                    EnqueueOperation(unitOfWork.Events, message);
                else
                {
                    var publisher = ResolvePublisher(services, publishOptions);
                    if (ReferenceEquals(publisher, unitOfWork.Events))
                        EnqueueOperation(publisher, message);
                    else
                        directPublisher = publisher;
                }
            }

            return accepted;
        }

        var result = unitOfWork is null
            ? await AcceptAsync(cancellationToken).ConfigureAwait(false)
            : await unitOfWork.TransactionAsync(AcceptAsync, cancellationToken).ConfigureAwait(false);

        if (result.Created && (unitOfWork is null || !unitOfWork.AreEventsSupported || directPublisher is not null))
            await PublishMessageAsync(directPublisher ?? ResolvePublisher(services, publishOptions), null, message, cancellationToken).ConfigureAwait(false);

        return AcceptedOperation(request, operationOptions, result.State, definition);
    }

    /// <summary>
    /// Resolves an explicitly selected or default publisher without bypassing its decorators.
    /// </summary>
    private static IEventPublisher ResolvePublisher(IServiceProvider services, WebApiPublishOptionsBase options)
    {
        var publisher = options.PublisherProvider is null ? services.GetRequiredService<IEventPublisher>() : options.PublisherProvider(services) ?? throw new InvalidOperationException("The configured publisher resolver must return a publisher.");
        if (publisher.HasBeenPublished || !publisher.IsEmpty)
            throw new InvalidOperationException("WebApi publishing requires an unused publisher with an empty queue.");

        return publisher;
    }

    /// <summary>
    /// Adds the command to the unit-of-work-owned queue, whose flush and rollback belong to that unit of work.
    /// </summary>
    private static void EnqueueOperation(IEventQueue queue, EventData message)
    {
        if (!queue.IsEmpty || queue is IEventPublisher { HasBeenPublished: true })
            throw new InvalidOperationException("Tracked acceptance requires an unused unit-of-work event queue.");

        queue.Add(message);
    }

    /// <summary>
    /// Queues and publishes one message to the configured destination.
    /// </summary>
    private static async Task PublishMessageAsync(IEventPublisher publisher, string? destination, EventData message, CancellationToken cancellationToken)
    {
        if (destination is null)
            publisher.Add(message);
        else
            publisher.Add(destination, message);

        await publisher.PublishAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates the tracked acceptance response and its polling headers.
    /// </summary>
    private WebApiResult<TResult> AcceptedOperation(HttpRequest request, OperationsOptions options, OperationState state, OperationTypeDefinition definition)
    {
        var status = Microsoft.AspNetCore.Builder.CoreExAspNetCoreExtensions.ToStatus(this, request, state, definition);
        var location = Microsoft.AspNetCore.Builder.CoreExAspNetCoreExtensions.GetOperationPath(options, status.Id);
        var operationLocation = GetLocationUri(request, location);
        var headers = new HeaderDictionary { ["Operation-Location"] = operationLocation.ToString() };
        if (!state.IsFinished)
            headers[HeaderNames.RetryAfter] = "1";

        return new WebApiResult<TResult>(request.HttpContext.Response)
        {
            Content = JsonSerializer.Serialize(status, JsonSerializerOptions),
            ContentType = DefaultJsonMediaType,
            StatusCode = HttpStatusCode.Accepted,
            Headers = new WebApiHeader { Location = location, Headers = headers }
        };
    }

    /// <summary>
    /// Validates that a message can initiate tracked work in the current domain and tenant.
    /// </summary>
    private static void EnsureOperationMessage(EventData eventData, string domainName, string? tenantId)
    {
        eventData.ThrowIfNull();
        domainName.ThrowIfNullOrWhiteSpace();

        if (eventData.MessageType != CoreEx.Events.MessageType.Command)
            throw new InvalidOperationException($"A tracked operation must be initiated by a '{nameof(CoreEx.Events.MessageType.Command)}' message; '{eventData.MessageType}' is only supported for untracked publishing.");

        if (eventData.DomainName is not null && !string.Equals(eventData.DomainName, domainName, StringComparison.Ordinal))
            throw new InvalidOperationException($"A tracked operation command must be addressed to the owning domain '{domainName}'; it targets '{eventData.DomainName}'.");

        if (!string.Equals(NormalizeTenantId(eventData.TenantId), NormalizeTenantId(tenantId), StringComparison.Ordinal))
            throw new InvalidOperationException("A tracked operation command tenant must match the operation tenant.");

        if (eventData.Attributes.ContainsKey("operationid") || eventData.Attributes.ContainsKey("operationname") || eventData.Attributes.ContainsKey("operationstartby"))
            throw new InvalidOperationException("The message is already associated with an operation; a tracked operation is initiated by exactly one command.");
    }

    /// <summary>
    /// Normalizes an empty tenant to <see langword="null"/> (domain-wide).
    /// </summary>
    private static string? NormalizeTenantId(string? tenantId) => string.IsNullOrEmpty(tenantId) ? null : tenantId;
}
