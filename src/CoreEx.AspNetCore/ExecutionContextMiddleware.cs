namespace CoreEx.AspNetCore;

/// <summary>
/// Provides an <see cref="ExecutionContext"/> handling middleware that (using dependency injection) enables additional configuration where required.
/// </summary>
/// <remarks>A new <see cref="ExecutionContext"/> <see cref="ExecutionContext.Current"/> instantiated through dependency injection is updated to use the <see cref="HttpContext.RequestServices"/>.</remarks>
/// <param name="next">The next <see cref="RequestDelegate"/>.</param>
/// <param name="configure">The optional function to update the <see cref="ExecutionContext"/>.</param>
public class ExecutionContextMiddleware(RequestDelegate next, Func<HttpContext, ExecutionContext, Task>? configure)
{
    private readonly RequestDelegate _next = next.ThrowIfNull();
    private readonly Func<HttpContext, ExecutionContext, Task> _configure = configure ?? DefaultConfigure;

    /// <summary>
    /// Provides a default configuration for the <see cref="ExecutionContext"/>.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContent"/>.</param>
    /// <param name="executionContext">The <see cref="ExecutionContext"/>.</param>
    public static Task DefaultConfigure(HttpContext httpContext, ExecutionContext executionContext) => Task.CompletedTask;

    /// <summary>
    /// Invokes the <see cref="ExecutionContextMiddleware"/>.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns>The <see cref="Task"/>.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var ec = context.ThrowIfNull().RequestServices.GetRequiredService<ExecutionContext>();
        ec.ServiceProvider ??= context.RequestServices;

        await _configure(context, ec).ConfigureAwait(false);
        await _next(context).ConfigureAwait(false);

        AddMessagesHeader(context, ec);
    }

    /// <summary>
    /// Adds the <see cref="HttpNames.WarningMessagesHeaderName"/> and <see cref="HttpNames.InfoMessagesHeaderName"/> to the <see cref="HttpContext.Response"/> where there are <see cref="ExecutionContext.Messages"/>.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <param name="executionContext">The <see cref="ExecutionContext"/>.</param>
    /// <remarks>Only adds where the <see cref="HttpResponse.StatusCode"/> is less than <c>400</c> and the <see cref="HttpNames.WarningMessagesHeaderName"/> and <see cref="HttpNames.InfoMessagesHeaderName"/> does not currently exist.</remarks>
    public static void AddMessagesHeader(HttpContext context, ExecutionContext executionContext)
    {
        context.ThrowIfNull();
        executionContext.ThrowIfNull();

        // Where there are info and/or warning messages then add to the response.
        if (executionContext.HasMessages && context.Response.StatusCode < 400)
        {
            if (!context.Response.Headers.ContainsKey(HttpNames.WarningMessagesHeaderName))
            {
                var msgs = executionContext.Messages!.Where(x => x.Type == MessageType.Warning).Select(x => x.Text?.ToString()).ToArray();
                if (msgs.Length > 0)
                    context.Response.Headers.Append(HttpNames.WarningMessagesHeaderName, new Microsoft.Extensions.Primitives.StringValues(msgs));
            }

            if (!context.Response.Headers.ContainsKey(HttpNames.InfoMessagesHeaderName))
            {
                var msgs = executionContext.Messages!.Where(x => x.Type == MessageType.Info).Select(x => x.Text.ToString()).ToArray();
                if (msgs.Length > 0)
                    context.Response.Headers.Append(HttpNames.InfoMessagesHeaderName, new Microsoft.Extensions.Primitives.StringValues(msgs));
            }
        }
    }
}