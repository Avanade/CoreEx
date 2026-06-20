using CoreEx.AspNetCore.Mvc;

namespace CoreEx.AspNetCore.Idempotency;

/// <summary>
/// Provides an <see cref="IIdempotencyProvider"/> implementation that uses an <see cref="IHybridCache"/> to store and retrieve idempotent request/response data.
/// </summary>
/// <param name="cache">The <see cref="IHybridCache"/>.</param>
/// <remarks>This implementation uses the <see cref="HttpNames.IdempotencyKeyHeaderName"/> header to identify idempotent requests and store/retrieve the associated response data in/from the <see cref="IHybridCache"/>.
/// <para>This allows for efficient handling of repeated requests with the same idempotency key, ensuring that only one request is processed and the result is cached for subsequent requests.</para></remarks>
public class HybridCacheIdempotencyProvider(IHybridCache cache) : IIdempotencyProvider
{
    private static readonly TimeSpan _defaultExpiration = TimeSpan.FromHours(8);

    private readonly IdempotencyProviderInvoker _invoker = IdempotencyProviderInvoker.Default;
    private readonly IHybridCache _cache = cache.ThrowIfNull();

    /// <summary>
    /// Gets or sets the maximum cached response body size in bytes.
    /// </summary>
    /// <remarks>The default is 512 * 1024 (512 KB).</remarks>
    public int MaxCachedResponseBodySize { get; set => field = value.ThrowWhen(value => value <= 0); } = 512 * 1024;

    /// <summary>
    /// Gets the list of HTTP request headers to include in the idempotency key fingerprint (see <see cref="IdempotencyKey.Fingerprint"/>).
    /// </summary>
    /// <remarks>Should be a <i>canonical</i> list of request headers to include in fingerprinting; being important, and non-varying, to avoid errant request equality checking.</remarks>
    public List<string> HttpRequestHeadersToIncludeInFingerprint { get; } = ["Content-Type"];

    /// <summary>
    /// Gets or sets the <see cref="HybridCacheEntryOptions"/> to use when storing idempotency key entries.
    /// </summary>
    public HybridCacheEntryOptions? CacheEntryOptions { get; set; }

