#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace Microsoft.AspNetCore.Builder;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides endpoint-mapping extensions for asynchronous operations.
/// </summary>
public static partial class CoreExAspNetCoreExtensions
{
    /// <summary>
    /// Maps the generic status, result and cancellation endpoints.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
    /// <param name="groupConfigure">An optional action to further configure the <see cref="RouteGroupBuilder"/>; for example, adding authorization policies, rate limiting or endpoint filters.</param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/> to support fluent-style method-chaining.</returns>
    /// <remarks>Each endpoint enforces operation ownership and per-type authorization; the <paramref name="groupConfigure"/> applies additional host-level policy to the whole group.</remarks>
    public static IEndpointRouteBuilder MapCoreExOperations(this IEndpointRouteBuilder endpoints, Action<RouteGroupBuilder>? groupConfigure = null)
    {
        endpoints.ThrowIfNull();
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<OperationsOptions>>().Value;
        options.Validate();
        var group = endpoints.MapGroup(options.RoutePrefix);

        group.MapGet("/{id}", GetStatusAsync)
            .WithName("CoreExOperationStatus")
            .WithMetadata(new OperationsEndpointMetadata(OperationsEndpointKind.Status))
            .Produces<OperationStatus>()
            .Produces(StatusCodes.Status304NotModified)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapGet("/{id}/result", GetResultAsync)
            .WithName("CoreExOperationResult")
            .WithMetadata(new OperationsEndpointMetadata(OperationsEndpointKind.Result))
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapPost("/{id}:cancel", CancelAsync)
            .WithName("CoreExOperationCancel")
            .WithMetadata(new OperationsEndpointMetadata(OperationsEndpointKind.Cancel))
            .Produces<OperationStatus>(StatusCodes.Status202Accepted)
            .Produces(StatusCodes.Status405MethodNotAllowed)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        groupConfigure?.Invoke(group);
        return endpoints;
    }

    /// <summary>
    /// Returns an authorized operation status with conditional polling headers.
    /// </summary>
    /// <remarks>The <see cref="CoreEx.AspNetCore.Http.WebApi"/> provides the standard error, <c>ETag</c> and <c>If-None-Match</c> (<c>304</c>) handling; a missing or unauthorized status is returned as a <c>404</c> result.</remarks>
    private static Task<IResult> GetStatusAsync(HttpRequest request, CoreEx.AspNetCore.Http.WebApi webApi, string id, CancellationToken cancellationToken)
        => webApi.GetWithResultAsync<OperationStatus?>(request, async (_, ct) =>
        {
            var loaded = await LoadAsync(webApi, request, id, ct).ConfigureAwait(false);
            if (loaded is null)
                return Result<OperationStatus?>.NotFoundError();

            var response = request.HttpContext.Response;
            SetPrivateCache(response);
            if (!IsTerminal(loaded.State.Status))
                SetRetryAfter(response, GetRetryAfter(loaded));

            if (loaded.State.Status == WorkStatus.Completed && loaded.State.ResourceLocation is not null)
                response.Headers.Location = webApi.GetLocationUri(request, new Uri(loaded.State.ResourceLocation, UriKind.RelativeOrAbsolute)).ToString();

            return ToStatus(webApi, request, loaded.State, loaded.Definition);
        }, cancellationToken: cancellationToken);

    /// <summary>
    /// Returns the application-owned result of a successful operation.
    /// </summary>
    /// <remarks>A <see langword="null"/> result (including where the result is addressed by its resource location) results in a <c>204</c>. Missing or unauthorized operations and operations that have not succeeded are returned as failed results.</remarks>
    private static Task<IResult> GetResultAsync(HttpRequest request, CoreEx.AspNetCore.Http.WebApi webApi, string id, CancellationToken cancellationToken)
        => webApi.GetWithResultAsync<object?>(request, async (_, ct) =>
        {
            var loaded = await LoadAsync(webApi, request, id, ct).ConfigureAwait(false);
            if (loaded is null)
                return Result.NotFoundError();

            SetPrivateCache(request.HttpContext.Response);
            if (loaded.State.Status != WorkStatus.Completed)
                return Result.ConflictError("The operation has not succeeded.");

            if (loaded.State.ResourceLocation is not null || loaded.Definition.ReadResultAsync is null)
                return Result<object?>.Ok(null);

            return Result.Ok(await loaded.Definition.ReadResultAsync(webApi.ExecutionContext, loaded.State, ct).ConfigureAwait(false));
        }, alternateStatusCode: HttpStatusCode.NoContent, cancellationToken: cancellationToken);

