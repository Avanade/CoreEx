namespace CoreEx.Validation;

/// <summary>
/// Provides the <typeparamref name="TEntity"/> <see cref="IValidationContext{TEntity}"/>.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
/// <param name="value">The entity value.</param>
/// <param name="args">The <see cref="ValidationArgs"/>.</param>
/// <param name="fullyQualifiedEntityNameOverride">Optional <see cref="ValidationArgs.FullyQualifiedEntityName"/> override.</param>
/// <param name="fullyQualifiedJsonEntityNameOverride">Optional <see cref="ValidationArgs.FullyQualifiedJsonEntityName"/> override.</param>
public sealed partial class ValidationContext<TEntity>(TEntity value, ValidationArgs args, string? fullyQualifiedEntityNameOverride = null, string? fullyQualifiedJsonEntityNameOverride = null) : IValidationContext<TEntity> where TEntity : class
{
    private HashSet<string>? _errorProperties;

    /// <summary>
    /// Gets the <see cref="ValidationArgs"/>.
    /// </summary>
    public ValidationArgs Args { get; } = args.ThrowIfNull();

    /// <inheritdoc/>
    public Type EntityType => typeof(TEntity);

    /// <inheritdoc/>
    object? IValidationResult.Value => Value;

    /// <inheritdoc/>
    public TEntity Value { get; } = value;

    /// <inheritdoc/>
    public string? FullyQualifiedEntityName { get; } = fullyQualifiedEntityNameOverride ?? args.FullyQualifiedEntityName;

    /// <inheritdoc/>
    public string? FullyQualifiedJsonEntityName { get; } = fullyQualifiedJsonEntityNameOverride ?? args.FullyQualifiedJsonEntityName;

    /// <inheritdoc/>
    public bool UseJsonNames { get; } = args.UseJsonNames;

    /// <inheritdoc/>
    public JsonSerializerOptions? JsonSerializerOptions { get; } = args.JsonSerializerOptions;

    /// <inheritdoc/>
    public IServiceProvider? ServiceProvider { get; } = args.ServiceProvider;

    /// <summary>
    /// Gets the <see cref="MessageItemCollection"/>.
    /// </summary>
    public MessageItemCollection? Messages { get; private set; }

    /// <inheritdoc/>
    public IDictionary<string, object?> Parameters { get; } = args.Parameters;

    /// <summary>
    /// Indicates whether there has been a validation error.
    /// </summary>
    public bool HasErrors { get; private set; }

    /// <summary>
    /// Determines whether the specified fully qualified property name has an error.
    /// </summary>
    /// <param name="fullyQualifiedPropertyName">The fully qualified property name.</param>
    /// <returns><see langword="true"/> where an error exists for the specified property; otherwise, <see langword="false"/>.</returns>
    public bool HasError(string fullyQualifiedPropertyName) => _errorProperties is not null && _errorProperties.Contains(fullyQualifiedPropertyName);

    /// <summary>
    /// Gets (creates) the messages collection.
    /// </summary>
    private MessageItemCollection GetMessages()
    {
        if (Messages is null)
        {
            Messages = [];
            Messages.CollectionChanged += Messages_CollectionChanged;
        }

        return Messages;
    }

    /// <summary>
    /// Handle the add of a message.
    /// </summary>
    private void Messages_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                foreach (var m in e.NewItems!)
                {
                    MessageItem mi = (MessageItem)m;
                    if (mi.Type == MessageType.Error)
                    {
                        HasErrors = true;
                        if (mi is ValidationMessageItem vmi && vmi.FullyQualifiedPropertyName is not null)
                            (_errorProperties ??= []).Add(vmi.FullyQualifiedPropertyName);
                    }
                }

                break;

            default:
                throw new InvalidOperationException("Operation invalid for Messages; only add supported.");
        }
    }

    /// <inheritdoc/>
    public Exception? ToException() => HasErrors ? new ValidationException(Messages!) : null;

    /// <inheritdoc/>
    IValidationResult IValidationResult.ThrowOnError() => ThrowOnError();

    /// <summary>
    /// Throws a <see cref="ValidationException"/> where an error was found (and optionally any warnings).
    /// </summary>
    /// <returns>The <see cref="ValidationContext{TEntity}"/> to support fluent-style method-chaining.</returns>
    public ValidationContext<TEntity> ThrowOnError()
    {
        var ex = ToException();
        if (ex is not null)
            throw ex;

        return this;
    }

    /// <summary>
    /// Merges a <paramref name="validationResult"/> into this.
    /// </summary>
    /// <param name="validationResult">The <see cref="IValidationResult"/> to merge.</param>
    internal void MergeResult(IValidationResult? validationResult)
    {
        if (validationResult?.Messages is not null && validationResult.Messages.Count > 0)
            GetMessages().AddRange(validationResult.Messages);
    }

    /// <inheritdoc/>
    public Result ToResult() => HasErrors ? Result.ValidationError(Messages!) : Result.Success;

    /// <summary>
    /// Executes a <i>further</i> <paramref name="validator"/> for the <see cref="Value"/>.
    /// </summary>
    /// <param name="validator">The <see cref="IValidatorEx{T}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>This is useful in scenarios where <i>further</i> validation needs to occur for the <see cref="Value"/>, that needs an <see cref="IValidatorEx{T}"/> to be constructed dynamically at runtime with the same
    /// <see cref="ValidationContext{TEntity}"/>, that is ostensibly a continuation/extension of any existing validation for that value.</remarks>
    public Task ValidateFurtherAsync(IValidatorEx<TEntity> validator, CancellationToken cancellationToken = default) => validator.ThrowIfNull().ValidateAsync(this, cancellationToken);

    /// <summary>
    /// Executes a <i>further</i> dynamically created <see cref="Validator{TEntity}"/> for the <see cref="Value"/> that enables further configuration.
    /// </summary>
    /// <param name="configure">An action to configure the <see cref="Validator{TEntity}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>This is useful in scenarios where <i>further</i> validation needs to occur for the <see cref="Value"/>, that needs an <see cref="IValidatorEx{T}"/> to be constructed dynamically at runtime with the same
    /// <see cref="ValidationContext{TEntity}"/>, that is ostensibly a continuation/extension of any existing validation for that value.</remarks>
    public Task ValidateFurtherAsync(Action<Validator<TEntity>> configure, CancellationToken cancellationToken = default)
    {
        if (configure is null)
            return Task.CompletedTask;

        var v = Validator.Create<TEntity>();
        configure(v);
        return ValidateFurtherAsync(v, cancellationToken);
    }
}