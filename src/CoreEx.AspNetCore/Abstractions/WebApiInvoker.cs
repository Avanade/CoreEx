namespace CoreEx.AspNetCore.Abstractions;

/// <summary>
/// Provides the base <see cref="WebApi{TResult}"/> invoker.
/// </summary>
/// <typeparam name="TResult">The ASP.NET Core result <see cref="Type"/>.</typeparam>
public abstract class WebApiInvoker<TResult> : InvokerBase<WebApi<TResult>> { }