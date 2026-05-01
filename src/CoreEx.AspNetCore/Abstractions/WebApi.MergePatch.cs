namespace CoreEx.AspNetCore.Abstractions;

public abstract partial class WebApi<TResult>
{
    /// <summary>
    /// Performs a <see cref="HttpMethods.Patch"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> returning a corresponding response <typeparamref name="TValue"/>.
    /// </summary>
    /// <typeparam name="TValue">The request and response value <see cref="Type"/>.</typeparam>
    /// <param name="request">The <see cref="HttpRequest"/>.</param>
    /// <param name="get">The function to execute the <i>get</i> to retrieve the value to patch into. Where this returns a <see langword="null"/> then this will result in a <see cref="HttpStatusCode"/> of <see cref="HttpStatusCode.NotFound"/>.</param>
    /// <param name="put">The function to execute the <i>put</i> to replace (update) the patched value.</param>
    /// <param name="statusCode">The <see cref="HttpStatusCode"/> where result is not <see langword="null"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <typeparamref name="TResult"/>.</returns>
    public Task<TResult> PatchAsync<TValue>(HttpRequest request, Func<WebApiResponseOptions<TValue>, CancellationToken, Task<TValue?>> get, Func<WebApiRequestResponseOptions<TValue, TValue>, CancellationToken, Task<TValue>> put, HttpStatusCode statusCode = HttpStatusCode.OK, CancellationToken cancellationToken = default)
        => PatchWithResultAsync<TValue>(request,
            async (ro, ct) => Result.Ok(await get(ro, ct).ConfigureAwait(false)),
            async (ro, ct) => Result.Ok(await put(ro, ct).ConfigureAwait(false)),
            statusCode, cancellationToken);

    /// <summary>
    /// Performs a <see cref="HttpMethods.Patch"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> returning a corresponding response <typeparamref name="TValue"/>.
    /// </summary>
    /// <typeparam name="TValue">The request and response value <see cref="Type"/>.</typeparam>
    /// <param name="request">The <see cref="HttpRequest"/>.</param>
    /// <param name="get">The function to execute the <i>get</i> to retrieve the value to patch into. Where this returns a <see langword="null"/> then this will result in a <see cref="HttpStatusCode"/> of <see cref="HttpStatusCode.NotFound"/>.</param>
    /// <param name="put">The function to execute the <i>put</i> to replace (update) the patched value.</param>
    /// <param name="statusCode">The <see cref="HttpStatusCode"/> where result is not <see langword="null"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <typeparamref name="TResult"/>.</returns>
    public async Task<TResult> PatchWithResultAsync<TValue>(HttpRequest request, Func<WebApiResponseOptions<TValue>, CancellationToken, Task<Result<TValue?>>> get, Func<WebApiRequestResponseOptions<TValue, TValue>, CancellationToken, Task<Result<TValue>>> put, HttpStatusCode statusCode = HttpStatusCode.OK, CancellationToken cancellationToken = default)
    {
        CheckRequest(request, [HttpMethods.Patch]);
        get.ThrowIfNull();
        put.ThrowIfNull();

        TResult RequiredErrorResult() => CreateResult(new WebApiResult<TResult>(request.HttpContext.Response) { Exception = Validation.Validation.CreateRequiredValueResult<Result>().Error });

        return await InvokeAsync(request, async cancellationToken =>
        {
            // Make sure that only the supported content types are used.
            var hct = request.GetTypedHeaders()?.ContentType?.MediaType.Value;
            if (StringComparer.OrdinalIgnoreCase.Compare(hct, HttpNames.MergePatchJsonMediaTypeName) != 0)
                return CreateResult(new WebApiResult<TResult>(request.HttpContext.Response)
                {
                    StatusCode = HttpStatusCode.UnsupportedMediaType,
                    ContentType = MediaTypeNames.Text.Plain,
                    Content = $"Unsupported '{HeaderNames.ContentType}' for an HTTP {HttpMethods.Patch}; only JSON Merge Patch is supported using either: '{HttpNames.MergePatchJsonMediaTypeName}' or '{MediaTypeNames.Application.Json}'."
                });

            // Get the JSON merge content.
            if (request.ContentLength is null || request.ContentLength == 0)
                return RequiredErrorResult();

            var content = await BinaryData.FromStreamAsync(request.Body, cancellationToken).ConfigureAwait(false);
            if (content.IsEmpty)
                return RequiredErrorResult();

            // Invokes the Merge which also includes the get function execution.
            var gro = new WebApiResponseOptions<TValue>(request).WithOperationType(OperationType.Get).WithStatusCode(statusCode);
            if (gro.IsInError(out var gror))
                return CreateResult(new WebApiResult<TResult>(request.HttpContext.Response) { Exception = gror.Error });

            var mpr = await JsonMergePatch.MergeWithResultAsync(content, async ct =>
            {
                // Perform the get operation to retrieve the current value.
                return Result.Go(await _invoker.InvokeAsync(this, async (_, cancellationToken) => await get(gro, cancellationToken).ConfigureAwait(false), cancellationToken, $"{nameof(PatchAsync)}::{nameof(get)}").ConfigureAwait(false))
                    .Then(gv => 
                    {
                        if (gv is null || gv is not IReadOnlyETag etag)
                            return gv;

                        // Where there is etag support and it is null (assumes auto-generation) then generate; and finally compare etag for a match.
                        //ETag.Compare(gro.ETag, etag.ETag ?? ETag.Generate(gv, JsonSerializerOptions));
                        if (gro.ETag != (etag.ETag ?? ETag.Generate(gv, JsonSerializerOptions)))
                            return Result.ConcurrencyError();

                        return gv;
                    });
            }, cancellationToken).ConfigureAwait(false);

            // Value is not good so error out.
            if (mpr.IsFailure)
            {
                if (mpr.Error is IExtendedException)
                    return CreateResult(new WebApiResult<TResult>(request.HttpContext.Response) { Exception = mpr.Error });
                else
                    return CreateResult(new WebApiResult<TResult>(request.HttpContext.Response) { Exception = Validation.Validation.CreateInvalidValueResult<Result>(mpr.Error.Message).Error });
            }

            // Value is not found so error out.
            if (mpr.Value.Merged is null)
                return CreateResult(new WebApiResult<TResult>(request.HttpContext.Response) { Exception = new NotFoundException() });

            // No change so return the get value as-is.
            if (!mpr.Value.HasChanges)
                return CreateResult(CreateContentForValue(gro, mpr.Value.Merged));

            // Finish by performing the put operation with the changed value.
            var pro = new WebApiRequestResponseOptions<TValue, TValue>(gro, mpr.Value.Merged).WithOperationType(OperationType.Update);
            var value = await _invoker.InvokeAsync(this, async (_, cancellationToken) => await put(pro, cancellationToken).ConfigureAwait(false), cancellationToken, $"{nameof(PatchAsync)}::{nameof(put)}").ConfigureAwait(false);
            return CreateResult(CreateContentForValue(pro, value));
        }, cancellationToken, nameof(PatchAsync)).ConfigureAwait(false);
    }
}