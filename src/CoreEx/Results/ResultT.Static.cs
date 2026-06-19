namespace CoreEx.Results;

public readonly partial struct Result<T>
{
    /// <summary>
    /// Gets the <see cref="IsSuccess"/> <see cref="Result{T}"/> with a default <see cref="Value"/>.
    /// </summary>
    public static Result<T> Success { get; } = default;

    /// <summary>
    /// Creates a <see cref="Result{T}"/> with a default <see cref="Result{T}.Value"/> that is considered <see cref="Result{T}.IsSuccess"/>.
    /// </summary>
    /// <returns>The <see cref="Result{T}"/> that is <see cref="Result{T}.IsSuccess"/> (see <see cref="Result{T}.Success"/>).</returns>
    public static Result<T> Ok() => Success;

    /// <summary>
    /// Creates a <see cref="Result{T}"/> with a <see cref="Result{T}.Value"/> that is considered <see cref="Result{T}.IsSuccess"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The <see cref="Result{T}"/> that is <see cref="Result{T}.IsSuccess"/>.</returns>
    /// <remarks>This is synonymous with <see cref="Map(T)"/>.</remarks>
    public static Result<T> Ok(T value) => Map(value);

    /// <summary>
    /// Maps a <paramref name="value"/> into a <see cref="Result{T}"/> that is considered <see cref="Result{T}.IsSuccess"/>.
    /// </summary>
    /// <param name="value">The value to map.</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    /// <remarks>This is synonymous with <see cref="Ok(T)"/>.</remarks>
    public static Result<T> Map(T value) => new(value);

    /// <summary>
    /// Creates a <see cref="Result{T}"/> with an <see cref="Result{T}.Error"/> (see <see cref="Result{T}.IsFailure"/>).
    /// </summary>
    /// <param name="error">The error represented as an <see cref="Exception"/>.</param>
    /// <returns>The <see cref="Result{T}"/> that has a state of <see cref="Result{T}.IsFailure"/>.</returns>
    public static Result<T> Fail(Exception error) => new(error);

    /// <summary>
    /// Creates a <see cref="Result{T}"/> with an <see cref="Result{T}.Error"/> (see <see cref="Result{T}.IsFailure"/>) of type <see cref="ValidationException"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="configure">The optional configuration action.</param>
    /// <returns>The <see cref="Result{T}"/> that has a state of <see cref="Result{T}.IsFailure"/>.</returns>
    public static Result<T> Fail(LText message, Action<ValidationException>? configure = null) => new((new ValidationException(message)).Adjust(configure));
}