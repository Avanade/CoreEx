using Microsoft.AspNetCore.Mvc;

namespace CoreEx.AspNetCore.Mvc;

/// <summary>
/// Provides the ASP.NET Core MVC Web API <see cref="WebApi{TResult}"/> invoker
/// </summary>
[InvokerName("CoreEx.AspNetCore.Mvc.WebApiInvoker")]
public class WebApiInvoker : WebApiInvoker<IActionResult>
{
    private static WebApiInvoker? _default;

    /// <summary>
    /// Gets the default <see cref="WebApiInvoker"/> instance.
    /// </summary>
    public static WebApiInvoker Default => ExecutionContext.GetService<WebApiInvoker>() ?? (_default ??= new WebApiInvoker());
}