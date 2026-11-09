namespace CoreEx.AspNetCore.Http;

/// <summary>Shares HTTP correlation capture and deferred response headers between ASP.NET and Functions.</summary>
public static class HttpCorrelation
{
    /// <summary>Captures a supplied header without overwriting factory metadata when absent.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="executionContext">The execution context.</param>
    /// <returns>False when the supplied header is invalid; the execution correlation is cleared.</returns>
    public static bool TryCapture(HttpContext context, ExecutionContext executionContext)
    {
        if (!context.ThrowIfNull().Request.Headers.ContainsKey(HttpNames.CorrelationIdHeaderName))
            return true;

        var values = context.Request.Headers[HttpNames.CorrelationIdHeaderName];
        if (values.Count != 1 || values[0] is null || !Correlation.IsValid(values[0]))
        {
            executionContext.CorrelationId = null;
            return false;
        }

        executionContext.CorrelationId = values[0];
        return true;
    }

    /// <summary>Registers a response header using the execution reference, including deferred Functions rendering.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="executionContext">The owning execution.</param>
    public static void RegisterResponse(HttpContext context, ExecutionContext executionContext)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.OnStarting(() =>
        {
            ApplyResponse(context, executionContext);
            return Task.CompletedTask;
        });
    }

    /// <summary>Applies the effective correlation immediately before committing the response.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="executionContext">The owning execution.</param>
    public static void ApplyResponse(HttpContext context, ExecutionContext executionContext)
    {
        if (context.Response.HasStarted)
            return;

        if (executionContext.CorrelationId is null)
            context.Response.Headers.Remove(HttpNames.CorrelationIdHeaderName);
        else
            context.Response.Headers[HttpNames.CorrelationIdHeaderName] = executionContext.CorrelationId;
    }

    /// <summary>Renders safe HTTP validation failure before invoking application code.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The rendering task.</returns>
    public static async Task WriteInvalidHeaderAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var webApi = context.RequestServices.GetService<WebApi>() ?? new WebApi(logger: context.RequestServices.GetService<ILogger<WebApi>>());
        var result = webApi.CreateErrorResult(context.Request, new ValidationException("X-Correlation-Id must contain one 1-256-character visible ASCII token without whitespace or commas."), cancellationToken);
        await result.ExecuteAsync(context).ConfigureAwait(false);
    }
}
