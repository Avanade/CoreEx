namespace CoreEx.AspNetCore.Abstractions;

public abstract partial class WebApi<TResult>
{
    /// <summary>
    /// Performs a <see cref="HttpMethods.Delete"/> operation.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequest"/>.</param>
    /// <param name="function">The function to execute.</param>
    /// <param name="statusCode">The resulting <see cref="HttpStatusCode"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <typeparamref name="TResult"/>.</returns>
    public Task<TResult> DeleteAsync(HttpRequest request, Func<WebApiOptions, CancellationToken, Task> function, HttpStatusCode statusCode = HttpStatusCode.NoContent, CancellationToken cancellationToken = default)
        => DeleteWithResultAsync(request, async (ro, ct) => { await function.ThrowIfNull()(ro, ct).ConfigureAwait(false); return Result.Success; }, statusCode, cancellationToken);

    /// <summary>
    /// Performs a <see cref="HttpMethods.Delete"/> operation.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequest"/>.</param>
    /// <param name="function">The function to execute.</param>
    /// <param name="statusCode">The resulting <see cref="HttpStatusCode"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <typeparamref name="TResult"/>.</returns>
    public async Task<TResult> DeleteWithResultAsync(HttpRequest request, Func<WebApiOptions, CancellationToken, Task<Result>> function, HttpStatusCode statusCode = HttpStatusCode.NoContent, CancellationToken cancellationToken = default)
    {
        CheckRequest(request, [HttpMethods.Delete]);
        function.ThrowIfNull();

        TResult CreateStatusResult(WebApiOptions ro) => CreateResult(new WebApiResult<TResult>(request.HttpContext.Response) { StatusCode = ro.StatusCode });

        return await InvokeAsync(request, async cancellationToken =>
        {
            var ro = new WebApiOptions(request).WithOperationType(OperationType.Delete).WithStatusCode(statusCode);
            if (ro.IsInError(out var ror))
                return CreateResult(new WebApiResult<TResult>(request.HttpContext.Response) { Exception = ror.Error });

            try
            {
                var result = await _invoker.InvokeAsync(this, async (_, cancellationToken) => await function(ro, cancellationToken).ConfigureAwait(false), cancellationToken, $"{nameof(DeleteAsync)}::{nameof(function)}").ConfigureAwait(false);

                if (result.IsFailure)
                {
                    if (ConvertNotfoundToDefaultStatusCodeOnDelete && result.IsNotFoundError)
                        return CreateStatusResult(ro);
                    else
                        return CreateResult(new WebApiResult<TResult>(request.HttpContext.Response) { Exception = result.Error });
                }
                else
                    return CreateStatusResult(ro);
            }
            catch (NotFoundException) when (ConvertNotfoundToDefaultStatusCodeOnDelete)
            {
                return CreateStatusResult(ro);
            }
        }, cancellationToken, nameof(DeleteAsync)).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs a <see cref="HttpMethods.Delete"/> operation with a response of <see cref="Type"/> <typeparamref name="TResponse"/>.
    /// </summary>
    /// <typeparam name="TResponse">The response result <see cref="Type"/>.</typeparam>
    /// <param name="request">The <see cref="HttpRequest"/>.</param>
    /// <param name="function">The function to execute.</param>
    /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <typeparamref name="TResult"/>.</returns>
    /// <remarks>The <see cref="WebApiBase.ConvertNotfoundToDefaultStatusCodeOnDelete"/> is not applicable for this method.</remarks>
    public Task<TResult> DeleteAsync<TResponse>(HttpRequest request, Func<WebApiResponseOptions<TResponse>, CancellationToken, Task<TResponse>> function, HttpStatusCode statusCode = HttpStatusCode.OK, CancellationToken cancellationToken = default)
        => DeleteWithResultAsync<TResponse>(request, async (ro, ct) => { var r = await function.ThrowIfNull()(ro, ct).ConfigureAwait(false); return Result.Ok(r); }, statusCode, cancellationToken);

    /// <summary>
    /// Performs a <see cref="HttpMethods.Delete"/> operation with a response of <see cref="Type"/> <typeparamref name="TResponse"/>.
    /// </summary>
    /// <typeparam name="TResponse">The response result <see cref="Type"/>.</typeparam>
    /// <param name="request">The <see cref="HttpRequest"/>.</param>
    /// <param name="function">The function to execute.</param>
    /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <typeparamref name="TResult"/>.</returns>
    /// <remarks>The <see cref="WebApiBase.ConvertNotfoundToDefaultStatusCodeOnDelete"/> is not applicable for this method.</remarks>
    public async Task<TResult> DeleteWithResultAsync<TResponse>(HttpRequest request, Func<WebApiResponseOptions<TResponse>, CancellationToken, Task<Result<TResponse>>> function, HttpStatusCode statusCode = HttpStatusCode.OK, CancellationToken cancellationToken = default)
    {
        CheckRequest(request, [HttpMethods.Delete]);
        function.ThrowIfNull();

        return await InvokeAsync(request, async cancellationToken =>
        {
            var ro = new WebApiResponseOptions<TResponse>(request).WithOperationType(OperationType.Delete).WithStatusCode(statusCode);

            if (ro.IsInError(out var ror))
                return CreateResult(new WebApiResult<TResult>(request.HttpContext.Response) { Exception = ror.Error });

            var fr = await _invoker.InvokeAsync(this, async (_, cancellationToken) => await function(ro, cancellationToken).ConfigureAwait(false), cancellationToken, $"{nameof(DeleteAsync)}::{nameof(function)}").ConfigureAwait(false);
            return CreateResult(CreateContentForValue(ro, fr));
        }, cancellationToken, nameof(DeleteAsync));
    }
}