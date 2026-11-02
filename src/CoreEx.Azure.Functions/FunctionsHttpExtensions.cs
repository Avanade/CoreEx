namespace CoreEx.Azure.Functions;

/// <summary>
/// Provides explicit HTTP idempotency integration for ASP.NET Core-based Functions.
/// </summary>
public static class FunctionsHttpExtensions
{
    /// <summary>
    /// Executes and renders an HTTP operation within the registered idempotency provider's capture boundary.
    /// </summary>
    /// <param name="webApi">The WebApi instance used to represent idempotency errors.</param>
    /// <param name="request">The originating request.</param>
    /// <param name="function">The deferred operation returning an MVC result, normally through WebApi.</param>
    /// <param name="isRequired">Whether the request must include an idempotency key.</param>
    /// <param name="cancellationToken">The invocation cancellation token.</param>
    /// <returns>An empty result after response execution, or a WebApi error result for a pre-processing failure.</returns>
    /// <remarks>The operation and its result execute before this method returns. Return its result directly from the function; do not write another response. Register <see cref="IIdempotencyProvider"/> separately. Streaming and additional HTTP output bindings are not supported by this helper.</remarks>
    public static async Task<IActionResult> FunctionWithIdempotencyAsync(this CoreEx.AspNetCore.Mvc.WebApi webApi, HttpRequest request, Func<CancellationToken, Task<IActionResult>> function,
        bool isRequired = false, CancellationToken cancellationToken = default)
    {
        webApi.ThrowIfNull();
        request.ThrowIfNull();
        function.ThrowIfNull();
        cancellationToken.ThrowIfCancellationRequested();
        if (request.HttpContext.Response.HasStarted)
            throw new InvalidOperationException("The response has already started; idempotency must precede response execution.");

        var provider = request.HttpContext.RequestServices.GetRequiredService<IIdempotencyProvider>();
        var invoked = false;
        try
        {
            await provider.OnInvokeAsync(new IdempotencyKeyAttribute(isRequired), request.HttpContext, async context =>
            {
                invoked = true;
                cancellationToken.ThrowIfCancellationRequested();
                var result = await function(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                await result.ExecuteResultAsync(new ActionContext(context, context.GetRouteData(), new ActionDescriptor())).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
        catch (Exception ex) when (!invoked && !request.HttpContext.Response.HasStarted)
        {
            return webApi.CreateErrorResult(request, ex, cancellationToken);
        }

        return new EmptyResult();
    }
}
