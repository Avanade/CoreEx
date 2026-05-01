namespace CoreEx;

public partial class ExecutionContext
{
    private static readonly AsyncLocal<ExecutionContext?> _asyncLocal = new();

    private bool _disposed;
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif

    /// <summary>
    /// Gets or sets the function to create a default <see cref="ExecutionContext"/> instance.
    /// </summary>
    public static Func<ExecutionContext>? Create { get; set; } = () => new();

    /// <summary>
    /// Indicates whether the <see cref="ExecutionContext"/> <see cref="Current"/> has a value.
    /// </summary>
    public static bool HasCurrent => _asyncLocal.Value is not null;

    /// <summary>
    /// Gets the current <see cref="ExecutionContext"/> for the executing thread graph (see <see cref="AsyncLocal{T}"/>).
    /// </summary>
    /// <remarks>Where not previously set (see <see cref="SetCurrent(ExecutionContext?)"/>) then the <see cref="Create"/> will be invoked as a backup to create an instance on first access. 
    /// <para>The <see cref="Reset"/> should be used to dispose and clear the current where no longer needed.</para>
    /// <para>Finally, where no current instance a <see cref="InvalidOperationException"/> will be thrown.</para></remarks>
    public static ExecutionContext Current => _asyncLocal.Value ??= Create?.Invoke() ??
        throw new InvalidOperationException("There is currently no ExecutionContext.Current instance; this must be set (SetCurrent) prior to access. Use ExecutionContext.HasCurrent to verify value and avoid this exception if appropriate.");

    /// <summary>
    /// Tries to get the current <see cref="ExecutionContext"/> for the executing thread graph (see <see cref="AsyncLocal{T}"/>).
    /// </summary>
    /// <param name="executionContext">The <see cref="Current"/> <see cref="ExecutionContext"/> where <see cref="HasCurrent"/>; otherwise, <see langword="null"/>.</param>
    /// <param name="throwWhereNull">Indicates whether to throw an <see cref="InvalidOperationException"/> when there is no current instance.</param>
    /// <returns><see langword="true"/> when <see cref="HasCurrent"/>; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetCurrent([NotNullWhen(true)] out ExecutionContext? executionContext, bool throwWhereNull = false)
    {
        executionContext = _asyncLocal.Value;

        if (executionContext is null && throwWhereNull)
            throw new InvalidOperationException("There is currently no ExecutionContext.Current instance; this must be set (SetCurrent) prior to access.");

        return executionContext is not null;
    }

    /// <summary>
    /// Resets (clears) the <see cref="Current"/> <see cref="ExecutionContext"/>.
    /// </summary>
    public static void Reset()
    {
        if (TryGetCurrent(out var executionContext))
            executionContext.Dispose();

        _asyncLocal.Value = null;
    }

    /// <summary>
    /// Sets the <see cref="Current"/> instance (only allowed where <see cref="HasCurrent"/> is <see langword="false"/>).
    /// </summary>
    /// <param name="executionContext">The <see cref="ExecutionContext"/> instance.</param>
    public static void SetCurrent(ExecutionContext executionContext)
    {
        if (HasCurrent)
            throw new InvalidOperationException("The SetCurrent method can only be used where there is no Current instance.");

        _asyncLocal.Value = executionContext.ThrowIfNull();
    }

    /// <summary>
    /// Gets the service of <see cref="Type"/> <typeparamref name="T"/> from the <see cref="Current"/> <see cref="ServiceProvider"/>.
    /// </summary>
    /// <typeparam name="T">The service <see cref="Type"/>.</typeparam>
    /// <returns>The corresponding instance.</returns>
    public static T? GetService<T>()
    {
        if (TryGetCurrent(out var executionContext) && executionContext.ServiceProvider is not null)
            return executionContext.ServiceProvider.GetService<T>();

        return default;
    }

    /// <summary>
    /// Gets the service of <see cref="Type"/> <typeparamref name="T"/> from the <see cref="Current"/> <see cref="ServiceProvider"/> using the <paramref name="serviceKey"/> .
    /// </summary>
    /// <typeparam name="T">The service <see cref="Type"/>.</typeparam>
    /// <param name="serviceKey">The service key.</param>
    /// <returns>The corresponding instance.</returns>
    public static T? GetKeyedService<T>(object? serviceKey)
    {
        if (TryGetCurrent(out var executionContext) && executionContext.ServiceProvider is not null)
            return executionContext.ServiceProvider.GetKeyedService<T>(serviceKey);

        return default;
    }

    /// <summary>
    /// Gets the service of <see cref="Type"/> <typeparamref name="T"/> from the <see cref="Current"/> <see cref="ServiceProvider"/> and will throw an <see cref="InvalidOperationException"/> where not found.
    /// </summary>
    /// <typeparam name="T">The service <see cref="Type"/>.</typeparam>
    /// <returns>The corresponding instance.</returns>
    public static T GetRequiredService<T>() where T : notnull
    {
        if (TryGetCurrent(out var executionContext) && executionContext.ServiceProvider is not null)
            return executionContext.ServiceProvider.GetRequiredService<T>();

        throw new InvalidOperationException($"Attempted to get service '{typeof(T).FullName}' but there is either no ExecutionContext.Current or the ExecutionContext.ServiceProvider has not been configured.");
    }