    /// <summary>
    /// Records an authorized cooperative cancellation request.
    /// </summary>
    /// <remarks>Expected missing and conflict outcomes are returned as failed results. Where cancellation is not enabled for the operation type a <c>405</c> is returned directly as there is no equivalent <i>CoreEx</i> exception.</remarks>
    private static Task<IResult> CancelAsync(HttpRequest request, CoreEx.AspNetCore.Http.WebApi webApi, string id, CancellationToken cancellationToken)
        => webApi.PostWithResultAsync<object?>(request, async (_, ct) =>
        {
            var loaded = await LoadAsync(webApi, request, id, ct).ConfigureAwait(false);
            if (loaded is null)
                return Result.NotFoundError();

            var response = request.HttpContext.Response;
            SetPrivateCache(response);
            if (!loaded.Definition.CancellationEnabled)
            {
                response.Headers.Allow = HttpMethods.Get;
                return Result.Ok(AspHttp.Results.StatusCode(StatusCodes.Status405MethodNotAllowed));
            }

            if (IsTerminal(loaded.State.Status))
                return Result.ConflictError("The operation is already terminal.");

            WorkTransitionResult result;
            try
            {
                result = await loaded.Services.GetRequiredService<WorkOrchestrator>().CancelAsync(loaded.State.OperationName, CompositeKey.Create(loaded.State.Id), cancellationToken: ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw TrackingUnavailable(loaded.Services, exception, "Requesting operation cancellation failed.");
            }

            if (result.Outcome == WorkTransitionOutcome.NotFound)
                return Result.NotFoundError();

            if (result.Outcome is WorkTransitionOutcome.AlreadyFinished or WorkTransitionOutcome.Expired or WorkTransitionOutcome.Conflict or WorkTransitionOutcome.ConcurrencyExhausted)
                return Result.ConflictError("The operation is already terminal or changed concurrently.");

            if (result.State is null)
                return Result.TransientError(TrackingUnavailableMessage);

            var status = ToStatus(webApi, request, result.State, loaded.Definition);
            var operationUri = webApi.GetLocationUri(request, GetOperationPath(loaded.Options, status.Id)).ToString();
            response.Headers.Location = operationUri;
            response.Headers["Operation-Location"] = operationUri;
            if (!IsTerminal(result.State.Status))
                SetRetryAfter(response, GetRetryAfter(loaded with { State = result.State, ObservedOn = result.ObservedOn }));

            return Result.Ok(status);
        }, HttpStatusCode.Accepted, cancellationToken: cancellationToken);

    /// <summary>
    /// Loads an operation only when its type and owner authorize access.
    /// </summary>
    /// <remarks>The user, tenant and services are all sourced from the <see cref="WebApiBase.ExecutionContext"/>; these are expected to have been established by the <c>UseExecutionContext</c> middleware.</remarks>
    private static async Task<LoadedOperation?> LoadAsync(WebApiBase webApi, HttpRequest request, string id, CancellationToken cancellationToken)
    {
        var ec = webApi.ExecutionContext;
        var services = webApi.GetServiceProvider(request);
        var options = services.GetRequiredService<IOptions<OperationsOptions>>().Value;
        if (string.IsNullOrWhiteSpace(id))
            return null;

        WorkState? state;
        DateTimeOffset observedOn;
        try
        {
            var snapshot = await services.GetRequiredService<WorkOrchestrator>().GetSnapshotAsync(options.OperationName, CompositeKey.Create(id), cancellationToken).ConfigureAwait(false);
            state = snapshot.State;
            observedOn = snapshot.Now;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw TrackingUnavailable(services, exception, "Reading operation tracking state failed.");
        }

        if (state is null || !options.Types.TryGetValue(state.TypeName ?? string.Empty, out var definition))
            return null;

        if (ec.User.Type == AuthenticationType.Unauthenticated || state.User is null || !state.User.Equals(ec.User))
            return null;

        if (definition.AuthorizeAsync is not null && !await definition.AuthorizeAsync(ec, state, cancellationToken).ConfigureAwait(false))
            return null;

        return new LoadedOperation(services, options, state, definition, observedOn);
    }

    /// <summary>
    /// Maps internal work state to the public operation status.
    /// </summary>
    /// <param name="webApi">The configured WebApi instance used to resolve resource links.</param>
    /// <param name="request">The originating <see cref="HttpRequest"/> used to resolve resource links.</param>
    /// <param name="state">The internal work state.</param>
    /// <param name="definition">The operation type configuration.</param>
    /// <returns>The public status representation.</returns>
    internal static OperationStatus ToStatus(WebApiBase webApi, HttpRequest request, WorkState state, OperationTypeDefinition definition)
    {
        var error = state.Error is not null && state.Status is (WorkStatus.Failed or WorkStatus.Expired or WorkStatus.Canceled) ? definition.MapError?.Invoke(state.Error) ?? MapError(state.Error) : null;
        var message = state.Progress?.Message;
        if (state.Status == WorkStatus.Indeterminate && string.IsNullOrWhiteSpace(message))
            message = "The previous execution outcome is unknown; processing may be retried.";

        return new OperationStatus
        {
            Id = state.Id!,
            Status = state.Status switch
            {
                WorkStatus.Created => "not-started",
                WorkStatus.Started or WorkStatus.Indeterminate => "running",
                WorkStatus.Completed => "succeeded",
                WorkStatus.Failed or WorkStatus.Expired => "failed",
                WorkStatus.Canceled => "canceled",
                _ => "failed"
            },
            CreatedOn = state.CreatedOn,
            UpdatedOn = state.UpdatedOn ?? state.Progress?.LastUpdated ?? state.FinishedOn ?? state.StartedOn ?? state.CreatedOn,
            ExpiryOn = state.ExpiryOn,
            CancelRequested = state.CancelRequested,
            PercentComplete = state.Progress?.PercentComplete,
            Message = message,
            Error = error,
            ResultLocation = state.ResourceLocation is null ? null : webApi.GetLocationUri(request, new Uri(state.ResourceLocation, UriKind.RelativeOrAbsolute)).ToString()
        };
    }

    /// <summary>
    /// Maps known work errors to safe public error descriptions.
    /// </summary>
    private static OperationError MapError(WorkError error) => error.Code switch
    {
        WorkErrorCodes.NotStarted => new("not-started", "The operation did not start before its deadline."),
        WorkErrorCodes.OutcomeUnknown => new("outcome-unknown", "The operation ended without a known outcome."),
        WorkErrorCodes.Canceled => new("canceled", "The operation was canceled."),
        WorkErrorCodes.MaxAttempts => new("max-attempts", "The operation reached its execution-attempt limit."),
        WorkErrorCodes.ExecutionFailed => new("execution-failed", "The operation failed."),
        _ => new("operation-failed", "The operation failed.")
    };

    /// <summary>
    /// Builds the application-relative path for an operation resource.
    /// </summary>
    /// <param name="options">The operations resource configuration.</param>
    /// <param name="id">The operation identifier.</param>
    /// <param name="suffix">The optional operation resource suffix.</param>
    /// <returns>The application-relative operation resource URI.</returns>
    internal static Uri GetOperationPath(OperationsOptions options, string id, string suffix = "")
    {
        var relativePath = $"{options.RoutePrefix.TrimEnd('/')}/{Uri.EscapeDataString(id)}{suffix}";
        return new Uri(relativePath, UriKind.Relative);
    }

    /// <summary>
    /// Calculates the default polling delay from the age of an operation.
    /// </summary>
    internal static TimeSpan RetryAfter(WorkState state, DateTimeOffset now)
    {
        var age = now - state.CreatedOn;
        return age < TimeSpan.FromSeconds(10) ? TimeSpan.FromSeconds(1)
            : age < TimeSpan.FromSeconds(30) ? TimeSpan.FromSeconds(2)
            : age < TimeSpan.FromMinutes(2) ? TimeSpan.FromSeconds(5)
            : TimeSpan.FromSeconds(15);
    }

    /// <summary>
    /// Resolves and validates the configured polling delay.
    /// </summary>
    private static TimeSpan GetRetryAfter(LoadedOperation loaded)
    {
        var retryAfter = loaded.Definition.RetryAfter?.Invoke(loaded.State) ?? RetryAfter(loaded.State, loaded.ObservedOn);
        if (retryAfter <= TimeSpan.Zero)
            throw new InvalidOperationException("The operation retry delay must be greater than zero.");

        return retryAfter;
    }

    /// <summary>
    /// Determines whether the work status is terminal.
    /// </summary>
    private static bool IsTerminal(WorkStatus status) => WorkStatus.Finished.HasFlag(status);

    /// <summary>
    /// Prevents shared caching of private operation responses.
    /// </summary>
    private static void SetPrivateCache(HttpResponse response) => response.Headers.CacheControl = "private, no-store";

    /// <summary>
    /// Carries authorized state and its operation type configuration.
    /// </summary>
    private sealed record LoadedOperation(IServiceProvider Services, OperationsOptions Options, WorkState State, OperationTypeDefinition Definition, DateTimeOffset ObservedOn);

    /// <summary>
    /// The public message used where the operation tracking store is unavailable.
    /// </summary>
    private const string TrackingUnavailableMessage = "Operation tracking is temporarily unavailable.";

    /// <summary>
    /// Logs a tracking-store failure and creates the corresponding <see cref="TransientException"/> (<c>503</c>).
    /// </summary>
    private static TransientException TrackingUnavailable(IServiceProvider services, Exception exception, string logMessage)
    {
        services.GetRequiredService<ILoggerFactory>().CreateLogger("CoreEx.Operations").LogError(exception, "{Message}", logMessage);
        return new TransientException(TrackingUnavailableMessage, exception);
    }

    /// <summary>
    /// Sets the <c>Retry-After</c> header in whole seconds.
    /// </summary>
    private static void SetRetryAfter(HttpResponse response, TimeSpan retryAfter) => response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
}
