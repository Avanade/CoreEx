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

            var message = options.ApplyEventTransform(result.Value);
            return await PublishEventAsync(request, options, message, options.PublishedType, ct).ConfigureAwait(false);
        }, cancellationToken, nameof(PublishAsync)).ConfigureAwait(false);
    }

    /// <summary>
    /// Parses, validates and prepares a request-body message for Web API publishing.
    /// </summary>
    /// <typeparam name="TRequest">The request payload type.</typeparam>
    /// <param name="requestOptions">The options for the current request.</param>
    /// <param name="validator">The optional request validator.</param>
    /// <param name="createMessage">The function that creates the event envelope.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The prepared message or a failure.</returns>
    internal async Task<Result<EventData>> PreparePublishRequestAsync<TRequest>(WebApiOptions requestOptions, IValidator<TRequest>? validator, Func<TRequest, EventData> createMessage, CancellationToken cancellationToken)
    {
        var parsed = await GetRequestValueAsync<TRequest>(requestOptions.Request, cancellationToken).ConfigureAwait(false);
        if (parsed.IsFailure)
            return Result<EventData>.Fail(parsed.Error);

        return await PreparePublishValueAsync(requestOptions, parsed.Value, validator, createMessage, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Validates and prepares an already-deserialized request value for Web API publishing.
    /// </summary>
    /// <typeparam name="TRequest">The request payload type.</typeparam>
    /// <param name="requestOptions">The options for the current request.</param>
    /// <param name="value">The request value.</param>
    /// <param name="validator">The optional request validator.</param>
    /// <param name="createMessage">The function that creates the event envelope.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The prepared message or a failure.</returns>
    internal async Task<Result<EventData>> PreparePublishValueAsync<TRequest>(WebApiOptions requestOptions, TRequest? value, IValidator<TRequest>? validator, Func<TRequest, EventData> createMessage, CancellationToken cancellationToken)
    {
        var typedOptions = new WebApiRequestOptions<TRequest>(requestOptions.Request, value) { WebApi = this }.WithOperationType(OperationType.Create).WithStatusCode(HttpStatusCode.Accepted);
        if (typedOptions.IsInError(out var invalid))
            return Result<EventData>.Fail(invalid.Error);

        var requestValue = typedOptions.Value;
        if (validator is not null)
            (await validator.ValidateAsync(requestValue, cancellationToken).ConfigureAwait(false)).ThrowOnError();

        return Result<EventData>.Ok(createMessage(requestValue));
    }

    /// <summary>
    /// Resolves the configured publisher and dispatches one message through the shared tracking and acceptance path.
    /// </summary>
    private async Task<WebApiResult<TResult>> PublishEventAsync(HttpRequest request, WebApiPublishOptionsBase options, EventData message, Type publishedType, CancellationToken cancellationToken)
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
        var publisher = options.PublisherProvider is null ? services.GetRequiredService<IEventPublisher>() : options.PublisherProvider(services) ?? throw new InvalidOperationException("The configured publisher resolver must return a publisher.");
        if (publisher.HasBeenPublished || !publisher.IsEmpty)
            throw new InvalidOperationException("WebApi publishing requires an unused publisher with an empty queue.");

        if (trackingOptions is null)
        {
            await PublishMessageAsync(publisher, options.Destination, message, cancellationToken).ConfigureAwait(false);
            return new WebApiResult<TResult>(request.HttpContext.Response) { StatusCode = HttpStatusCode.Accepted };
        }

        return await PublishTrackedAsync(request, options, trackingOptions, publisher, message, publishedType, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates operation state before dispatch and fences ambiguous publication failures.
    /// </summary>
    private async Task<WebApiResult<TResult>> PublishTrackedAsync(HttpRequest request, WebApiPublishOptionsBase publishOptions, WebApiPublishOperationOptions trackingOptions, IEventPublisher publisher, EventData message, Type publishedType, CancellationToken cancellationToken)
    {
        var services = GetServiceProvider(request);
        var operationOptions = services.GetRequiredService<IOptions<OperationsOptions>>().Value;
        operationOptions.Validate();

        var typeName = publishedType.FullName ?? publishedType.Name;

        if (!operationOptions.Types.ContainsKey(typeName))
            throw new InvalidOperationException($"Tracked publishing requires operation type '{typeName}' to be registered with AddCoreExOperation.");

        var executionContext = ExecutionContext;
        EnsureOperationMessage(message, services.GetRequiredService<CoreEx.Hosting.IHostSettings>().DomainName, executionContext.TenantId);
        if (executionContext.User.Type == AuthenticationType.Unauthenticated)
            return new WebApiResult<TResult>(request.HttpContext.Response) { Exception = new AuthenticationException() };

        var owner = executionContext.User;
        var id = Runtime.NewId();
        var operationKey = CompositeKey.Create(id);

        var work = services.GetRequiredService<WorkOrchestrator>();
        var startTimeout = trackingOptions.StartTimeout;
        var expiry = trackingOptions.Expiry ?? work.ExpiryTimeSpan;
        if (startTimeout <= TimeSpan.Zero || startTimeout > expiry)
            throw new ArgumentOutOfRangeException(nameof(publishOptions), "The start timeout must be greater than zero and no greater than the operation expiry.");

        WorkState created;
        try
        {
            var createdResult = await work.CreateAsync(new WorkArgs(typeName, operationOptions.OperationName, operationKey)
            {
                StartTimeout = startTimeout,
                Expiry = trackingOptions.Expiry,
                User = owner
            }, cancellationToken).ConfigureAwait(false);
            created = createdResult.ThrowOnError().Value;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            Activity.Current?.SetTag("operation.create", "unavailable");
            services.GetRequiredService<ILoggerFactory>().CreateLogger("CoreEx.Operations").LogError(exception, "Creating operation tracking state failed before message dispatch.");
            ReleaseIdempotencyAttempt(request.HttpContext);
            return ServiceUnavailable(request.HttpContext.Response);
        }

        message.DomainName = created.DomainName;
        message.TenantId = NormalizeTenantId(created.TenantId);
        message.OperationId = created.Id.ThrowIfNullOrWhiteSpace(nameof(created.Id));
        message.OperationName = created.OperationName;
        message.OperationStartBy = created.StartBy ?? throw new InvalidDataException("A tracked operation has no first-start deadline.");

        try
        {
            await PublishMessageAsync(publisher, publishOptions.Destination, message, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Activity.Current?.SetTag("operation.publish", "outcome-unknown");
            services.GetRequiredService<ILoggerFactory>().CreateLogger("CoreEx.Operations").LogWarning(exception, "Message publication failed after tracked operation creation; delivery outcome is unknown.");

            WorkTransitionResult? fence = null;
            using (var reconciliation = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
            {
                try
                {
                    fence = await work.FenceAsync(operationOptions.OperationName, operationKey, new WorkError("dispatch-failed", "The operation message could not be dispatched.", true), reconciliation.Token).ConfigureAwait(false);
                }
                catch (Exception fenceException)
                {
                    services.GetRequiredService<ILoggerFactory>().CreateLogger("CoreEx.Operations").LogWarning(fenceException, "The operation dispatch fence could not be resolved.");
                }
            }

            if (fence?.Outcome == WorkTransitionOutcome.Applied)
            {
                ReleaseIdempotencyAttempt(request.HttpContext);
                return ServiceUnavailable(request.HttpContext.Response);
            }

            var observed = fence?.State;
            if (observed is not null && observed.Status != WorkStatus.Created)
                return AcceptedOperation(request, operationOptions, observed, operationOptions.Types[typeName]);

            return AcceptedOperation(request, operationOptions, created, operationOptions.Types[typeName]);
        }

        return AcceptedOperation(request, operationOptions, created, operationOptions.Types[typeName]);
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
    private WebApiResult<TResult> AcceptedOperation(HttpRequest request, OperationsOptions options, WorkState state, OperationTypeDefinition definition)
    {
        var status = Microsoft.AspNetCore.Builder.CoreExAspNetCoreExtensions.ToStatus(this, request, state, definition);
        var location = Microsoft.AspNetCore.Builder.CoreExAspNetCoreExtensions.GetOperationPath(options, status.Id);
        var operationLocation = GetLocationUri(request, location);
        var headers = new HeaderDictionary { ["Operation-Location"] = operationLocation.ToString() };
        if (!WorkStatus.Finished.HasFlag(state.Status))
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
    /// Creates a transient error response when tracked publication cannot safely proceed.
    /// </summary>
    private static WebApiResult<TResult> ServiceUnavailable(HttpResponse response) => new(response) { Exception = new TransientException(new LText("CoreEx.AspNetCore.Operations.Unavailable", "Operation publishing is temporarily unavailable.")) };

    /// <summary>
    /// Requests release of an owned idempotency attempt when the HTTP feature is present.
    /// </summary>
    private static void ReleaseIdempotencyAttempt(HttpContext context) => context.Features.Get<IIdempotencyAttemptFeature>()?.ReleaseForRetry();

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

        if (eventData.Attributes.ContainsKey("operationid"))
            throw new InvalidOperationException("The message is already associated with an operation; a tracked operation is initiated by exactly one command.");
    }

    /// <summary>
    /// Normalizes an empty tenant to <see langword="null"/> (domain-wide).
    /// </summary>
    private static string? NormalizeTenantId(string? tenantId) => string.IsNullOrEmpty(tenantId) ? null : tenantId;
}
