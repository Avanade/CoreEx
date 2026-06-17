
namespace CoreEx.Validation;

/// <summary>
/// Represents the result of a <see cref="MultiValidator"/> <see cref="MultiValidator.ValidateAsync(System.Threading.CancellationToken)"/>.
/// </summary>
public class MultiValidatorResult : IValidationResult
{
    private MessageItemCollection? _messages;

    /// <inheritdoc/>
    /// <remarks>This is nonsensical and as such will throw a <see cref="NotSupportedException"/>.</remarks>
    object? IValidationResult.Value => throw new NotSupportedException();

    /// <inheritdoc/>
    public bool HasErrors { get; private set; }

    /// <inheritdoc/>
    public MessageItemCollection Messages
    {
        get
        {
            if (_messages is not null)
                return _messages;

            _messages = [];
            _messages.CollectionChanged += Messages_CollectionChanged;
            return _messages;
        }
    }

    /// <summary>
    /// Handle the add of a message.
    /// </summary>
    private void Messages_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (HasErrors)
            return;

        switch (e.Action)
        {
            case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                foreach (var m in e.NewItems!)
                {
                    MessageItem mi = (MessageItem)m;
                    if (mi.Type == MessageType.Error)
                    {
                        HasErrors = true;
                        return;
                    }
                }

                break;

            default:
                throw new InvalidOperationException("Operation invalid for Messages; only an Add is supported.");
        }
    }

    /// <inheritdoc/>
    public Exception? ToException() => HasErrors ? new ValidationException(Messages!) : null;

    /// <inheritdoc/>
    IValidationResult IValidationResult.ThrowOnError() => ThrowOnError();

    /// <summary>
    /// Throws a <see cref="ValidationException"/> where an error was found (and optionally any warnings).
    /// </summary>
    /// <returns>The <see cref="MultiValidatorResult"/> to support fluent-style method-chaining.</returns>
    public MultiValidatorResult ThrowOnError()
    {
        var ex = ToException();
        if (ex is not null)
            throw ex;

        return this;
    }

    /// <inheritdoc/>
    public Result ToResult() => HasErrors ? Result.ValidationError(Messages!) : Result.Success;
}