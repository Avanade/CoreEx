namespace CoreEx.AspNetCore.Abstractions;

public abstract partial class WebApi<TResult>
{
    /// <summary>
    /// Performs a <see cref="HttpMethods.Get"/> (and <see cref="HttpMethods.Head"/>) operation returning a response of <see cref="Type"/> <typeparamref name="TResponse"/>.
    /// </summary>
    /// <typeparam name="TResponse">The result <see cref="Type"/>.</typeparam>
    /// <param name="request">The <see cref="HttpRequest"/>.</param>
    /// <param name="function">The function to execute.</param>
    /// <param name="statusCode">The <see cref="HttpStatusCode"/> where result is not <see langword="null"/>.</param>
    /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <see langword="null"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <typeparamref name="TResult"/>.</returns>
    /// <remarks>This <see cref="GetAsync"/> also handles a <see cref="HttpMethods.Head"/> and responds accordingly.</remarks>
    public Task<TResult> GetAsync<TResponse>(HttpRequest request, Func<WebApiOptionsBase, CancellationToken, Task<TResponse>> function, HttpStatusCode statusCode = HttpStatusCode.OK, HttpStatusCode alternateStatusCode = HttpStatusCode.NotFound, CancellationToken cancellationToken = default)
        => GetWithResultAsync<TResponse>(request, async (ro, ct) => { var rv = await function.ThrowIfNull()(ro, ct).ConfigureAwait(false); return Result.Ok(rv); }, statusCode, alternateStatusCode, cancellationToken);

    /// <summary>
    /// Performs a <see cref="HttpMethods.Get"/> (and <see cref="HttpMethods.Head"/>) operation returning a response of <see cref="Type"/> <typeparamref name="TResponse"/>.
    /// </summary>
    /// <typeparam name="TResponse">The result <see cref="Type"/>.</typeparam>
    /// <param name="request">The <see cref="HttpRequest"/>.</param>
    /// <param name="function">The function to execute.</param>
    /// <param name="statusCode">The <see cref="HttpStatusCode"/> where result is not <see langword="null"/>.</param>
    /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <see langword="null"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <typeparamref name="TResult"/>.</returns>
    /// <remarks>This <see cref="GetAsync"/> also handles a <see cref="HttpMethods.Head"/> and responds accordingly.</remarks>
    public async Task<TResult> GetWithResultAsync<TResponse>(HttpRequest request, Func<WebApiOptionsBase, CancellationToken, Task<Result<TResponse>>> function, HttpStatusCode statusCode = HttpStatusCode.OK, HttpStatusCode alternateStatusCode = HttpStatusCode.NotFound, CancellationToken cancellationToken = default)
    {
        CheckRequest(request, [HttpMethods.Get, HttpMethods.Head]);
        function.ThrowIfNull();

        return await InvokeAsync(request, async cancellationToken =>
        {
            var ro = new WebApiResponseOptions<TResponse>(request).WithStatusCode(statusCode).WithAlternateStatusCode(alternateStatusCode);
            if (ro.IsInError(out var ror))
                return CreateResult(new WebApiResult<TResult>(request.HttpContext.Response) { Exception = ror.Error });

            var result = await _invoker.InvokeAsync(this, async (_, cancellationToken) => await function(ro, cancellationToken).ConfigureAwait(false), cancellationToken, $"{nameof(GetAsync)}::{nameof(function)}").ConfigureAwait(false);
            return CreateResult(CreateContentForValue(ro, result));
        }, cancellationToken, nameof(GetAsync)).ConfigureAwait(false);
    }
}