namespace CoreEx.Results;

/// <summary>
/// Represents the outcome of an operation with a <see cref="Value"/>.
/// </summary>
/// <typeparam name="T">The <see cref="Value"/> <see cref="Type"/>.</typeparam>
/// <remarks>There are logic and performance benefits for leveraging a <see cref="Result{T}"/>, especially where explicitly managing <i>known/expected</i> errors, as this can avoid the overhead of throwing exceptions. 
/// This instead, provides a means to manage and return errors in a more functional manner (see <see href="https://en.wikipedia.org/wiki/Monad_(functional_programming)">monad</see>-based error handling and 
/// <see href="https://fsharpforfunandprofit.com/posts/recipe-part2/">Railway Oriented Programming</see> for more information). 
/// <para>This is not to say that exceptions are not valid and should be avoided, they absolutely serve a purpose and should continue to be leveraged where the outcome of an operation is unexpected.
/// See the <see href="https://learn.microsoft.com/en-us/dotnet/fsharp/style-guide/conventions#error-management">error management</see> guidance provided by Microsoft.</para>
/// <para>However, in some instances where returning a business functional error that is intended to be handled by the consumer then an <see cref="Exception"/>, although convenient, is possibly not the best approach.
/// Finally, an exception contains additional context, such as the stack trace to assist with the likes of troubleshooting, which is generally not required for an explicit (expected) business error.</para>
/// <para>See also <see cref="Result"/>.</para></remarks>
[DebuggerStepThrough]
[DebuggerDisplay("{ToDebuggerString()}")]
public readonly partial struct Result<T> : IResult<T>, IEquatable<Result<T>>
{
    private readonly T _value = default!;
    private readonly Exception? _error = default;

    /// <summary>
    /// Initializes a new <see cref="IsSuccess"/> instance of the <see cref="Result{T}"/> with a <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The <see cref="Result{T}.Value"/>.</param>
    public Result(T value) => _value = value;

    /// <summary>
    /// Initializes a new <see cref="IsFailure"/> instance of the <see cref="Result{T}"/> with a corresponding <paramref name="error"/>.
    /// </summary>
    /// <param name="error">The error represented as an <see cref="Exception"/>.</param>
    public Result(Exception error) => _error = error.ThrowIfNull();

    /// <inheritdoc/>
    object? IResult.Value => Value;

    /// <inheritdoc/>
    public T Value
    {
        get
        {
            if (IsSuccess)
                return _value;

            Result.ThrowErrorOrAggregateException(Error);
            return default;
        }
    }

    /// <summary>
    /// Gets the underlying <see cref="Value"/> where <see cref="IsSuccess"/>; otherwise, returns the default value for the type <typeparamref name="T"/>.
    /// </summary>
    public T? ValueOrDefault => IsSuccess ? _value : default;

    /// <inheritdoc/>
    public Exception Error { get => _error ?? throw new InvalidOperationException($"The {nameof(Error)} cannot be accessed as the {nameof(Result)} is in a successful state."); }

    /// <inheritdoc/>
    public bool IsSuccess => _error is null;

    /// <inheritdoc/>
    public bool IsFailure => _error is not null;

    /// <inheritdoc/>
    IResult IResult.ToFailure(Exception error) => new Result<T>(error);

    /// <inheritdoc/>
    public bool IsFailureOfType<TException>() where TException : Exception => _error is not null && _error is TException;

    /// <summary>
    /// Throws the <see cref="Error"/> where <see cref="IsFailure"/>; otherwise, does nothing.
    /// </summary>
    /// <returns>The <see cref="Result{T}"/> where <see cref="IsSuccess"/> to enable further fluent-style method-chaining.</returns>
    public Result<T> ThrowOnError()
    {
        if (IsFailure)
            Result.ThrowErrorOrAggregateException(Error);

        return this;
    }

    /// <inheritdoc/>
    public override string ToString() => IsSuccess ? $"Success: {(Value is null ? "null" : Value)}" : $"Failure: {Error.Message}";

    /// <summary>
    /// Get the <see cref="string"/> representation of the <see cref="Result"/> for debugging purposes.
    /// </summary>
    private string ToDebuggerString() => IsSuccess ? $"Success: {(Value is null ? "null" : Value)}" : $"Failure: {Error.Message} [{Error.GetType().Name}]";

    /// <summary>
    /// Requires (validates) that the <see cref="Value"/> is non-default; otherwise, will result in a <see cref="Result.ValidationError(MessageItem, Action{ValidationException}?)"/>.
    /// </summary>
    /// <param name="name">The value name (defaults to <see cref="Validation.Validation.ValueName"/>).</param>
    /// <param name="text">The friendly text name used in validation messages (defaults to <paramref name="name"/> as sentence case where not specified).</param>
    /// <returns>The resulting <see cref="Result{T}"/>.</returns>
    /// <remarks>The format of the error messages is defined by <see cref="Validation.Validation.MandatoryFormat"/>.</remarks>
    public Result<T> Required(string? name = null, LText? text = null)
    {
        if (IsSuccess && EqualityComparer<T>.Default.Equals(Value, default!))
            return Result.ValidationError(MessageItem.CreateErrorMessage(name
                ?? Validation.Validation.ValueName, Validation.Validation.MandatoryFormat, text
                    ?? ((name is null || name == Validation.Validation.ValueName) ? Validation.Validation.ValueText : name.ToSentenceCase()!)));

        return this;
    }

    /// <summary>
    /// Converts the <see cref="Result"/> to a <see cref="Task{TResult}"/> using <see cref="Task.FromResult{TResult}(TResult)"/>.
    /// </summary>
    /// <returns>The completed <see cref="Task{TResult}"/>.</returns>
    public Task<Result<T>> AsTask() => Task.FromResult(this);

    /// <summary>
    /// Implicitly converts an <see cref="Exception"/> to a <see cref="Result"/> that is considered <see cref="IsFailure"/>.
    /// </summary>
    /// <param name="error">The underlying error represented as an <see cref="Exception"/>.</param>
    public static implicit operator Result<T>(Exception error) => new(error);

    /// <summary>
    /// Implicitly converts a <see cref="Result"/> to a <see cref="Result{T}"/> defaulting the <see cref="Value"/> where <see cref="IsSuccess"/>.
    /// </summary>
    /// <param name="result">The <see cref="Result"/>.</param>
    public static implicit operator Result<T>(Result result) => result.Bind(() => new Result<T>());

    /// <summary>
    /// Explicitly converts a <see cref="Result{T}"/> to a <see cref="Result"/> losing the <see cref="Value"/> where <see cref="IsSuccess"/>.
    /// </summary>
    /// <param name="result">The <see cref="Result{T}"/></param>
    public static explicit operator Result(Result<T> result) => result.Bind();

    /// <summary>
    /// Implicitly converts a <see cref="Value"/> to a <see cref="Result{T}"/> as <see cref="IsSuccess"/>.
    /// </summary>
    /// <param name="value">The underlying value.</param>
    public static implicit operator Result<T>(T value) => Result<T>.Ok(value);

    /// <summary>
    /// Implicitly converts a <see cref="Result{T}"/> to a <see cref="Value"/> where <see cref="IsSuccess"/>.
    /// </summary>
    /// <param name="result">The <see cref="Result{T}"/></param>
    public static implicit operator T(Result<T> result) => result.Value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Result<T> r && Equals(r);

    /// <inheritdoc/>
    public bool Equals(Result<T> other) => IsSuccess ? (other.IsSuccess && EqualityComparer<T>.Default.Equals(Value, other.Value)) : (IsFailure == other.IsFailure && Error.GetType() == other.Error.GetType() && Error.ToString() == other.Error.ToString());

    /// <summary>
    /// Indicates whether the current <see cref="Result"/> is equal to another <see cref="Result"/>.
    /// </summary>
    public static bool operator ==(Result<T> left, Result<T> right) => left.Equals(right);

    /// <summary>
    /// Indicates whether the current <see cref="Result"/> is not equal to another <see cref="Result"/>.
    /// </summary>
    public static bool operator !=(Result<T> left, Result<T> right) => !(left == right);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(IsSuccess, IsSuccess ? (Value?.GetHashCode() ?? 0) : 0, IsFailure ? Error.GetHashCode() : 0);
}