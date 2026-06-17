namespace CoreEx.AspNetCore.Http;

/// <summary>
/// Provides the ASP.NET Core Minimal Web API <see cref="WebApi{TResult}"/> invoker.
/// </summary>
[InvokerName("CoreEx.AspNetCore.Http.WebApi")]
public class WebApiInvoker : WebApiInvoker<IResult>
{
    private static WebApiInvoker? _default;

    /// <summary>
    /// Gets the default <see cref="WebApiInvoker"/> instance.
    /// </summary>
    public static WebApiInvoker Default => ExecutionContext.GetService<WebApiInvoker>() ?? (_default ??= new WebApiInvoker());
}