    /// <inheritdoc/>
    public async Task OnInvokeAsync(IdempotencyKeyAttribute attribute, HttpContext context, RequestDelegate next)
    {
        IdempotencyKey? cached = null;
        string? idempotencyKey = null;
        string? idempotencyCacheKey = null;
        HybridCacheEntryOptions? cacheEntryOptions = CacheEntryOptions;

        // Pre-processing - check for existing cached response and replay where applicable.
        var replayedResponse = await _invoker.InvokeAsync(this, async tracer =>
        {
            if (context.Request.Headers.TryGetValue(HttpNames.IdempotencyKeyHeaderName, out var strings))
            {
                // Get the key and ensure valid.
                idempotencyKey = strings.FirstOrDefault();
                idempotencyCacheKey = $"Idempotency:{idempotencyKey}";
                tracer.Activity?.AddTag("idempotency.key", idempotencyKey);

                if (!IdempotencyKey.IsIdempotencyKeyValid(idempotencyKey, out var exception))
                {
                    tracer.Activity?.AddTag("idempotency.result", "key-is-invalid");
                    throw exception;
                }

                // Check the cache for existing entry and ensure valid for this request.
                cacheEntryOptions ??= HybridCacheEntryOptions.CreateFor<IdempotencyKey>(_defaultExpiration, _defaultExpiration, CacheStrategy.Hybrid);
                cacheEntryOptions.WithTags(HttpNames.IdempotencyKeyHeaderName);

                var initial = await IdempotencyKey.CreateFromHttpRequestAsync(context, HttpRequestHeadersToIncludeInFingerprint).ConfigureAwait(false);
                cached = await _cache.GetOrCreateByKeyAsync(idempotencyCacheKey, _ => Task.FromResult(initial), cacheEntryOptions).ConfigureAwait(false);

                if (initial.Fingerprint != cached.Fingerprint)
                {
                    tracer.Activity?.AddTag("idempotency.result", "key-used-for-different-request");
                    throw IdempotencyKey.CreateUsedForDifferentRequestException();
                }

                // Where in-progress and a different request then throw the relevant concurrency exception.
                if (cached.Status == IdempotencyStatus.InProgress)
                {
                    if (!ReferenceEquals(initial, cached))
                    {
                        tracer.Activity?.AddTag("idempotency.result", "key-used-for-different-request");
                        throw IdempotencyKey.CreateInProgressException();
                    }
                }
                else if (cached.Status == IdempotencyStatus.CompletedTooLargeToReplay)
                {
                    if (tracer.Logger?.IsEnabled(LogLevel.Warning) ?? false)
                        tracer.Logger.LogWarning("Idempotent request with key '{IdempotencyKey}' cannot be replayed as the original response was too large to cache.", idempotencyKey);

                    tracer.Activity?.AddTag("idempotency.result", "original-response-too-large");
                    throw IdempotencyKey.CreateResponseTooLargeException();
                }
                else
                {
                    if (tracer.Logger?.IsEnabled(LogLevel.Information) ?? false)
                        tracer.Logger.LogInformation("Idempotent request with key '{IdempotencyKey}' has resulted in the response being replayed from the cache.", idempotencyKey);

                    // Write the cached response to the context and return, done, boom!
                    tracer.Activity?.AddTag("idempotency.result", "used-cached-response");
                    await cached.WriteToHttpResponseAsync(context).ConfigureAwait(false);

                    return true;
                }
            }
            else if (attribute.IsRequired)
            {
                tracer.Activity?.AddTag("idempotency.result", "key-required");
                throw IdempotencyKey.CreateIdempotencyKeyRequiredException();
            }

            tracer.Activity?.AddTag("idempotency.result", "processing");
            return false;
        }, memberName: $"{nameof(OnInvokeAsync)}::PreProcessing").ConfigureAwait(false);

        // Have processed before and have just arranged to replay the stored idempotency data; nice one stu (https://www.kiwitv.org.nz/index.php/tv-shows-mainmenu-42/46-kids/276-nice-one)!
        if (replayedResponse)
            return;

        // Where idempotency not requested for this request; carry on, nothing to see here.
        if (cached is null)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        // Prepare to capture the response.
        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        // Execute the next action then post-process.
        try
        {
            // Execute the next action in the pipeline.
            try
            {
                await next(context).ConfigureAwait(false);
            }
            catch
            {
                // Remove the in-progress marker for failed responses - retry with same idempotency-key allowed.
                await _invoker.InvokeAsync(this, async tracer =>
                {
                    await GuardedCacheRemoveAsync(idempotencyCacheKey!, cacheEntryOptions, tracer.Logger).ConfigureAwait(false);

                    tracer.Activity?.AddTag("idempotency.result", "error-and-removed");
                }, memberName: $"{nameof(OnInvokeAsync)}::PostProcessing").ConfigureAwait(false);

                // Keep bubbling...
                throw;
            }

            // Post-processing - cache the response on successful completion; otherwise, remove the in-progress marker.
            await _invoker.InvokeAsync(this, async tracer =>
            {
                // Cache the successful response.
                if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
                {
                    cached.StatusCode = context.Response.StatusCode;
                    cached.Headers = context.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToArray());

                    if (buffer.Length > MaxCachedResponseBodySize)
                    {
                        cached.Body = null;
                        cached.Status = IdempotencyStatus.CompletedTooLargeToReplay;
                        await _cache.SetByKeyAsync(idempotencyCacheKey!, cached, cacheEntryOptions).ConfigureAwait(false);

                        if (tracer.Logger is not null && tracer.Logger.IsEnabled(LogLevel.Warning))
                            tracer.Logger.LogWarning("Idempotent request with key '{IdempotencyKey}' response body size of {ResponseBodySize} bytes exceeds the maximum allowable cached size of {MaxCachedResponseBodySize} bytes; response will not be stored for replay.",
                                idempotencyKey, buffer.Length, MaxCachedResponseBodySize);

                        tracer.Activity?.AddTag("idempotency.result", "success-but-too-large-to-store");
                    }
                    else
                    {
                        buffer.Position = 0;
                        cached.Body = BinaryData.FromStream(buffer);
                        cached.Status = IdempotencyStatus.CompletedAndReplayable;
                        await _cache.SetByKeyAsync(idempotencyCacheKey!, cached, cacheEntryOptions).ConfigureAwait(false);

                        tracer.Activity?.AddTag("idempotency.result", "success-and-stored");
                    }
                }
                else
                {
                    // Remove the in-progress marker for failed responses - retry with same idempotency-key allowed.
                    await GuardedCacheRemoveAsync(idempotencyCacheKey!, cacheEntryOptions, tracer.Logger).ConfigureAwait(false);

                    tracer.Activity?.AddTag("idempotency.result", "error-and-removed");
                }

                // Rewind and copy the response back to the original stream.
                context.Response.Body = originalBody;
                context.Response.ContentLength = buffer.Length;

                buffer.Position = 0;
                await buffer.CopyToAsync(originalBody, context.RequestAborted).ConfigureAwait(false);
            }, memberName: $"{nameof(OnInvokeAsync)}::PostProcessing").ConfigureAwait(false);
        }
        finally
        {
            // Rewire the response body.
            context.Response.Body = originalBody;
        }
    }

    /// <summary>
    /// Guard the cache remove to swallow any exceptions.
    /// </summary>
    private async Task GuardedCacheRemoveAsync(string idempotencyCacheKey, HybridCacheEntryOptions? cacheEntryOptions, ILogger? logger)
    {
        try
        {
            await _cache.RemoveByKeyAsync(idempotencyCacheKey, cacheEntryOptions).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Log and swallow.
            if (logger?.IsEnabled(LogLevel.Warning) ?? false)
                logger?.LogWarning(ex, "Attempt to remove Idempotency-Key cache entry '{IdempotencyKey}' failed; underlying response processing has continued.", idempotencyCacheKey);
        }
    }
}