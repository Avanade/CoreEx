namespace CoreEx;

/// <summary>
/// Represents a <b>Data Consistency</b> exception.
/// </summary>
/// <remarks>An example would be where the operation would result in data consistency error; i.e. possible data corruption may occur.
/// <para>This is not considered an error (<see cref="IExtendedException.IsError"/> is set to <see langword="false"/>).</para>
/// <para>The <see cref="Exception.Message"/> defaults to: <i>A potential data consistency error occurred.</i></para></remarks>
/// <param name="message">The error message.</param>
/// <param name="innerException">The inner <see cref="Exception"/>.</param>
public class DataConsistencyException(LText? message, Exception? innerException) 
    : ExtendedException<DataConsistencyException>(message ?? new LText(typeof(DataConsistencyException).FullName, _message), innerException, true)
{
    private const string _message = "A potential data consistency error occurred.";

    /// <summary>
    /// Initializes a new instance of the <see cref="DataConsistencyException"/> class.
    /// </summary>
    public DataConsistencyException() : this(null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataConsistencyException"/> class using the specified <paramref name="message"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    public DataConsistencyException(LText? message) : this(message, null) { }

    /// <inheritdoc/>
    protected override void OnInitialize()
    {
        ErrorType = "data-consistency";
        StatusCode = GetConfiguredStatusCode(HttpStatusCode.InternalServerError);
        IsError = false;
    }
}