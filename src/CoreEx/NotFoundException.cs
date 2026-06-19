namespace CoreEx;

/// <summary>
/// Represents a <b>Not Found</b> exception.
/// </summary>
/// <remarks>The <see cref="Exception.Message"/> defaults to: <i>Requested data was not found.</i></remarks>
/// <param name="message">The error message.</param>
/// <param name="innerException">The inner <see cref="Exception"/>.</param>
public class NotFoundException(LText? message, Exception? innerException)
    : ExtendedException<NotFoundException>(message ?? new LText(typeof(NotFoundException).FullName, _message), innerException)
{
    private const string _message = "Requested data was not found.";

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class.
    /// </summary>
    public NotFoundException() : this(null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class using the specified <paramref name="message"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    public NotFoundException(LText? message) : this(message, null) { }

    /// <inheritdoc/>
    protected override void OnInitialize()
    {
        ErrorType = "not-found";
        StatusCode = GetConfiguredStatusCode(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Throws a <see cref="NotFoundException"/> if the <paramref name="value"/> is <see langword="default"/>.
    /// </summary>
    /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
    /// <param name="value">The value to validate as non-default.</param>
    /// <param name="message">The optional message <see cref="LText"/>.</param>
    /// <returns>The <paramref name="value"/> to support fluent-style method-chaining.</returns>
    [return: NotNull]
    public static T? ThrowIfDefault<T>([NotNull] T? value, LText? message = null)
    {
        if (value is null || EqualityComparer<T>.Default.Equals(value, default!))
            throw new NotFoundException(message);

        return value;
    }
}