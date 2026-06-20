namespace CoreEx.AspNetCore;

/// <summary>
/// Provides standard extensions.
/// </summary>
public static partial class AspNetCoreExtensions
{
    /// <summary>
    /// Overrides the <see cref="WebApiOptionsBase.StatusCode"/>.
    /// </summary>
    /// <typeparam name="TRequestOptions">The <see cref="WebApiOptionsBase"/> <see cref="Type"/>.</typeparam>
    /// <param name="requestOptions">The <see cref="WebApiOptionsBase"/>.</param>
    /// <param name="statusCode">The <see cref="HttpStatusCode"/>.</param>
    /// <returns>The <paramref name="requestOptions"/> to support fluent-style method-chaining.</returns>
    public static TRequestOptions WithStatusCode<TRequestOptions>(this TRequestOptions requestOptions, HttpStatusCode statusCode) where TRequestOptions : WebApiOptionsBase
    {
        requestOptions.ThrowIfNull().StatusCode = statusCode;
        return requestOptions;
    }

    /// <summary>
    /// Overrides the <see cref="WebApiOptionsBase.AlternateStatusCode"/>.
    /// </summary>
    /// <typeparam name="TRequestOptions">The <see cref="WebApiOptionsBase"/> <see cref="Type"/>.</typeparam>
    /// <param name="requestOptions">The <see cref="WebApiOptionsBase"/>.</param>
    /// <param name="alternateStatusCode">The <see cref="HttpStatusCode"/>.</param>
    /// <returns>The <paramref name="requestOptions"/> to support fluent-style method-chaining.</returns>
    public static TRequestOptions WithAlternateStatusCode<TRequestOptions>(this TRequestOptions requestOptions, HttpStatusCode alternateStatusCode) where TRequestOptions : WebApiOptionsBase
    {
        requestOptions.ThrowIfNull().AlternateStatusCode = alternateStatusCode;
        return requestOptions;
    }

    /// <summary>
    /// Overrides the <see cref="WebApiOptionsBase.OperationType"/>.
    /// </summary>
    /// <typeparam name="TRequestOptions">The <see cref="WebApiOptionsBase"/> <see cref="Type"/>.</typeparam>
    /// <param name="requestOptions">The <see cref="WebApiOptionsBase"/>.</param>
    /// <param name="operationType">The <see cref="CoreEx.OperationType"/>.</param>
    /// <returns>The <paramref name="requestOptions"/> to support fluent-style method-chaining.</returns>
    public static TRequestOptions WithOperationType<TRequestOptions>(this TRequestOptions requestOptions, OperationType operationType) where TRequestOptions : WebApiOptionsBase
    {
        requestOptions.ThrowIfNull().OperationType = operationType;
        return requestOptions;
    }

    /// <summary>
    /// Overrides the <see cref="WebApiOptionsBase.LocationUri"/> function.
    /// </summary>
    /// <typeparam name="TRequestOptions">The <see cref="WebApiOptionsBase"/> <see cref="Type"/>.</typeparam>
    /// <param name="requestOptions">The <see cref="WebApiOptionsBase"/>.</param>
    /// <param name="locationUri">The function to return the location <see cref="Uri"/>.</param>
    /// <returns></returns>
    public static TRequestOptions WithLocationUri<TRequestOptions>(this TRequestOptions requestOptions, Func<Uri> locationUri) where TRequestOptions : WebApiOptionsBase
    {
        requestOptions.ThrowIfNull().LocationUri = locationUri;
        return requestOptions;
    }
}