namespace CoreEx.AspNetCore.Abstractions;

public abstract partial class WebApi<TResult>
{
    /// <summary>
    /// Creates and publishes a message using custom asynchronous logic without parsing a request body.
    /// </summary>
    /// <typeparam name="TMessage">The message payload type; used to identify the registered operation type when tracking is enabled.</typeparam>
    /// <param name="request">The HTTP request.</param>
    /// <param name="function">The function that creates the <see cref="EventData"/> without dispatching it.</param>
    /// <param name="options">The shared publishing and optional operation-tracking options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The resulting accepted response.</returns>
    /// <remarks>The function owns any input parsing and validation. Use <see cref="WebApiPublishOptions.CreateDefaultMessage{T}(T)"/> to create a message with the configured defaults. Publishing and operation tracking remain owned by this method; the caller must specify the correct <typeparamref name="TMessage"/> for tracking.</remarks>
    public Task<TResult> PublishAsync<TMessage>(HttpRequest request, Func<WebApiOptions, CancellationToken, Task<EventData>> function, WebApiPublishOptions options, CancellationToken cancellationToken = default)
        => PublishWithResultAsync<TMessage>(request, async (ro, ct) => Result.Ok(await function.ThrowIfNull()(ro, ct).ConfigureAwait(false)), options, cancellationToken);

    /// <summary>
    /// Creates and publishes a message using custom asynchronous Result-based logic without parsing a request body.
    /// </summary>
    /// <typeparam name="TMessage">The message payload type; used to identify the registered operation type when tracking is enabled.</typeparam>
    /// <param name="request">The HTTP request.</param>
    /// <param name="function">The function that creates the <see cref="EventData"/> or returns a failure without dispatching it.</param>
    /// <param name="options">The shared publishing and optional operation-tracking options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The resulting accepted or failure response.</returns>
    /// <remarks>The function owns any input parsing and validation. Use <see cref="WebApiPublishOptions.CreateDefaultMessage{T}(T)"/> to create a message with the configured defaults. The caller must specify the correct <typeparamref name="TMessage"/> for tracking. A failure prevents publisher resolution and operation creation.</remarks>
    public async Task<TResult> PublishWithResultAsync<TMessage>(HttpRequest request, Func<WebApiOptions, CancellationToken, Task<Result<EventData>>> function, WebApiPublishOptions options, CancellationToken cancellationToken = default)
    {
        CheckRequest(request, [HttpMethods.Post]);
        function.ThrowIfNull();
        options.ThrowIfNull();

        return await InvokeWithResultAsync(request, async ct =>
        {
            var requestOptions = new WebApiOptions(request) { WebApi = this }.WithOperationType(OperationType.Create).WithStatusCode(HttpStatusCode.Accepted);
            if (requestOptions.IsInError(out var invalid))
                return new WebApiResult<TResult>(request.HttpContext.Response) { Exception = invalid.Error };

            var result = await _invoker.InvokeAsync(this, async (_, ct) => await function(requestOptions, ct).ConfigureAwait(false), ct, $"{nameof(PublishAsync)}::{nameof(function)}").ConfigureAwait(false);
            if (result.IsFailure)
                return new WebApiResult<TResult>(request.HttpContext.Response) { Exception = result.Error };

            return await PublishEventAsync(request, options, result.Value, typeof(TMessage), ct).ConfigureAwait(false);
        }, cancellationToken, nameof(PublishAsync)).ConfigureAwait(false);
    }

