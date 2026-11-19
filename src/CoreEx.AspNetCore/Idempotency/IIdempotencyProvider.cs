namespace CoreEx.AspNetCore.Idempotency;

/// <summary>
/// Provides the underlying <see cref="IdempotencyKeyMiddleware"/> implementation services.
/// </summary>
/// <remarks>See also <see cref="IdempotencyKeyAttribute"/>.</remarks>
public interface IIdempotencyProvider
{
    /// <summary>
    /// Represents the <see cref="IMiddleware.InvokeAsync(HttpContext, RequestDelegate)"/> idempotency handling.
    /// </summary>
    /// <param name="attribute">The <see cref="IdempotencyKeyAttribute"/>.</param>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <param name="next">The next <see cref="RequestDelegate"/>.</param>
    /// <param name="cancellationToken">The host or invocation cancellation token.</param>
    Task OnInvokeAsync(IdempotencyKeyAttribute attribute, HttpContext context, RequestDelegate next, CancellationToken cancellationToken = default);
}