    /// <summary>
    /// Gets the service of <see cref="Type"/> <typeparamref name="T"/> from the <see cref="Current"/> <see cref="ServiceProvider"/> using the <paramref name="serviceKey"/> and will throw an <see cref="InvalidOperationException"/> where not found.
    /// </summary>
    /// <typeparam name="T">The service <see cref="Type"/>.</typeparam>
    /// <param name="serviceKey">The service key.</param>
    /// <returns>The corresponding instance.</returns>
    public static T GetRequiredKeyedService<T>(object? serviceKey) where T : notnull
    {
        if (TryGetCurrent(out var executionContext) && executionContext.ServiceProvider is not null)
            return executionContext.ServiceProvider.GetRequiredKeyedService<T>(serviceKey);

        throw new InvalidOperationException($"Attempted to get service '{typeof(T).FullName}' but there is either no ExecutionContext.Current or the ExecutionContext.ServiceProvider has not been configured.");
    }

    /// <summary>
    /// Gets the service of <see cref="Type"/> <paramref name="type"/> from the <see cref="Current"/> <see cref="ServiceProvider"/>.
    /// </summary>
    /// <param name="type">The service <see cref="Type"/>.</param>
    /// <returns>The corresponding instance.</returns>
    public static object? GetService(Type type)
    {
        type.ThrowIfNull();
        if (TryGetCurrent(out var executionContext) && executionContext.ServiceProvider is not null)
            return executionContext.ServiceProvider.GetService(type);

        return null;
    }

    /// <summary>
    /// Gets the service of <see cref="Type"/> <paramref name="type"/> from the <see cref="Current"/> <see cref="ServiceProvider"/> using the <paramref name="serviceKey"/> .
    /// </summary>
    /// <param name="type">The service <see cref="Type"/>.</param>
    /// <param name="serviceKey">The service key.</param>
    /// <returns>The corresponding instance.</returns>
    public static object? GetKeyedService(Type type, object? serviceKey)
    {
        type.ThrowIfNull();
        if (TryGetCurrent(out var executionContext) && executionContext.ServiceProvider is not null)
            return executionContext.ServiceProvider.GetKeyedServices(type, serviceKey).FirstOrDefault(s => s?.GetType() == type);

        return null;
    }

    /// <summary>
    /// Gets the service of <see cref="Type"/> <paramref name="type"/> from the <see cref="Current"/> <see cref="ServiceProvider"/> and will throw an <see cref="InvalidOperationException"/> where not found.
    /// </summary>
    /// <param name="type">The service <see cref="Type"/>.</param>
    /// <returns>The corresponding instance.</returns>
    public static object GetRequiredService(Type type)
    {
        type.ThrowIfNull();
        if (TryGetCurrent(out var executionContext) && executionContext.ServiceProvider is not null)
            return executionContext.ServiceProvider.GetRequiredService(type);

        throw new InvalidOperationException($"Attempted to get service '{type.FullName}' but there is either no ExecutionContext.Current or the ExecutionContext.ServiceProvider has not been configured.");
    }

    /// <summary>
    /// Gets the service of <see cref="Type"/> <paramref name="type"/> from the <see cref="Current"/> <see cref="ServiceProvider"/> using the <paramref name="serviceKey"/> and will throw an <see cref="InvalidOperationException"/> where not found.
    /// </summary>
    /// <param name="type">The service <see cref="Type"/>.</param>
    /// <param name="serviceKey">The service key.</param>
    /// <returns>The corresponding instance.</returns>
    public static object GetRequiredKeyedService(Type type, object? serviceKey)
    {
        type.ThrowIfNull();
        if (TryGetCurrent(out var executionContext) && executionContext.ServiceProvider is not null)
            return executionContext.ServiceProvider.GetRequiredKeyedService(type, serviceKey);

        throw new InvalidOperationException($"Attempted to get service '{type.FullName}' but there is either no ExecutionContext.Current or the ExecutionContext.ServiceProvider has not been configured.");
    }

    /// <summary>
    /// Gets the related <paramref name="text"/> where the <see cref="Current"/> <see cref="IncludeRelatedText"/> is <see langword="true"/>; otherwise, returns <see langword="null"/>.
    /// </summary>
    /// <param name="text">The text function that is <i>only</i> executed where <see cref="IncludeRelatedText"/> is <see langword="true"/>.</param>
    public static string? GetRelatedText(Func<string?> text) => TryGetCurrent(out var ec) && ec.IncludeRelatedText ? text.ThrowIfNull()() : null;

    /// <summary>
    /// Dispose of resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="ExecutionContext"/> and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            lock (_lock)
            {
                if (!_disposed)
                {
                    if (_asyncLocal.Value == this)
                        _asyncLocal.Value = null;

                    _disposed = true;
                    ServiceProvider = null;
                }
            }
        }
    }
}