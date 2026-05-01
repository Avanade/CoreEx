namespace CoreEx;

/// <summary>
/// Represents a thread-bound (request) execution context using <see cref="AsyncLocal{ExecutionContext}"/>.
/// </summary>
/// <remarks>Used to house/pass context parameters and capabilities that are outside of the general operation arguments. This class should be extended by consumers where additional properties are required.
/// <para>The <see cref="ExecutionContext"/> implements <see cref="IDisposable"/>; however, from a standard implementation perspective there are no unmanaged resources leveraged. The <see cref="Dispose()"/> will result in a <see cref="Reset"/>.</para></remarks>
public partial class ExecutionContext : IDisposable, IReadOnlyTenantId
{
    private DateTimeOffset? _timestamp;
    private MessageItemCollection? _messages;
    private Lazy<ConcurrentDictionary<string, object?>> _attributes = new(true);
    private bool _isCopied;

    /// <summary>
    /// Gets or sets the <see cref="IServiceProvider"/>.
    /// </summary>
    public IServiceProvider? ServiceProvider { get; set; }

    /// <inheritdoc/>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp (<see cref="TimeProvider.GetUtcNow"/>) for the <see cref="ExecutionContext"/> lifetime; i.e (to enable consistent execution-related timestamping).
    /// </summary>
    /// <remarks>This value is intended to remain unchanged for the life of the <see cref="ExecutionContext"/> to ensure consistency of the value.</remarks>
    public DateTimeOffset Timestamp { get => _timestamp ??= ServiceProvider?.GetService<TimeProvider>()?.GetUtcNow() ?? TimeProvider.System.GetUtcNow(); set => _timestamp = value; }

    /// <summary>
    /// Gets the execution <see cref="MessageItemCollection"/> that should generally be returned to the end-consumer.
    /// </summary>
    public MessageItemCollection? Messages => _messages;

    /// <summary>
    /// Indicates whether there are any <see cref="Messages"/>.
    /// </summary>
    public bool HasMessages => _messages is not null && _messages.Count > 0;

    /// <summary>
    /// Gets the additional execution context attributes.
    /// </summary>
    public ConcurrentDictionary<string, object?> Attributes { get => _attributes.Value; }

    /// <summary>
    /// Gets or sets the corresponding <see cref="AuthenticationUser"/>.
    /// </summary>
    public AuthenticationUser User { get; set => field = value.ThrowIfNull(); } = AuthenticationUser.EnvironmentUser;

    /// <summary>
    /// Gets or sets the UI <see cref="CultureInfo"/>.
    /// </summary>
    public CultureInfo UICulture { get; set; } = CultureInfo.CurrentUICulture;

    /// <summary>
    /// Indicates whether <see cref="HttpNames.IncludeTextQueryStringName"/> is specified within the current request to include related text(s) where available.
    /// </summary>
    public bool IncludeRelatedText { get; set; }

    /// <summary>
    /// Gets or sets the corresponding <see href="https://en.wikipedia.org/wiki/Create,_read,_update_and_delete">CRUD</see> operation type (Create, Read, Update and Delete).
    /// </summary>
    public OperationType OperationType { get; set; } = OperationType.Unspecified;

    /// <summary>
    /// Adds a <see cref="MessageType.Warning"/> message to the <see cref="Messages"/>.
    /// </summary>
    /// <param name="text">The message text.</param>
    /// <returns>The <see cref="ExecutionContext"/> to support fluent-style method-chaining.</returns>
    public ExecutionContext AddWarningMessage(LText text)
    {
        (_messages ??= []).Add(new MessageItem(MessageType.Warning, text));
        return this;
    }

    /// <summary>
    /// Adds a <see cref="MessageType.Info"/> message to the <see cref="Messages"/>.
    /// </summary>
    /// <param name="text">The message text.</param>
    /// <returns>The <see cref="ExecutionContext"/> to support fluent-style method-chaining.</returns>
    public ExecutionContext AddInfoMessage(LText text)
    {
        (_messages ??= []).Add(new MessageItem(MessageType.Info, text));
        return this;
    }

    /// <summary>
    /// Indicates whether this instance was created as a result of a <see cref="CreateCopy"/> operation.
    /// </summary>
    public bool IsACopy => _isCopied;

    /// <summary>
    /// Creates a copy of the <see cref="ExecutionContext"/> using the <see cref="Create"/> function to instantiate before copying or referencing all underlying properties.
    /// </summary>
    /// <returns>The new <see cref="ExecutionContext"/> instance.</returns>
    /// <remarks>This is intended for <b>advanced scenarios</b> and may have unintended consequences where not used correctly.
    /// <i>Note:</i> the <see cref="User"/>, <see cref="Messages"/> and <see cref="Attributes"/> share the same instance, i.e. are not copied. The <see cref="IsACopy"/> is updated accordingly to indicate the current copy state.</remarks>
    public virtual ExecutionContext CreateCopy()
    {
        var ec = Create is null ? throw new InvalidOperationException($"The {nameof(Create)} function must not be null to create a copy.") : Create();
        ec._timestamp = _timestamp;
        ec._messages = _messages;
        ec.ServiceProvider = ServiceProvider;
        ec.User = User;
        ec.TenantId = TenantId;
        ec.UICulture = UICulture;
        ec._isCopied = true;

        if (_attributes.IsValueCreated)
            ec._attributes = new Lazy<ConcurrentDictionary<string, object?>>(new ConcurrentDictionary<string, object?>(_attributes.Value));

        return ec;
    }
}