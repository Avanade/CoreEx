namespace CoreEx.AspNetCore.Abstractions;

public abstract partial class WebApi<TResult>
{
    /// <summary>
    /// Performs a <see cref="HttpMethods.Patch"/> operation with no request content or response value.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequest"/>.</param>
    /// <param name="function">The function to execute.</param>
    /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <typeparamref name="TResult"/>.</returns>
    public Task<TResult> PatchAsync(HttpRequest request, Func<WebApiOptions, CancellationToken, Task> function, HttpStatusCode statusCode = HttpStatusCode.NoContent, CancellationToken cancellationToken = default)
        => PatchWithResultAsync(request, async (ro, ct) => { await function.ThrowIfNull()(ro, ct).ConfigureAwait(false); return Result.Success; }, statusCode, cancellationToken);

    /// <summary>
    /// Performs a <see cref="HttpMethods.Patch"/> operation with no request content or response value.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequest"/>.</param>
    /// <param name="function">The function to execute.</param>
    /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <typeparamref name="TResult"/>.</returns>
    public async Task<TResult> PatchWithResultAsync(HttpRequest request, Func<WebApiOptions, CancellationToken, Task<Result>> function, HttpStatusCode statusCode = HttpStatusCode.NoContent, CancellationToken cancellationToken = default)
    {
        CheckRequest(request, [HttpMethods.Patch]);
        function.ThrowIfNull();

        return await InvokeAsync(request, async cancellationToken =>
        {
            var ro = new WebApiOptions(request).WithOperationType(OperationType.Create).WithStatusCode(statusCode);
            if (ro.IsInError(out var ror))
                return CreateResult(new WebApiResult<TResult>(request.HttpContext.Response) { Exception = ror.Error });

            var fr = await _invoker.InvokeAsync(this, async (_, cancellationToken) => await function(ro, cancellationToken).ConfigureAwait(false), cancellationToken, $"{nameof(HttpMethods.Patch)}Async::{nameof(function)}").ConfigureAwait(false);
            if (fr.IsSuccess)
                return CreateResult(new WebApiResult<TResult>(request.HttpContext.Response) { StatusCode = ro.StatusCode });
            else
                return CreateResult(new WebApiResult<TResult>(request.HttpContext.Response) { Exception = fr.Error });
        }, cancellationToken, nameof(PatchAsync)).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs a <see cref="HttpMethods.Patch"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TRequest"/> and no corresponding response value.
    /// </summary>
    /// <typeparam name="TRequest">The request JSON content value <see cref="Type"/>.</typeparam>
    /// <param name="request">The <see cref="HttpRequest"/>.</param>
    /// <param name="function">The function to execute.</param>
    /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <typeparamref name="TResult"/>.</returns>
    public Task<TResult> PatchAsync<TRequest>(HttpRequest request, Func<WebApiRequestOptions<TRequest>, CancellationToken, Task> function, HttpStatusCode statusCode = HttpStatusCode.NoContent, CancellationToken cancellationToken = default)
        => PatchWithResultAsync<TRequest>(request, async (ro, ct) => { await function.ThrowIfNull()(ro, ct).ConfigureAwait(false); return Result.Success; }, statusCode, cancellationToken);

    /// <summary>
    /// Performs a <see cref="HttpMethods.Patch"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TRequest"/> and no corresponding response value.
    /// </summary>
    /// <typeparam name="TRequest">The request JSON content value <see cref="Type"/>.</typeparam>
    /// <param name="request">The <see cref="HttpRequest"/>.</param>
    /// <param name="function">The function to execute.</param>
    /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <typeparamref name="TResult"/>.</returns>
    public async Task<TResult> PatchWithResultAsync<TRequest>(HttpRequest request, Func<WebApiRequestOptions<TRequest>, CancellationToken, Task<Result>> function, HttpStatusCode statusCode = HttpStatusCode.NoContent, CancellationToken cancellationToken = default)
    {
        CheckRequest(request, [HttpMethods.Patch]);
        function.ThrowIfNull();

        return await InvokeAsync(request, async cancellationToken =>
        {
            var vr = await GetRequestValueAsync<TRequest>(request, cancellationToken).ConfigureAwait(false);
            if (vr.IsFailure)
                return CreateResult(new WebApiResult<TResult>(request.HttpContext.Response) { Exception = vr.Error });

            var ro = new WebApiRequestOptions<TRequest>(request, vr.Value).WithOperationType(OperationType.Create).WithStatusCode(statusCode);
            if (ro.IsInError(out var ror))
                return CreateResult(new WebApiResult<TResult>(request.HttpContext.Response) { Exception = ror.Error });

            var fr = await _invoker.InvokeAsync(this, async (_, cancellationToken) => await function(ro, cancellationToken).ConfigureAwait(false), cancellationToken, $"{nameof(HttpMethods.Patch)}Async::{nameof(function)}").ConfigureAwait(false);
            if (fr.IsSuccess)
                return CreateResult(new WebApiResult<TResult>(request.HttpContext.Response) { StatusCode = ro.StatusCode });
            else
                return CreateResult(new WebApiResult<TResult>(request.HttpContext.Response) { Exception = fr.Error });
        }, cancellationToken, nameof(PatchAsync)).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs a <see cref="HttpMethods.Patch"/> operation with the specified request <paramref name="value"/> and no corresponding response value.
    /// </summary>
    /// <typeparam name="TRequest">The request JSON content value <see cref="Type"/>.</typeparam>
    /// <param name="request">The <see cref="HttpRequest"/>.</param>
    /// <param name="value">The value (already deserialized).</param>
    /// <param name="function">The function to execute.</param>
    /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <typeparamref name="TResult"/>.</returns>
    public Task<TResult> PatchAsync<TRequest>(HttpRequest request, TRequest value, Func<WebApiRequestOptions<TRequest>, CancellationToken, Task> function, HttpStatusCode statusCode = HttpStatusCode.NoContent, CancellationToken cancellationToken = default)
        => PatchWithResultAsync(request, value, async (ro, ct) => { await function.ThrowIfNull()(ro, ct).ConfigureAwait(false); return Result.Success; }, statusCode, cancellationToken);

    /// <summary>
    /// Performs a <see cref="HttpMethods.Patch"/> operation with the specified request <paramref name="value"/> and no corresponding response value.
    /// </summary>
    /// <typeparam name="TRequest">The request JSON content value <see cref="Type"/>.</typeparam>
    /// <param name="request">The <see cref="HttpRequest"/>.</param>
    /// <param name="value">The value (already deserialized).</param>
    /// <param name="function">The function to execute.</param>
    /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <typeparamref name="TResult"/>.</returns>
    public async Task<TResult> PatchWithResultAsync<TRequest>(HttpRequest request, TRequest value, Func<WebApiRequestOptions<TRequest>, CancellationToken, Task<Result>> function, HttpStatusCode statusCode = HttpStatusCode.NoContent, CancellationToken cancellationToken = default)
    {
        CheckRequest(request, [HttpMethods.Patch]);
        function.ThrowIfNull();

        return await InvokeAsync(request, async cancellationToken =>
        {
            var ro = new WebApiRequestOptions<TRequest>(request, value).WithOperationType(OperationType.Create).WithStatusCode(statusCode);
            if (ro.IsInError(out var ror))
                return CreateResult(new WebApiResult<TResult>(request.HttpContext.Response) { Exception = ror.Error });

            var fr = await _invoker.InvokeAsync(this, async (_, cancellationToken) => await function(ro, cancellationToken).ConfigureAwait(false), cancellationToken, $"{nameof(HttpMethods.Patch)}Async::{nameof(function)}").ConfigureAwait(false);
            if (fr.IsSuccess)
                return CreateResult(new WebApiResult<TResult>(request.HttpContext.Response) { StatusCode = ro.StatusCode });
            else
                return CreateResult(new WebApiResult<TResult>(request.HttpContext.Response) { Exception = fr.Error });
        }, cancellationToken, nameof(PatchAsync)).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs a <see cref="HttpMethods.Patch"/> operation with no request body and a response of <see cref="Type"/> <typeparamref name="TResponse"/>.
    /// </summary>
    /// <typeparam name="TResponse">The response result <see cref="Type"/>.</typeparam>
    /// <param name="request">The <see cref="HttpRequest"/>.</param>
    /// <param name="function">The function to execute.</param>
    /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
    /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <see langword="null"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <typeparamref name="TResult"/>.</returns>
    public Task<TResult> PatchAsync<TResponse>(HttpRequest request, Func<WebApiResponseOptions<TResponse>, CancellationToken, Task<TResponse>> function, HttpStatusCode statusCode = HttpStatusCode.OK,
        HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent, CancellationToken cancellationToken = default)
        => PatchWithResultAsync<TResponse>(request, async (ro, ct) => { var r = await function.ThrowIfNull()(ro, ct).ConfigureAwait(false); return Result.Ok(r); }, statusCode, alternateStatusCode, cancellationToken);

    /// <summary>
    /// Performs a <see cref="HttpMethods.Patch"/> operation with no request body and a response of <see cref="Type"/> <typeparamref name="TResponse"/>.
    /// </summary>
    /// <typeparam name="TResponse">The response result <see cref="Type"/>.</typeparam>
    /// <param name="request">The <see cref="HttpRequest"/>.</param>
    /// <param name="function">The function to execute.</param>
    /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
    /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <see langword="null"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <typeparamref name="TResult"/>.</returns>
    public async Task<TResult> PatchWithResultAsync<TResponse>(HttpRequest request, Func<WebApiResponseOptions<TResponse>, CancellationToken, Task<Result<TResponse>>> function, HttpStatusCode statusCode = HttpStatusCode.OK,
        HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent, CancellationToken cancellationToken = default)
    {
        CheckRequest(request, [HttpMethods.Patch]);
        function.ThrowIfNull();

        return await InvokeAsync(request, async cancellationToken =>
        {
            var ro = new WebApiResponseOptions<TResponse>(request).WithOperationType(OperationType.Create).WithStatusCode(statusCode).WithAlternateStatusCode(alternateStatusCode);
            if (ro.IsInError(out var ror))
                return CreateResult(new WebApiResult<TResult>(request.HttpContext.Response) { Exception = ror.Error });

            var fr = await _invoker.InvokeAsync(this, async (_, cancellationToken) => await function(ro, cancellationToken).ConfigureAwait(false), cancellationToken, $"{nameof(HttpMethods.Patch)}Async::{nameof(function)}").ConfigureAwait(false);
            return CreateResult(CreateContentForValue(ro, fr));
        }, cancellationToken, nameof(PatchAsync)).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs a <see cref="HttpMethods.Patch"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TRequest"/> and a response of <see cref="Type"/> <typeparamref name="TResponse"/>.
    /// </summary>
    /// <typeparam name="TRequest">The request JSON content value <see cref="Type"/>.</typeparam>
    /// <typeparam name="TResponse">The response result <see cref="Type"/>.</typeparam>
    /// <param name="request">The <see cref="HttpRequest"/>.</param>
    /// <param name="function">The function to execute.</param>
    /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
    /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <see langword="null"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <typeparamref name="TResult"/>.</returns>
    public Task<TResult> PatchAsync<TRequest, TResponse>(HttpRequest request, Func<WebApiRequestResponseOptions<TRequest, TResponse>, CancellationToken, Task<TResponse>> function, HttpStatusCode statusCode = HttpStatusCode.Created,
        HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent, CancellationToken cancellationToken = default)
        => PatchWithResultAsync<TRequest, TResponse>(request, async (ro, ct) => { var r = await function.ThrowIfNull()(ro, ct).ConfigureAwait(false); return Result.Ok(r); }, statusCode, alternateStatusCode, cancellationToken);

    /// <summary>
    /// Performs a <see cref="HttpMethods.Patch"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TRequest"/> and a response of <see cref="Type"/> <typeparamref name="TResponse"/>.
    /// </summary>
    /// <typeparam name="TRequest">The request JSON content value <see cref="Type"/>.</typeparam>
    /// <typeparam name="TResponse">The response result <see cref="Type"/>.</typeparam>
    /// <param name="request">The <see cref="HttpRequest"/>.</param>
    /// <param name="function">The function to execute.</param>
    /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
    /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <see langword="null"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <typeparamref name="TResult"/>.</returns>
    public async Task<TResult> PatchWithResultAsync<TRequest, TResponse>(HttpRequest request, Func<WebApiRequestResponseOptions<TRequest, TResponse>, CancellationToken, Task<Result<TResponse>>> function, HttpStatusCode statusCode = HttpStatusCode.Created,
        HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent, CancellationToken cancellationToken = default)
    {
        CheckRequest(request, [HttpMethods.Patch]);
        function.ThrowIfNull();

        return await InvokeAsync(request, async cancellationToken =>
        {
            var vr = await GetRequestValueAsync<TRequest>(request, cancellationToken).ConfigureAwait(false);
            if (vr.IsFailure)
                return CreateResult(new WebApiResult<TResult>(request.HttpContext.Response) { Exception = vr.Error });

            var ro = new WebApiRequestResponseOptions<TRequest, TResponse>(request, vr.Value).WithOperationType(OperationType.Create).WithStatusCode(statusCode).WithAlternateStatusCode(alternateStatusCode);
            if (ro.IsInError(out var ror))
                return CreateResult(new WebApiResult<TResult>(request.HttpContext.Response) { Exception = ror.Error });

            var fr = await _invoker.InvokeAsync(this, async (_, cancellationToken) => await function(ro, cancellationToken).ConfigureAwait(false), cancellationToken, $"{nameof(HttpMethods.Patch)}Async::{nameof(function)}").ConfigureAwait(false);
            return CreateResult(CreateContentForValue(ro, fr));
        }, cancellationToken, nameof(PatchAsync)).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs a <see cref="HttpMethods.Patch"/> operation with the specified request <paramref name="value"/> and a response of <see cref="Type"/> <typeparamref name="TResponse"/>.
    /// </summary>
    /// <typeparam name="TRequest">The request JSON content value <see cref="Type"/>.</typeparam>
    /// <typeparam name="TResponse">The response result <see cref="Type"/>.</typeparam>
    /// <param name="request">The <see cref="HttpRequest"/>.</param>
    /// <param name="value">The value (already deserialized).</param>
    /// <param name="function">The function to execute.</param>
    /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
    /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <see langword="null"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <typeparamref name="TResult"/>.</returns>
    public Task<TResult> PatchAsync<TRequest, TResponse>(HttpRequest request, TRequest value, Func<WebApiRequestResponseOptions<TRequest, TResponse>, CancellationToken, Task<TResponse>> function, HttpStatusCode statusCode = HttpStatusCode.Created,
        HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent, CancellationToken cancellationToken = default)
        => PatchWithResultAsync<TRequest, TResponse>(request, value, async (ro, ct) => { var r = await function.ThrowIfNull()(ro, ct).ConfigureAwait(false); return Result.Ok(r); }, statusCode, alternateStatusCode, cancellationToken);

    /// <summary>
    /// Performs a <see cref="HttpMethods.Patch"/> operation with the specified request <paramref name="value"/> and a response of <see cref="Type"/> <typeparamref name="TResponse"/>.
    /// </summary>
    /// <typeparam name="TRequest">The request JSON content value <see cref="Type"/>.</typeparam>
    /// <typeparam name="TResponse">The response result <see cref="Type"/>.</typeparam>
    /// <param name="request">The <see cref="HttpRequest"/>.</param>
    /// <param name="value">The value (already deserialized).</param>
    /// <param name="function">The function to execute.</param>
    /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
    /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <see langword="null"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <typeparamref name="TResult"/>.</returns>
    public async Task<TResult> PatchWithResultAsync<TRequest, TResponse>(HttpRequest request, TRequest value, Func<WebApiRequestResponseOptions<TRequest, TResponse>, CancellationToken, Task<Result<TResponse>>> function, HttpStatusCode statusCode = HttpStatusCode.Created,
        HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent, CancellationToken cancellationToken = default)
    {
        CheckRequest(request, [HttpMethods.Patch]);
        function.ThrowIfNull();

        return await InvokeAsync(request, async cancellationToken =>
        {
            var ro = new WebApiRequestResponseOptions<TRequest, TResponse>(request, value).WithOperationType(OperationType.Create).WithStatusCode(statusCode).WithAlternateStatusCode(alternateStatusCode);
            if (ro.IsInError(out var ror))
                return CreateResult(new WebApiResult<TResult>(request.HttpContext.Response) { Exception = ror.Error });

            var fr = await _invoker.InvokeAsync(this, async (_, cancellationToken) => await function(ro, cancellationToken).ConfigureAwait(false), cancellationToken, $"{nameof(HttpMethods.Patch)}Async::{nameof(function)}").ConfigureAwait(false);
            return CreateResult(CreateContentForValue(ro, fr));
        }, cancellationToken, nameof(PatchAsync)).ConfigureAwait(false);
    }
}