namespace CoreEx.AspNetCore.Idempotency;

/// <summary>
/// Provides the <see cref="Mvc.IdempotencyKeyAttribute"/> handling middleware to enable idempotent operations via a pluggable <see cref="IIdempotencyProvider"/>.
/// </summary>
public sealed class IdempotencyKeyMiddleware(IIdempotencyProvider provider) : IMiddleware
{
    private readonly IIdempotencyProvider? _provider = provider.ThrowIfNull();

    /// <inheritdoc/>
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Check if the endpoint supports idempotency.
        var endpoint = context.ThrowIfNull().GetEndpoint();
        var idempotencyAttribute = endpoint?.Metadata.GetMetadata<Mvc.IdempotencyKeyAttribute>();

        // Where no idempotency attribute, just continue on as normal; otherwise, Invoke the provider to handle.
        if (idempotencyAttribute is null)
            await next(context).ConfigureAwait(false);
        else
            await _provider.ThrowIfNull().OnInvokeAsync(idempotencyAttribute, context, next).ConfigureAwait(false);
    }
}