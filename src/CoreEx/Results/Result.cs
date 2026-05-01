namespace CoreEx.Results;

/// <summary>
/// Represents the outcome of an operation with no value.
/// </summary>
/// <remarks>There are logic and performance benefits for leveraging a <see cref="Result"/>, especially where explicitly managing <i>known/expected</i> errors, as this can avoid the overhead of throwing exceptions. 
/// This instead, provides a means to manage and return errors in a more functional manner (see <see href="https://en.wikipedia.org/wiki/Monad_(functional_programming)">monad</see>-based error handling and 
/// <see href="https://fsharpforfunandprofit.com/posts/recipe-part2/">Railway Oriented Programming</see> for more information). 
/// <para>This is not to say that exceptions are not valid and should be avoided, they absolutely serve a purpose and should continue to be leveraged where the outcome of an operation is unexpected.
/// See the <see href="https://learn.microsoft.com/en-us/dotnet/fsharp/style-guide/conventions#error-management">error management</see> guidance provided by Microsoft.</para>
/// <para>However, in some instances where returning a business functional error that is intended to be handled by the consumer then an <see cref="Exception"/>, although convenient, is possibly not the best approach.
/// Finally, an exception contains additional context, such as the stack trace to assist with the likes of troubleshooting, which is generally not required for an explicit (expected) business error.</para>
/// <para>See also <see cref="Result{T}"/>.</para></remarks>
[DebuggerStepThrough]
[DebuggerDisplay("{ToDebuggerString()}")]
public readonly partial struct Result : IResult, IEquatable<Result>
{
    private readonly Exception? _error = default;

    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> that is considered <see cref="IsSuccess"/>.
    /// </summary>
    public Result() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>).
    /// </summary>
    /// <param name="error">The error represented as an <see cref="Exception"/>.</param>
    public Result(Exception error) => _error = error.ThrowIfNull();

    /// <inheritdoc/>
    object? IResult.Value
    {
        get
        {
            ThrowOnError();
            return null;
        }
    }

    /// <inheritdoc/>
    public Exception Error { get => _error ?? throw new InvalidOperationException($"The {nameof(Error)} cannot be accessed as the {nameof(Result)} is in a successful state."); }

    /// <inheritdoc/>
    public bool IsSuccess => _error is null;

    /// <inheritdoc/>
    public bool IsFailure => _error is not null;

    /// <inheritdoc/>
    IResult IResult.ToFailure(Exception error) => new Result(error);

    /// <inheritdoc/>
    public bool IsFailureOfType<TException>() where TException : Exception => _error is not null && _error is TException;

    /// <summary>
    /// Converts the <see cref="Result"/> to a corresponding <see cref="Result{T}"/> (of <see cref="Type"/> <typeparamref name="T"/>) defaulting to <see cref="Result{T}.Success"/> where <see cref="IsSuccess"/>; otherwise, where
    /// <see cref="IsFailure"/> returns a resulting instance with the corresponding <see cref="Error"/>.
    /// </summary>
    /// <typeparam name="T">The (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
    /// <returns>The corresponding <see cref="Result{T}"/>.</returns>
    /// <remarks>This invokes <see cref="ResultsExtensions.Bind{T}(Result)"/> internally to perform.</remarks>
    public Result<T> ToResult<T>() => this.Bind<T>();

    /// <summary>
    /// Throws the <see cref="Error"/> where <see cref="IsFailure"/>; otherwise, does nothing.
    /// </summary>
    /// <returns>The <see cref="Result"/> where <see cref="IsSuccess"/> to enable further fluent-style method-chaining.</returns>
    public Result ThrowOnError()
    {
        if (IsFailure)
            ThrowErrorOrAggregateException(Error);

        return this;
    }

    /// <summary>
    /// Throws either the <paramref name="error"/> directly where not previously thrown; otherwise, throws a new <see cref="AggregateException"/> which contains the originating <paramref name="error"/>.
    /// </summary>
    /// <param name="error">The originating <see cref="Exception"/>.</param>
    [DoesNotReturn]
    internal static void ThrowErrorOrAggregateException(Exception error)
    {
        if (error.StackTrace is null)
            throw error;

        throw new AggregateException(error);
    }

    /// <summary>
    /// Converts the <see cref="Result"/> to a <see cref="Task{TResult}"/> using <see cref="Task.FromResult{TResult}(TResult)"/>.
    /// </summary>
    /// <returns>The completed <see cref="Task{TResult}"/>.</returns>
    public Task<Result> AsTask() => Task.FromResult(this);

    /// <inheritdoc/>
    public override string ToString() => IsSuccess ? "Success." : $"Failure: {Error.Message}";

    /// <summary>
    /// Get the <see cref="string"/> representation of the <see cref="Result"/> for debugging purposes.
    /// </summary>
    private string ToDebuggerString() => IsSuccess ? "Success." : $"Failure: {Error.Message} [{Error.GetType().Name}]";

    /// <summary>
    /// Implicitly converts an <see cref="Exception"/> to a <see cref="Result"/> that is considered <see cref="IsFailure"/>.
    /// </summary>
    /// <param name="error">The underlying error represented as an <see cref="Exception"/>.</param>
    public static implicit operator Result(Exception error) => new(error);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Result r && Equals(r);

    /// <inheritdoc/>
    public bool Equals(Result other) => IsSuccess ? other.IsSuccess : (IsFailure == other.IsFailure && Error.GetType() == other.Error.GetType() && Error.ToString() == other.Error.ToString());

    /// <summary>
    /// Indicates whether the current <see cref="Result"/> is equal to another <see cref="Result"/>.
    /// </summary>
    public static bool operator ==(Result left, Result right) => left.Equals(right);

    /// <summary>
    /// Indicates whether the current <see cref="Result"/> is not equal to another <see cref="Result"/>.
    /// </summary>
    public static bool operator !=(Result left, Result right) => !(left == right);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(IsSuccess, IsFailure ? Error.GetHashCode() : 0);
}