    /// <summary>
    /// Validates and publishes the request body as a message, optionally tracking its asynchronous processing.
    /// </summary>
    /// <typeparam name="TRequest">The HTTP request and message payload type.</typeparam>
    /// <param name="request">The HTTP request.</param>
    /// <param name="options">The publishing options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The resulting accepted response.</returns>
    public async Task<TResult> PublishAsync<TRequest>(HttpRequest request, WebApiPublishOptions<TRequest> options, CancellationToken cancellationToken = default)
    {
        CheckRequest(request, [HttpMethods.Post]);
        options.ThrowIfNull();
        return await PublishRequestAsync(request, options, value => options.EventFactory?.Invoke(value) ?? options.CreateDefaultMessage(value), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Validates, maps and publishes the request body, optionally tracking its asynchronous processing.
    /// </summary>
    /// <typeparam name="TRequest">The HTTP request type.</typeparam>
    /// <typeparam name="TMessage">The message payload type.</typeparam>
    /// <param name="request">The HTTP request.</param>
    /// <param name="options">The publishing options and required mapper.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The resulting accepted response.</returns>
    public async Task<TResult> PublishAsync<TRequest, TMessage>(HttpRequest request, WebApiPublishOptions<TRequest, TMessage> options, CancellationToken cancellationToken = default) where TRequest : class where TMessage : class
    {
        CheckRequest(request, [HttpMethods.Post]);
        options.ThrowIfNull();
        options.Mapper.ThrowIfNull();

        return await PublishRequestAsync(request, options, value =>
        {
            var message = options.Mapper.Map(value);
            return options.MessageFactory?.Invoke(message) ?? options.CreateDefaultMessage(message);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Validates and publishes an already-deserialized request as a message, optionally tracking its asynchronous processing.
    /// </summary>
    /// <typeparam name="TRequest">The HTTP request and message payload type.</typeparam>
    /// <param name="request">The HTTP request.</param>
    /// <param name="value">The deserialized request value.</param>
    /// <param name="options">The publishing options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The resulting accepted response.</returns>
    public async Task<TResult> PublishAsync<TRequest>(HttpRequest request, TRequest value, WebApiPublishOptions<TRequest> options, CancellationToken cancellationToken = default)
    {
        CheckRequest(request, [HttpMethods.Post]);
        options.ThrowIfNull();
        return await PublishValueAsync(request, value, options, item => options.EventFactory?.Invoke(item) ?? options.CreateDefaultMessage(item), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Validates, maps and publishes an already-deserialized request, optionally tracking its asynchronous processing.
    /// </summary>
    /// <typeparam name="TRequest">The HTTP request type.</typeparam>
    /// <typeparam name="TMessage">The message payload type.</typeparam>
    /// <param name="request">The HTTP request.</param>
    /// <param name="value">The deserialized request value.</param>
    /// <param name="options">The publishing options and required mapper.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The resulting accepted response.</returns>
    public async Task<TResult> PublishAsync<TRequest, TMessage>(HttpRequest request, TRequest value, WebApiPublishOptions<TRequest, TMessage> options, CancellationToken cancellationToken = default)
        where TRequest : class where TMessage : class
    {
        CheckRequest(request, [HttpMethods.Post]);
        options.ThrowIfNull();
        options.Mapper.ThrowIfNull();
        return await PublishValueAsync(request, value, options, item =>
        {
            var message = options.Mapper.Map(item);
            return options.MessageFactory?.Invoke(message) ?? options.CreateDefaultMessage(message);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Parses the request body and publishes the resulting value within the Web API invocation.
    /// </summary>
    private async Task<TResult> PublishRequestAsync<TRequest>(HttpRequest request, WebApiPublishOptions<TRequest> options, Func<TRequest, EventData> createMessage, CancellationToken cancellationToken)
    {
        return await InvokeWithResultAsync(request, async ct =>
        {
            var parsed = await GetRequestValueAsync<TRequest>(request, ct).ConfigureAwait(false);
            if (parsed.IsFailure)
                return new WebApiResult<TResult>(request.HttpContext.Response) { Exception = parsed.Error };

            return await PublishValueResultAsync(request, parsed.Value, options, createMessage, ct).ConfigureAwait(false);
        }, cancellationToken, nameof(PublishAsync)).ConfigureAwait(false);
    }

    /// <summary>
    /// Publishes an already-deserialized value within the Web API invocation.
    /// </summary>
    private async Task<TResult> PublishValueAsync<TRequest>(HttpRequest request, TRequest value, WebApiPublishOptions<TRequest> options, Func<TRequest, EventData> createMessage, CancellationToken cancellationToken)
        => await InvokeWithResultAsync(request, ct => PublishValueResultAsync(request, value, options, createMessage, ct), cancellationToken, nameof(PublishAsync)).ConfigureAwait(false);

    /// <summary>
    /// Validates the request and dispatches the message with the configured tracking mode.
    /// </summary>
    private async Task<WebApiResult<TResult>> PublishValueResultAsync<TRequest>(HttpRequest request, TRequest? value, WebApiPublishOptions<TRequest> options, Func<TRequest, EventData> createMessage, CancellationToken cancellationToken)
    {
        var requestOptions = new WebApiRequestOptions<TRequest>(request, value) { WebApi = this }.WithOperationType(OperationType.Create).WithStatusCode(HttpStatusCode.Accepted);
        if (requestOptions.IsInError(out var invalid))
            return new WebApiResult<TResult>(request.HttpContext.Response) { Exception = invalid.Error };

        var requestValue = requestOptions.Value;
        if (options.Validator is not null)
            (await options.Validator.ValidateAsync(requestValue, cancellationToken).ConfigureAwait(false)).ThrowOnError();

        var message = createMessage(requestValue);
        return await PublishEventAsync(request, options, message, options.PublishedType, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Resolves the configured publisher and dispatches one message through the shared tracking and acceptance path.
    /// </summary>
    private async Task<WebApiResult<TResult>> PublishEventAsync(HttpRequest request, WebApiPublishOptions options, EventData message, Type publishedType, CancellationToken cancellationToken)
    {
        message.ThrowIfNull();
        var endpoint = request.HttpContext.GetEndpoint();
        var isTrackingEndpoint = endpoint?.Metadata.GetMetadata<PublishTrackingAttribute>() is not null;
        if (endpoint is not null && options.Operation is not null && !isTrackingEndpoint)
            throw new InvalidOperationException("Operation publishing options can only be used on an endpoint marked with PublishTrackingAttribute.");

        var trackingOptions = endpoint is null ? options.Operation : isTrackingEndpoint ? options.Operation ?? new WebApiPublishOperationOptions() : null;
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
    private async Task<WebApiResult<TResult>> PublishTrackedAsync(HttpRequest request, WebApiPublishOptions publishOptions, WebApiPublishOperationOptions trackingOptions, IEventPublisher publisher, EventData message, Type publishedType, CancellationToken cancellationToken)
    {
        var services = GetServiceProvider(request);
        var operationOptions = services.GetRequiredService<IOptions<OperationsOptions>>().Value;
        operationOptions.Validate();

        var hostSettings = services.GetRequiredService<IHostSettings>();
        var typeName = publishedType.FullName ?? publishedType.Name;

        if (!operationOptions.Types.ContainsKey(typeName))
            throw new InvalidOperationException($"Tracked publishing requires operation type '{typeName}' to be registered with AddCoreExOperation.");

        var executionContext = ExecutionContext;
        if (executionContext.User.Type == AuthenticationType.Unauthenticated)
            return new WebApiResult<TResult>(request.HttpContext.Response) { Exception = new AuthenticationException() };

        var owner = executionContext.User;
        var id = Runtime.NewId();
        var operationKey = Microsoft.AspNetCore.Builder.CoreExAspNetCoreExtensions.CreateOperationKey(operationOptions, hostSettings, executionContext, id);
        if (operationKey is null)
            throw new InvalidOperationException("Tracked publishing could not resolve the operation scope or tenant.");

        var work = services.GetRequiredService<WorkOrchestrator>();
        var startTimeout = trackingOptions.StartTimeout;
        var expiry = trackingOptions.Expiry ?? work.ExpiryTimeSpan;
        if (startTimeout <= TimeSpan.Zero || startTimeout > expiry)
            throw new ArgumentOutOfRangeException(nameof(publishOptions), "The start timeout must be greater than zero and no greater than the operation expiry.");

        WorkState created;
        try
        {
            var createdResult = await work.CreateAsync(new WorkArgs(typeName, operationKey)
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

        message.WithOperation(operationKey, created.StartBy ?? throw new InvalidDataException("A tracked operation has no first-start deadline."));

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
                    fence = await work.FenceAsync(operationKey, new WorkError("dispatch-failed", "The operation message could not be dispatched.", true), reconciliation.Token).ConfigureAwait(false);
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
}
