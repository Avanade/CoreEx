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
    /// <returns>The <paramref name="requestOptions"/> to support fluent-style method-chaining.</returns>
    public static TRequestOptions WithLocationUri<TRequestOptions>(this TRequestOptions requestOptions, Func<Uri> locationUri) where TRequestOptions : WebApiOptionsBase
    {
        requestOptions.ThrowIfNull().LocationUri = locationUri;
        return requestOptions;
    }

    /// <summary>
    /// Asserts that an <c>If-Match</c> header (<see cref="WebApiOptionsBase.ETag"/>) was supplied for the request.
    /// </summary>
    /// <typeparam name="TRequestOptions">The <see cref="WebApiOptionsBase"/> <see cref="Type"/>.</typeparam>
    /// <param name="requestOptions">The <see cref="WebApiOptionsBase"/>.</param>
    /// <returns>The <paramref name="requestOptions"/> to support fluent-style method-chaining.</returns>
    /// <remarks>Checks <see cref="WebApiOptionsBase.ETag"/> immediately (i.e. at the point this is called, not deferred to a later checkpoint), so it is safe to call from within a
    /// verb handler regardless of method. Unlike the automatic PUT/PATCH requirement (enforced only where the request value implements <see cref="IETag"/>), this applies regardless
    /// of whether the underlying value implements <see cref="IETag"/> — it simply asserts the header itself was present. Use for an operation (e.g. POST or DELETE) where an
    /// <c>If-Match</c> precondition is required but is not implied by convention — for example, a POST that adds an item to a booking and must fail if the booking has changed since
    /// it was read.</remarks>
    /// <exception cref="ConcurrencyException">Thrown where <see cref="WebApiOptionsBase.ETag"/> is <see langword="null"/>.</exception>
    public static TRequestOptions WithIfMatchRequired<TRequestOptions>(this TRequestOptions requestOptions) where TRequestOptions : WebApiOptionsBase
    {
        if (requestOptions.ThrowIfNull().ETag is null)
            throw new ConcurrencyException(WebApiOptionsBase.ConcurrencyMessage).WithStatusCode(HttpStatusCode.PreconditionRequired);

        return requestOptions;
    }
}
