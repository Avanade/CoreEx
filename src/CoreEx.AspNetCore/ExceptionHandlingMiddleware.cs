namespace CoreEx.AspNetCore;

/// <summary>
/// Provides unhandled <see cref="Exception"/> handling to be used by standard middleware to convert into a <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/>.
/// </summary>
public static class ExceptionHandlingMiddleware
{
    /// <summary>
    /// Configures the application to use <i>CoreEx</i> exception handling.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
    public static void ConvertExceptionToProblemDetails(this IApplicationBuilder app) => app.Run(async context =>
    {
        // Where the response has already started then do nothing.
        if (context.Response.HasStarted)
            return;

        // Get the exception and process using standardized handling; note, already logged by ASPNET, so bypass ours to ensure double logging does not occur.
        var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        if (ex is not null)
        {
            var webApi = context.RequestServices.GetRequiredService<Http.WebApi>();
            var ir = webApi.CreateResult(new WebApiResult<IResult>(context.Response) { Exception = ex, BypassExceptionLogging = true });
            await ir.ExecuteAsync(context).ConfigureAwait(false);
        }
    });
}