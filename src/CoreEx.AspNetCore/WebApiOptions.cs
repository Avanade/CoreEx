namespace CoreEx.AspNetCore;

/// <summary>
/// Represents the <see cref="WebApi{TResult}"/> options.
/// </summary>
public sealed class WebApiOptions : WebApiOptionsBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WebApiOptions"/> class.
    /// </summary>
    /// <param name="httpRequest">The <see cref="HttpRequest"/>.</param>
    public WebApiOptions(HttpRequest httpRequest) : base(httpRequest) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="WebApiOptions"/> class from an existing instance.
    /// </summary>
    /// <param name="options">The <see cref="WebApiOptionsBase"/>.</param>
    public WebApiOptions(WebApiOptionsBase options) : base(options) { }
}