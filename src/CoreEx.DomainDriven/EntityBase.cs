namespace CoreEx.DomainDriven;

/// <summary>
/// Provides the base <see href="https://en.wikipedia.org/wiki/Domain-driven_design">domain-driven</see> entity functionality.
/// </summary>
public abstract class EntityBase : IEntity
{
    /// <summary>
    /// Gets the read-only error message.
    /// </summary>
    public const string ReadOnlyErrorMessage = "The operation is not valid due to the current state being read-only.";

    /// <inheritdoc/>
    object? IIdentifierCore.Id => throw new NotImplementedException();

    /// <inheritdoc/>
    [JsonIgnore]
    public abstract CompositeKey EntityKey { get; }

    /// <inheritdoc/>
    [JsonIgnore]
    Type IIdentifierCore.IdType => throw new NotImplementedException();

    /// <inheritdoc/>
    [JsonIgnore]
    bool IIdentifierCore.IsIdReadOnly => true;

    /// <inheritdoc/>
    void IIdentifierCore.SetIdentifier(object? id) => throw new InvalidOperationException("Identifier is read-only.");

    /// <inheritdoc/>
    [JsonIgnore]
    public bool IsReadOnly { get; private set; }

    /// <inheritdoc/>
    [JsonIgnore]
    public PersistenceState PersistenceState { get; private set; }

    /// <summary>
    /// Gets the <see cref="ChangeLog"/>.
    /// </summary>
    public ChangeLog? ChangeLog { get; protected set; }

    /// <summary>
    /// Gets the entity tag.
    /// </summary>
    public string? ETag { get; protected set; }

    /// <summary>
    /// Sets (overrides) the <see cref="PersistenceState"/>.
    /// </summary>
    /// <param name="state">The new <see cref="DomainDriven.PersistenceState"/>.</param>
    /// <remarks>This method does not check <see cref="IsReadOnly"/> by design as this is considered independent to.</remarks>
    protected void SetPersistenceState(PersistenceState state)
    {
        // Nothing doing!
        if (state == PersistenceState)
            return;

        // Validate state transition.
        state.ThrowWhen(state => state == PersistenceState.Unknown, $"The {nameof(PersistenceState)} cannot be set to '{PersistenceState.Unknown}'.");
        state.ThrowWhen(state => state == PersistenceState.NotModified && PersistenceState != PersistenceState.Unknown, $"The {nameof(PersistenceState)} can only be set to '{PersistenceState.NotModified}' from '{PersistenceState.Unknown}' state.");

        // Transition to the new state.
        PersistenceState = state;
    }

