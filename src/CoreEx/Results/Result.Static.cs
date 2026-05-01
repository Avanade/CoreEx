namespace CoreEx.Results;

public readonly partial struct Result
{
    /// <summary>
    /// Gets the <see cref="IsSuccess"/> <see cref="Result"/>.
    /// </summary>
    public static Result Success { get; } = new();

    /// <summary>
    /// Gets the <see cref="IsSuccess"/> <see cref="Result"/> <see cref="Task"/>.
    /// </summary>
    public static Task<Result> SuccessTask => Task.FromResult(Success);

    /// <summary>
    /// Gets the <see cref="Result{T}"/> with a <see cref="Result{T}.Value"/> of <see langword="false"/>.
    /// </summary>
    public static Result<bool> False { get; } = new Result<bool>(false);

    /// <summary>
    /// Gets the <see cref="Result{T}"/> with a <see cref="Result{T}.Value"/> of <see langword="true"/>.
    /// </summary>
    public static Result<bool> True { get; } = new Result<bool>(true);

    /// <summary>
    /// Executes the specified <paramref name="action"/> and returns <see cref="Success"/>.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>The <see cref="Success"/> <see cref="Result"/>.</returns>
    /// <remarks>This is a helper method to simplify code where an <paramref name="action"/> should be invoked followed immediately by returning a corresponding <see cref="Success"/> to complete/conclude.</remarks>
    public static Result Done(Action action)
    {
        action.ThrowIfNull()();
        return Success;
    }

    /// <summary>
    /// Creates a <see cref="Result{T}"/> with a <see cref="Result{T}.Value"/> that is considered <see cref="Result{T}.IsSuccess"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>The <see cref="Result{T}"/> that is <see cref="Result{T}.IsSuccess"/>.</returns>
    public static Result<T> Ok<T>(T value) => Result<T>.Ok(value);

    /// <summary>
    /// Creates a <see cref="Result"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>).
    /// </summary>
    /// <param name="error">The error represented as an <see cref="Exception"/>.</param>
    /// <returns>The <see cref="Result"/> that has a state of <see cref="IsFailure"/>.</returns>
    public static Result Fail(Exception error) => new(error);

    /// <summary>
    /// Creates a <see cref="Result"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="ValidationException"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="configure">The optional configuration action.</param>
    /// <returns>The <see cref="Result"/> that has a state of <see cref="IsFailure"/>.</returns>
    public static Result Fail(LText? message = null, Action<ValidationException>? configure = null) => new((new ValidationException(message)).Adjust(configure));
}