    /// <summary>
    /// Encapsulates modification to the entity.
    /// </summary>
    /// <param name="action">The action that performs the entity modification.</param>
    /// <remarks>Wraps the invocation of the <paramref name="action"/> by performing the following:
    ///  <list type="bullet">
    ///   <item><see cref="CheckCanMutate"/>.</item>
    ///   <item><see cref="CheckReadOnly"/>.</item>
    ///   <item>Invokes the <paramref name="action"/>.</item>
    ///   <item>Sets to <see cref="PersistenceState.Modified"/> (where <see cref="PersistenceState.NotModified"/>).</item>
    ///   <item><see cref="OnMutate"/>.</item>
    ///   <item>Raises <see cref="Mutated"/> event.</item>
    ///  </list>
    /// </remarks>
    protected void Modify(Action? action = null)
    {
        CheckCanMutate();
        CheckReadOnly();

        action?.Invoke();

        if (PersistenceState.IsNotModified)
            SetPersistenceState(PersistenceState.Modified);

        OnMutate();
        Mutated?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Encapsulates modification to the entity with a corresponding result.
    /// </summary>
    /// <typeparam name="TResult">The result <see cref="Type"/>.</typeparam>
    /// <param name="function">The function that performs the entity modification.</param>
    /// <returns>The resulting value.</returns>
    /// <remarks>Wraps the invocation of the <paramref name="function"/> by performing the following:
    ///  <list type="bullet">
    ///   <item><see cref="CheckCanMutate"/>.</item>
    ///   <item><see cref="CheckReadOnly"/>.</item>
    ///   <item>Invokes the <paramref name="function"/>.</item>
    ///   <item>Sets to <see cref="PersistenceState.Modified"/> (where <see cref="PersistenceState.NotModified"/>).</item>
    ///   <item><see cref="OnMutate"/>.</item>
    ///   <item>Raises <see cref="Mutated"/> event.</item>
    ///  </list>
    /// </remarks>
    protected TResult Modify<TResult>(Func<TResult> function)
    {
        CheckCanMutate();
        CheckReadOnly();

        var result = function.ThrowIfNull()();

        if (PersistenceState.IsNotModified)
            SetPersistenceState(PersistenceState.Modified);

        OnMutate();
        Mutated?.Invoke(this, EventArgs.Empty);

        return result;
    }

    /// <summary>
    /// Encapsulates modification to the entity, finally setting to read-only.
    /// </summary>
    /// <param name="action">The optional action that performs the entity modification.</param>
    /// <remarks>Wraps the invocation of the <paramref name="action"/> by performing the following:
    ///  <list type="bullet">
    ///   <item><see cref="CheckCanMutate"/>.</item>
    ///   <item><see cref="CheckReadOnly"/>.</item>
    ///   <item>Invokes the <paramref name="action"/>.</item>
    ///   <item>Sets to <see cref="PersistenceState.Modified"/> (where <see cref="PersistenceState.NotModified"/>).</item>
    ///   <item><see cref="OnMutate"/>.</item>
    ///   <item>Raises <see cref="Mutated"/> event.</item>
    ///   <item><see cref="MakeReadOnly"/>.</item>
    ///  </list>
    /// </remarks>
    protected void ModifyAndMakeReadOnly(Action? action = null)
    {
        Modify(action);
        MakeReadOnly();
    }

    /// <summary>
    /// Encapsulates modification to the entity with a corresponding result, finally setting to read-only.
    /// </summary>
    /// <typeparam name="TResult">The result <see cref="Type"/>.</typeparam>
    /// <param name="function">The function that performs the entity modification.</param>
    /// <returns>The resulting value.</returns>
    /// <remarks>Wraps the invocation of the <paramref name="function"/> by performing the following:
    ///  <list type="bullet">
    ///   <item><see cref="CheckCanMutate"/>.</item>
    ///   <item><see cref="CheckReadOnly"/>.</item>
    ///   <item>Invokes the <paramref name="function"/>.</item>
    ///   <item>Sets to <see cref="PersistenceState.Modified"/> (where <see cref="PersistenceState.NotModified"/>).</item>
    ///   <item><see cref="OnMutate"/>.</item>
    ///   <item>Raises <see cref="Mutated"/> event.</item>
    ///   <item><see cref="MakeReadOnly"/>.</item>
    ///  </list>
    /// </remarks>
    protected TResult ModifyAndMakeReadOnly<TResult>(Func<TResult> function)
    {
        var result = Modify(function);
        MakeReadOnly();
        return result;
    }

    /// <summary>
    /// Encapsulates marking for removal (deletion) of the entity, finally setting to read-only.
    /// </summary>
    /// <param name="action">The optional action that performs any entity modification.</param>
    /// <remarks>Wraps the invocation of the <paramref name="action"/> by performing the following:
    ///  <list type="bullet">
    ///   <item><see cref="CheckCanMutate"/>.</item>
    ///   <item><see cref="CheckReadOnly"/>.</item>
    ///   <item>Invokes the optional <paramref name="action"/>.</item>
    ///   <item>Sets to <see cref="PersistenceState.Removed"/>.</item>
    ///   <item><see cref="OnMutate"/>.</item>
    ///   <item>Raises <see cref="Mutated"/> event.</item>
    ///   <item><see cref="MakeReadOnly"/>.</item>
    ///  </list>
    /// </remarks>
    protected void Remove(Action? action = null)
    {
        CheckCanMutate();
        CheckReadOnly();

        action?.Invoke();

        SetPersistenceState(PersistenceState.Removed);

        OnMutate();
        MakeReadOnly();
        Mutated?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Encapsulates marking for removal (deletion) of the entity, finally setting to read-only.
    /// </summary>
    /// <typeparam name="TResult">The result <see cref="Type"/>.</typeparam>
    /// <param name="function">The function that performs any entity modification.</param>
    /// <returns>The resulting value.</returns>
    /// <remarks>Wraps the invocation of the <paramref name="function"/> by performing the following:
    ///  <list type="bullet">
    ///   <item><see cref="CheckCanMutate"/>.</item>
    ///   <item><see cref="CheckReadOnly"/>.</item>
    ///   <item>Invokes the <paramref name="function"/>.</item>
    ///   <item>Sets to <see cref="PersistenceState.Removed"/>.</item>
    ///   <item><see cref="OnMutate"/>.</item>
    ///   <item>Raises <see cref="Mutated"/> event.</item>
    ///   <item><see cref="MakeReadOnly"/>.</item>
    ///  </list>
    /// </remarks>
    protected TResult Remove<TResult>(Func<TResult> function)
    {
        CheckCanMutate();
        CheckReadOnly();

        var result = function.ThrowIfNull()();

        SetPersistenceState(PersistenceState.Removed);

        OnMutate();
        Mutated?.Invoke(this, EventArgs.Empty);
        MakeReadOnly();

        return result;
    }

    /// <summary>
    /// Makes the entity read-only.
    /// </summary>
    /// <remarks>See <see cref="IsReadOnly"/>.</remarks>
    protected void MakeReadOnly() => IsReadOnly = true;

    /// <summary>
    /// Checks whether the entity <see cref="IsReadOnly"/> and if so throws an <see cref="InvalidOperationException"/>.
    /// </summary>
    protected void CheckReadOnly()
    {
        if (IsReadOnly)
            throw new InvalidOperationException(ReadOnlyErrorMessage);
    }

    /// <summary>
    /// Checkes whether the entity can be mutated by invoking <see cref="OnCheckCanMutate"/> and throwing on error.
    /// </summary>
    protected void CheckCanMutate() => OnCheckCanMutate().ThrowOnError();

    /// <summary>
    /// Provides an opportunity to perform pre-checks prior to mutation of the entity; i.e. within the <see cref="Modify(Action?)"/> and <see cref="Remove(Action?)"/> methods.
    /// </summary>
    /// <returns>A <see cref="Result"/> indicating the outcome of the pre-checks.</returns>
    /// <remarks>This is invoked by <see cref="CheckCanMutate"/>.</remarks>
    protected virtual Result OnCheckCanMutate() => Result.Success;

    /// <summary>
    /// Provides an opportunity to perform additional actions after mutation of the entity; i.e. within the <see cref="Modify(Action?)"/> and <see cref="Remove(Action?)"/> methods.
    /// </summary>
    /// <remarks>This is invoked by the <see cref="Modify(Action?)"/> and <see cref="Remove(Action?)"/> methods.</remarks>
    protected virtual void OnMutate() { }

    /// <summary>
    /// Occurs when the entity is mutated.
    /// </summary>
    public event EventHandler? Mutated;

    /// <summary>
    /// Sets (overrides) the <see cref="ChangeLog"/>.
    /// </summary>
    /// <param name="changeLog">The <see cref="Entities.ChangeLog"/>.</param>
    /// <remarks>Bypasses <see cref="IsReadOnly"/> checking and will not result in an <see cref="PersistenceState"/> change by design; intended to enable setting during hydration from a data source.</remarks>
    public void SetChangeLog(ChangeLog? changeLog) => ChangeLog = changeLog;

    /// <summary>
    /// Sets (overrides) the <see cref="ETag"/>.
    /// </summary>
    /// <param name="eTag">The entity tag.</param>
    /// <remarks>Bypasses <see cref="IsReadOnly"/> checking and will not result in an <see cref="PersistenceState"/> change by design; intended to enable setting during hydration from a data source.</remarks>
    public void SetETag(string? eTag) => ETag = eTag;

    /// <inheritdoc/>
    public override string? ToString() => ((IEntityKey)this).EntityKey.ToString();
}