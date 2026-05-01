namespace CoreEx.RefData;

/// <summary>
/// Provides the centralized reference data orchestration. Primarily responsible for the management of one or more <see cref="IReferenceDataProvider"/> instances.  
/// </summary>
/// <remarks>Provides <i>cached</i> access to the underlying reference data collections via the likes of <see cref="GetByTypeAsync{TRef}"/>, <see cref="GetByTypeAsync(Type, CancellationToken)"/> or <see cref="GetByNameAsync(string, CancellationToken)"/>.
/// <para>To improve performance the reference data <i>should</i> be cached; this is enabled using the <see cref="IReferenceDataCache"/>. The underlying reference data loading is executed in the context of a <see cref="ServiceProviderServiceExtensions.CreateScope(IServiceProvider)"/> 
/// to limit/minimize any impact on the processing of the current request by isolating all scoped services.</para></remarks>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/> needed to instantiate the registered providers, etc.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
public sealed class ReferenceDataOrchestrator(IServiceProvider serviceProvider, ILogger<ReferenceDataOrchestrator> logger)
{
    /// <summary>
    /// Gets the error message where the <see cref="IReferenceData.Text"/> <see cref="Wildcard"/> value is invalid.
    /// </summary>
    public const string TextWildcardErrorMessage = "Text contains invalid or unsupported wildcard selection.";

    private const string InvokerCacheType = "refdata.cachetype";
    private const string InvokerCacheState = "refdata.cachestate";
    private const string InvokerCacheCount = "refdata.cachecount";

    private static readonly AsyncLocal<ReferenceDataOrchestrator?> _asyncLocal = new();
    private readonly ReferenceDataOrchestratorInvoker _invoker = serviceProvider?.GetService<ReferenceDataOrchestratorInvoker>() ?? new();

#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private readonly ConcurrentDictionary<Type, Type> _typeToProvider = new();
    private readonly ConcurrentDictionary<string, Type> _nameToType = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<Type, Type> _typeToCollType = new();

    /// <summary>
    /// Tries to get the current <see cref="ReferenceDataOrchestrator"/> for the executing thread graph (see <see cref="AsyncLocal{T}"/>), or if not set, the <see cref="ExecutionContext"/> <see cref="IServiceProvider"/> service is used.
    /// </summary>
    /// <param name="referenceDataOrchestrator">The <see cref="Current"/> <see cref="ReferenceDataOrchestrator"/> where <see cref="HasCurrent"/>; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when <see cref="HasCurrent"/>; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetCurrent([NotNullWhen(true)] out ReferenceDataOrchestrator? referenceDataOrchestrator)
    {
        referenceDataOrchestrator = _asyncLocal.Value ??= ExecutionContext.GetService<ReferenceDataOrchestrator>();
        return referenceDataOrchestrator is not null;
    }

    /// <summary>
    /// Gets the current <see cref="ReferenceDataOrchestrator"/> for the executing thread graph (see <see cref="AsyncLocal{T}"/>), or if not set, the <see cref="ExecutionContext"/> <see cref="IServiceProvider"/> service is used.
    /// </summary>
    public static ReferenceDataOrchestrator Current => TryGetCurrent(out var rdo) ? rdo : throw new InvalidOperationException($"Unable to get an instance of the {nameof(ReferenceDataOrchestrator)} from the {nameof(ExecutionContext)}; the {nameof(ReferenceDataOrchestrator)} must be registered as a singleton service");

    /// <summary>
    /// Indicates whether the <see cref="ReferenceDataOrchestrator"/> <see cref="Current"/> has a value.
    /// </summary>
    public static bool HasCurrent => TryGetCurrent(out var _);

    /// <summary>
    /// Sets (or overrides) the <see cref="Current"/> instance.
    /// </summary>
    /// <param name="orchestrator">The <see cref="ReferenceDataOrchestrator"/>.</param>
    public static void SetCurrent(ReferenceDataOrchestrator? orchestrator = null) => _asyncLocal.Value = orchestrator;

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/>.
    /// </summary>
    public IServiceProvider ServiceProvider { get; } = serviceProvider.ThrowIfNull();

    /// <summary>
    /// Gets the <see cref="ILogger"/>.
    /// </summary>
    public ILogger Logger { get; } = logger.ThrowIfNull();

    /// <summary>
    /// Gets or sets the <see cref="ParallelOptions.MaxDegreeOfParallelism"/> to use when performing a <see cref="PrefetchAsync(IEnumerable{string}, CancellationToken)"/>.
    /// </summary>
    /// <remarks>Defaults to <c>2</c> to minimize potential impact</remarks>
    public int PrefetchMaxDegreeOfParallelism { get; set; } = 2;

    /// <summary>
    /// Registers the <see cref="IReferenceDataProvider"/> <see cref="Type"/>.
    /// </summary>
    /// <returns>The <see cref="ReferenceDataOrchestrator"/> to support fluent-style method-chaining.</returns>
    /// <remarks>Internally this builds the relationship between the <see cref="IReferenceDataProvider.Types"/> and the owning <see cref="IReferenceDataProvider"/> to enable cached access to the underlying 
    /// <see cref="IReferenceDataCollection"/> using <see cref="GetByTypeAsync(Type, CancellationToken)"/> or <see cref="this[Type]"/>.</remarks>
    public ReferenceDataOrchestrator Register() => Register<IReferenceDataProvider>();

    /// <summary>
    /// Registers an <see cref="IReferenceDataProvider"/> <see cref="Type"/>.
    /// </summary>
    /// <typeparam name="TProvider">The <see cref="IReferenceDataProvider"/> to register.</typeparam>
    /// <returns>The <see cref="ReferenceDataOrchestrator"/> to support fluent-style method-chaining.</returns>
    /// <remarks>Internally this builds the relationship between the <see cref="IReferenceDataProvider.Types"/> and the owning <see cref="IReferenceDataProvider"/> to enable cached access to the underlying 
    /// <see cref="IReferenceDataCollection"/> using <see cref="GetByTypeAsync(Type, CancellationToken)"/> or <see cref="this[Type]"/>.</remarks>
    public ReferenceDataOrchestrator Register<TProvider>() where TProvider : IReferenceDataProvider
    {
        using var scope = ServiceProvider.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<TProvider>();

        foreach (var (refType, collType) in provider.Types)
        {
            // Lock to ensure that the two internal dictionaries (together) are updated in a thread-safe manner
            lock (_lock)
            {
                if (_nameToType.ContainsKey(refType.Name))
                    throw new InvalidOperationException($"Type '{refType.FullName}' cannot be added as name '{refType.Name}' already associated with previously added Type '{_nameToType.GetValueOrDefault(refType.Name)?.FullName}'.");

                if (!_typeToProvider.TryAdd(refType, typeof(TProvider)))
                    throw new InvalidOperationException($"Type '{refType.FullName}' cannot be added as already associated with previously added Provider '{_typeToProvider.GetValueOrDefault(refType)?.GetType().FullName}'.");

                _nameToType.TryAdd(refType.Name, refType);
                _typeToCollType.TryAdd(refType, collType);
            }
        }

        return this;
    }

    /// <summary>
    /// Determines whether the <see cref="ReferenceDataOrchestrator"/> contains the specified <see cref="IReferenceData"/> <see cref="Type"/>.
    /// </summary>
    /// <typeparam name="TRef">The <see cref="IReferenceData"/> <see cref="Type"/>.</typeparam>
    /// <returns><see langword="true"/> indicates that it exists; otherwise, <see langword="false"/>.</returns>
    public bool ContainsType<TRef>() where TRef : IReferenceData => ContainsType(typeof(TRef));

    /// <summary>
    /// Determines whether the <see cref="ReferenceDataOrchestrator"/> contains the specified <see cref="IReferenceData"/> <see cref="Type"/>.
    /// </summary>
    /// <param name="type">The <see cref="IReferenceData"/> <see cref="Type"/>.</param>
    /// <returns><see langword="true"/> indicates that it exists; otherwise, <see langword="false"/>.</returns>
    public bool ContainsType(Type type) => _typeToProvider.ContainsKey(type);

    /// <summary>
    /// Determines whether the <see cref="ReferenceDataOrchestrator"/> contains the specified <see cref="IReferenceData"/> name (see <see cref="IReferenceData"/> <see cref="Type"/> <see cref="System.Reflection.MemberInfo.Name"/>). 
    /// </summary>
    /// <param name="name">The reference data name.</param>
    /// <returns><see langword="true"/> indicates that it exists; otherwise, <see langword="false"/>.</returns>
    public bool ContainsName(string name) => _nameToType.ContainsKey(name);

    /// <summary>
    /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> <see cref="Type"/> synchronously. 
    /// </summary>
    /// <param name="type">The <see cref="IReferenceData"/> <see cref="Type"/>.</param>
    /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, <see langword="null"/>.</returns>
    public IReferenceDataCollection? this[Type type] => GetByType(type);

    /// <summary>
    /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> name (see <see cref="IReferenceData"/> <see cref="Type"/> <see cref="System.Reflection.MemberInfo.Name"/>) synchronously. 
    /// </summary>
    /// <param name="name">The reference data name.</param>
    /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, <see langword="null"/>.</returns>
    public IReferenceDataCollection? this[string name] => GetByName(name);

    /// <summary>
    /// Gets a <see cref="Type"/> list for all the registered <see cref="IReferenceData"/> types.
    /// </summary>
    /// <returns>The <see cref="Type"/> list.</returns>
    public IEnumerable<Type> GetAllTypes() => _typeToProvider.Keys;

    /// <summary>
    /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> <see cref="Type"/>. 
    /// </summary>
    /// <typeparam name="TRef">The <see cref="IReferenceData"/> <see cref="Type"/>.</typeparam>
    /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, <see langword="null"/>.</returns>
    public IReferenceDataCollection? GetByType<TRef>() where TRef : IReferenceData => GetByType(typeof(TRef));

    /// <summary>
    /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> <see cref="Type"/>. 
    /// </summary>
    /// <param name="type">The <see cref="IReferenceData"/> <see cref="Type"/>.</param>
    /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, <see langword="null"/>.</returns>
    public IReferenceDataCollection? GetByType(Type type)
    {
        // To ensure greatest flexibility of the reference data capabilities, it may (and often from class properties by-design), be called in contexts where an async execution context is not available,
        // so we need to support a synchronous call pattern which will execute the underlying async code as it will need to get from cache or underlying repository. RunSync is also optimized to avoid
        // unnecessary context switches where the underlying code is already completed.
        return Invoker.RunSync(() => GetByTypeAsync(type));
    }

    /// <summary>
    /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> <see cref="Type"/> (will throw <see cref="InvalidOperationException"/> where not found).
    /// </summary>
    /// <typeparam name="TRef">The <see cref="IReferenceData"/> <see cref="Type"/>.</typeparam>
    /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, will throw an <see cref="InvalidOperationException"/>.</returns>
    public IReferenceDataCollection GetByTypeRequired<TRef>() where TRef : IReferenceData => GetByTypeRequired(typeof(TRef));

    /// <summary>
    /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> <see cref="Type"/> (will throw <see cref="InvalidOperationException"/> where not found).
    /// </summary>
    /// <param name="type">The <see cref="IReferenceData"/> <see cref="Type"/>.</param>
    /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, will throw an <see cref="InvalidOperationException"/>.</returns>
    public IReferenceDataCollection GetByTypeRequired(Type type) => Invoker.RunSync(() => GetByTypeRequiredAsync(type));

    /// <summary>
    /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> <see cref="Type"/>. 
    /// </summary>
    /// <typeparam name="TRef">The <see cref="IReferenceData"/> <see cref="Type"/>.</typeparam>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, <see langword="null"/>.</returns>
    public Task<IReferenceDataCollection?> GetByTypeAsync<TRef>(CancellationToken cancellationToken = default) where TRef : IReferenceData => GetByTypeAsync(typeof(TRef), cancellationToken);

    /// <summary>
    /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> <see cref="Type"/>. 
    /// </summary>
    /// <param name="type">The <see cref="IReferenceData"/> <see cref="Type"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, <see langword="null"/>.</returns>
    public async Task<IReferenceDataCollection?> GetByTypeAsync(Type type, CancellationToken cancellationToken = default)
    {
        if (!_typeToProvider.TryGetValue(type.ThrowIfNull(), out var providerType))
            return null;

        if (!ExecutionContext.HasCurrent)
            throw new InvalidOperationException($"The {nameof(ReferenceDataOrchestrator)} requires an active {nameof(ExecutionContext)} to support underlying scoped service resolution.");

        // Get the underlying scoped cache.
        var cache = ExecutionContext.GetRequiredService<IReferenceDataCache>();

        // Get the corresponding reference data collection type.
        var collType = _typeToCollType[type];

        // Get or create the reference data collection.
        var coll = await cache.GetOrCreateAsync(collType, (collType, cancellationToken) =>  
        {
            return _invoker.InvokeAsync(this, async (tracer, cancellationToken) =>
            {
                if (tracer.Activity is not null)
                {
                    tracer.Activity.AddTag(InvokerCacheType, type.ToString());
                    tracer.Activity.AddTag(InvokerCacheState, "Task.Run");
                }

                if (Logger?.IsEnabled(LogLevel.Debug) == true)
                    Logger.LogDebug("Reference data type {RefDataType} cache load start: ServiceProvider.CreateScope and Threading.ExecutionContext.SuppressFlow to support underlying cache data get.", type.FullName);

                /* 
                 * OK, a bit of complexity going on here. Why? As we do not know exactly when this method will be called, we need to:
                 * a) ensure that the underlying reference data provider is executed in a completely separate execution context to the caller to ensure that any scoped services used by the provider do not 
                 *    impact the caller's execution context and/or scoped services.
                 * b) when called, are we are retrieving data from a repository, and the caller in a transaction scope, we do not want to flow the transaction scope to the new thread as this may cause deadlocks;
                 *    achieved by using Threading.ExecutionContext.SuppressFlow.
                 * c) a Task.Run is used to ensure that the provider execution occurs on a different thread to the caller, again to ensure complete execution context separation. Note that the Task.Run is executed
                 *    within the context of the cache GetOrCreateAsync factory method, so will only be used when the cache item is not already populated.
                 */

                using var ec = ExecutionContext.Current.CreateCopy();
                var rdo = this;

                await using var scope = ServiceProvider.CreateAsyncScope();
                Task<IReferenceDataCollection> task;
                using (System.Threading.ExecutionContext.SuppressFlow())
                {
                    task = Task.Run(async () =>
                    {
                        try
                        {
                            return await GetByTypeInNewScopeAsync(rdo, ec, scope, type, providerType, cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception ex) when (Logger?.IsEnabled(LogLevel.Error) == true)
                        {
                            Logger.LogError(ex, "Reference data type {RefDataType} cache load failed in worker task: {ex.Message}", type.FullName, ex.Message);
                            throw; // Re-throw to propagate
                        }
                    });
                }

                var coll = await task.WaitAsync(cancellationToken).ConfigureAwait(false);

                if (Logger?.IsEnabled(LogLevel.Information) == true)
                    Logger.LogInformation("Reference data type {RefDataType} cache load finish: {ItemCount} items cached.", type.ToString(), coll.Count);

                tracer.Activity?.AddTag(InvokerCacheCount, coll.Count);

                return coll;
            }, cancellationToken, nameof(GetByTypeAsync));
        }, cancellationToken).ConfigureAwait(false);

        return coll ?? throw new InvalidOperationException($"The {nameof(IReferenceDataCollection)} returned for Type '{type.FullName}' from Provider '{providerType.FullName}' must not be null.");
    }

    /// <summary>
    /// Performs the actual reference data load in a new thread context / scope.
    /// </summary>
    private async Task<IReferenceDataCollection> GetByTypeInNewScopeAsync(ReferenceDataOrchestrator rdo, ExecutionContext executionContext, AsyncServiceScope scope, Type type, Type providerType, CancellationToken cancellationToken)
    {
        _asyncLocal.Value = rdo;

        executionContext.ServiceProvider = scope.ServiceProvider;
        ExecutionContext.SetCurrent(executionContext);

        // Start related activity as this "work" is occurring on an unrelated different thread (by design to ensure complete execution separation).
        return await _invoker.InvokeAsync(rdo, async (tracer, cancellationToken) =>
        {
            if (tracer.Activity is not null)
            {
                tracer.Activity.AddTag(InvokerCacheType, type.ToString());
                tracer.Activity.AddTag(InvokerCacheState, "Task.Worker");
            }

            var provider = (IReferenceDataProvider)scope.ServiceProvider.GetRequiredService(providerType);
            var coll = await provider.GetAsync(type, cancellationToken).ConfigureAwait(false);

            tracer.Activity?.AddTag(InvokerCacheCount, coll.Count);

            return coll;
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> <see cref="Type"/> (will throw <see cref="InvalidOperationException"/> where not found).
    /// </summary>
    /// <typeparam name="TRef">The <see cref="IReferenceData"/> <see cref="Type"/>.</typeparam>
    /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, will throw an <see cref="InvalidOperationException"/>.</returns>
    public Task<IReferenceDataCollection> GetByTypeRequiredAsync<TRef>(CancellationToken cancellationToken = default) where TRef : IReferenceData
        => GetByTypeRequiredAsync(typeof(TRef), cancellationToken);

    /// <summary>
    /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> <see cref="Type"/> (will throw <see cref="InvalidOperationException"/> where not found).
    /// </summary>
    /// <param name="type">The <see cref="IReferenceData"/> <see cref="Type"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, will throw an <see cref="InvalidOperationException"/>.</returns>
    public async Task<IReferenceDataCollection> GetByTypeRequiredAsync(Type type, CancellationToken cancellationToken = default)
        => (await GetByTypeAsync(type, cancellationToken).ConfigureAwait(false)) ?? throw new InvalidOperationException($"Reference data collection for type '{type.FullName}' does not exist.");

    /// <summary>
    /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> name (see <see cref="IReferenceData"/> <see cref="Type"/> <see cref="System.Reflection.MemberInfo.Name"/>). 
    /// </summary>
    /// <param name="name">The reference data name.</param>
    /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, <see langword="null"/>.</returns>
    public IReferenceDataCollection? GetByName(string name)
        => _nameToType.TryGetValue(name.ThrowIfNull(), out var type) ? GetByType(type) : null;

    /// <summary>
    /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> name (see <see cref="IReferenceData"/> <see cref="Type"/> <see cref="System.Reflection.MemberInfo.Name"/>). 
    /// </summary>
    /// <param name="name">The reference data name.</param>
    /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, <see langword="null"/>.</returns>
    public IReferenceDataCollection GetByNameRequired(string name)
         => _nameToType.TryGetValue(name.ThrowIfNull(), out var type) ? GetByTypeRequired(type) : throw new InvalidOperationException($"Reference data collection for name '{name}' does not exist.");

    /// <summary>
    /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> name (see <see cref="IReferenceData"/> <see cref="Type"/> <see cref="System.Reflection.MemberInfo.Name"/>). 
    /// </summary>
    /// <param name="name">The reference data name.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, <see langword="null"/>.</returns>
    public Task<IReferenceDataCollection?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        => _nameToType.TryGetValue(name.ThrowIfNull(), out var type) ? GetByTypeAsync(type, cancellationToken) : Task.FromResult<IReferenceDataCollection?>(null);

    /// <summary>
    /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> name (see <see cref="IReferenceData"/> <see cref="Type"/> <see cref="System.Reflection.MemberInfo.Name"/>). 
    /// </summary>
    /// <param name="name">The reference data name.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, <see langword="null"/>.</returns>
    public Task<IReferenceDataCollection> GetByNameRequiredAsync(string name, CancellationToken cancellationToken = default)
        => _nameToType.TryGetValue(name.ThrowIfNull(), out var type) ? GetByTypeRequiredAsync(type, cancellationToken) : throw new InvalidOperationException($"Reference data collection for name '{name}' does not exist.");

    /// <summary>
    /// Gets the <see cref="IReferenceData"/> list for the specified <see cref="IReferenceData"/> <see cref="Type"/> applying the <paramref name="codes"/> and <paramref name="textPattern"/> filter.
    /// </summary>
    /// <typeparam name="TRef">The <see cref="IReferenceData"/> <see cref="System.Type"/>.</typeparam>
    /// <param name="codes">The reference data code list.</param>
    /// <param name="textPattern">The reference data text (including wildcards).</param>
    /// <param name="includeInactive">Indicates whether to include inactive (<see cref="IReferenceData.IsInactive"/> equal <see langword="true"/>) entries.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The filtered collection.</returns>
    public async Task<IEnumerable<TRef>> GetWithFilterAsync<TRef>(IEnumerable<string>? codes = null, string? textPattern = null, bool includeInactive = false, CancellationToken cancellationToken = default) where TRef : IReferenceData
        => (await GetWithFilterAsync(typeof(TRef), codes, textPattern, includeInactive, cancellationToken).ConfigureAwait(false)).OfType<TRef>();

    /// <summary>
    /// Gets the <see cref="IReferenceData"/> list for the specified <see cref="IReferenceData"/> <see cref="Type"/> applying the <paramref name="codes"/> and <paramref name="textPattern"/> filter.
    /// </summary>
    /// <param name="type">The <see cref="IReferenceData"/> <see cref="Type"/>.</param>
    /// <param name="codes">The reference data code list.</param>
    /// <param name="textPattern">The reference data text (including wildcards).</param>
    /// <param name="includeInactive">Indicates whether to include inactive (<see cref="IReferenceData.IsInactive"/> equal <see langword="true"/>) entries.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The filtered collection.</returns>
    public async Task<IEnumerable<IReferenceData>> GetWithFilterAsync(Type type, IEnumerable<string>? codes = null, string? textPattern = null, bool includeInactive = false, CancellationToken cancellationToken = default)
        => GetWithFilterAsync(await GetByTypeAsync(type, cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException($"Reference data collection for type '{type.FullName}' does not exist."), codes, textPattern, includeInactive);

    /// <summary>
    /// Gets the <see cref="IReferenceData"/> list for the specified <see cref="IReferenceData"/> name (see <see cref="IReferenceData"/> <see cref="Type"/> <see cref="MemberInfo.Name"/>) applying the <paramref name="codes"/> and <paramref name="textPattern"/> filter.
    /// </summary>
    /// <param name="name">The reference data name.</param>
    /// <param name="codes">The reference data code list.</param>
    /// <param name="textPattern">The reference data text (including wildcards).</param>
    /// <param name="includeInactive">Indicates whether to include inactive (<see cref="IReferenceData.IsInactive"/> equal <see langword="true"/>) entries.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The filtered collection.</returns>
    public async Task<IEnumerable<IReferenceData>> GetWithFilterAsync(string name, IEnumerable<string>? codes = null, string? textPattern = null, bool includeInactive = false, CancellationToken cancellationToken = default)
        => GetWithFilterAsync(await GetByNameAsync(name, cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException($"Reference data collection for name '{name}' does not exist."), codes, textPattern, includeInactive);

    /// <summary>
    /// Apply the selected filter to the collection.
    /// </summary>
    private static IEnumerable<IReferenceData> GetWithFilterAsync(IReferenceDataCollection coll, IEnumerable<string>? codes = null, string? textPattern = null, bool includeInactive = false)
    {
        if ((codes is null || !codes.Any()) && string.IsNullOrEmpty(textPattern) && !includeInactive)
            return coll.ActiveItems;

        // Validate the arguments.
        if (textPattern is not null && Wildcard.Default.Parse(textPattern).HasError)
            throw new ValidationException(TextWildcardErrorMessage);

        // Apply the filter.
        var items = includeInactive ? coll.AllItems : coll.ActiveItems;
        var result = items
            .WhereWhen(codes is not null && codes.Any(), x => codes!.Contains(x.Code, StringComparer.OrdinalIgnoreCase))
            .WhereWildcard(x => x.Text, textPattern);

        return result;
    }

    /// <summary>
    /// Prefetches all of the named <see cref="IReferenceData"/> items. 
    /// </summary>
    /// <param name="names">The list of <see cref="IReferenceData"/> names.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The actual distinct list of names used.</returns>
    /// <remarks>As the reference data is a great candidate for caching this will force a prefetch of the cache items.</remarks>
    public async Task<IEnumerable<string>> PrefetchAsync(IEnumerable<string> names, CancellationToken cancellationToken = default)
    {
        // Get the distinct list of known names to prefetch.
        var list = names.Where(ContainsName).Distinct(StringComparer.OrdinalIgnoreCase);

        // Go get 'em all!
        await Parallel.ForEachAsync(list, new ParallelOptions { MaxDegreeOfParallelism = PrefetchMaxDegreeOfParallelism, CancellationToken = cancellationToken },
            async (name, ct) => await GetByNameAsync(name, ct).ConfigureAwait(false)).ConfigureAwait(false);

        return list;
    }

    /// <summary>
    /// Gets the reference data items for the specified <paramref name="names"/>.
    /// </summary>
    /// <param name="names">The reference data names.</param>
    /// <param name="includeInactive">Indicates whether to include inactive (<see cref="IReferenceData.IsInactive"/> equal <see langword="true"/>) entries.</param>
    /// <param name="mapper">The mapping of names to their replacement.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="ReferenceDataMultiDictionary"/>.</returns>
    /// <remarks>Will return an empty collection where no <paramref name="names"/> are specified.</remarks>
    public async Task<ReferenceDataMultiDictionary> GetNamedAsync(IEnumerable<string> names, bool includeInactive = false, IDictionary<string, string>? mapper = null, CancellationToken cancellationToken = default)
    {
        var mc = new ReferenceDataMultiDictionary();

        if (names is not null)
        {
            var list = await PrefetchAsync(ReplaceNames(names, mapper), cancellationToken).ConfigureAwait(false);

            foreach (var name in list)
            {
                mc.Add(mapper?.Where(x => x.Value == name).Select(x => x.Key).FirstOrDefault() ?? _nameToType[name].Name,
                    await GetWithFilterAsync(name, includeInactive: includeInactive, cancellationToken: cancellationToken).ConfigureAwait(false));
            }
        }

        return mc;
    }

    /// <summary>
    /// Replaces the specified <paramref name="names"/> based on the provided <paramref name="mapper"/>.
    /// </summary>
    private static IEnumerable<string> ReplaceNames(IEnumerable<string> names, IDictionary<string, string>? mapper)
    {
        foreach (var name in names)
        {
            if (mapper is not null && mapper.TryGetValue(name, out var mappedName))
                yield return mappedName;
            else
                yield return name;
        }
    }

    /// <summary>
    /// Tries to get the <typeparamref name="TRef"/> <see cref="IReferenceData"/> item for the specified <paramref name="code"/>.
    /// </summary>
    /// <typeparam name="TRef">The <see cref="IReferenceData"/> <see cref="Type"/>.</typeparam>
    /// <param name="code">The <see cref="IReferenceData.Code"/>.</param>
    /// <param name="item">The <typeparamref name="TRef"/> instance.</param>
    /// <returns><see langword="true"/> where found; otherwise, <see langword="false"/>.</returns>
    /// <remarks>Where the item (<see cref="IReferenceData"/>) is not found it will be created and <see cref="IReferenceData.SetInvalid"/> will be invoked; unless where <paramref name="code"/> is
    /// <see langword="null"/> in which case a <see langword="null"/> will also be returned.</remarks>
    public static bool TryGetByCode<TRef>(string? code, out TRef item) where TRef : IReferenceData, new()
    {
        if (code is not null && HasCurrent)
        {
            var rdc = Current.GetByType<TRef>();
            if (rdc is not null && rdc.TryGetByCode(code, out var rd))
            {
                item = (TRef)rd!;
                return true;
            }
        }

        item = new TRef { Code = code };
        ((IReferenceData)item).SetInvalid();
        return false;
    }

    /// <summary>
    /// Tries to get the <typeparamref name="TRef"/> <see cref="IReferenceData"/> item for the specified <paramref name="id"/>.
    /// </summary>
    /// <typeparam name="TRef">The <see cref="IReferenceData"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="TId">The <see cref="IReferenceData{TId}"/> <see cref="IReferenceData.Id"/> <see cref="Type"/>.</typeparam>
    /// <param name="id">The <see cref="IReferenceData"/> <see cref="IReferenceData.Id"/>.</param>
    /// <param name="item">The <typeparamref name="TRef"/> instance.</param>
    /// <returns><see langword="true"/> where found; otherwise, <see langword="false"/>.</returns>
    /// <remarks>Where the item (<see cref="IReferenceData"/>) is not found it will be created and <see cref="IReferenceData.SetInvalid"/> will be invoked.</remarks>
    public static bool TryGetById<TRef, TId>(TId id, out TRef item) where TRef : IReferenceData<TId>, new()
    {
        if (id is not null && HasCurrent)
        {
            var rdc = Current.GetByType<TRef>();
            if (rdc is not null && rdc.TryGetById(id, out var rd))
            {
                item = (TRef)rd!;
                return true;
            }
        }

        item = new TRef { Id = id };
        ((IReferenceData)item).SetInvalid();
        return false;
    }

    /// <summary>
    /// Tries to get the <typeparamref name="TRef"/> <see cref="IReferenceData"/> item for the specified <paramref name="id"/>.
    /// </summary>
    /// <typeparam name="TRef">The <see cref="IReferenceData"/> <see cref="Type"/>.</typeparam>
    /// <param name="id">The <see cref="IReferenceData"/> <see cref="IReferenceData.Id"/>.</param>
    /// <param name="item">The <typeparamref name="TRef"/> instance.</param>
    /// <returns><see langword="true"/> where found; otherwise, <see langword="false"/>.</returns>
    /// <remarks>Where the item (<see cref="IReferenceData"/>) is not found it will be created and <see cref="IReferenceData.SetInvalid"/> will be invoked.</remarks>
    public static bool TryGetById<TRef>(object id, out TRef item) where TRef : IReferenceData, new()
    {
        if (id is not null && HasCurrent)
        {
            var rdc = Current.GetByType<TRef>();
            if (rdc is not null && rdc.TryGetById(id, out var rd))
            {
                item = (TRef)rd!;
                return true;
            }
        }

        item = new TRef { Id = id };
        ((IReferenceData)item).SetInvalid();
        return false;
    }


    /// <summary>
    /// Performs a conversion from a mapping value to an instance of <typeparamref name="TRef"/>.
    /// </summary>
    /// <typeparam name="TRef">The <see cref="IReferenceData"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="T">The mapping value <see cref="Type"/>.</typeparam>
    /// <param name="name">The mapping name.</param>
    /// <param name="value">The mapping value.</param>
    /// <remarks>Where the item (<see cref="IReferenceData"/>) is not found it will be created and <see cref="IReferenceData.SetInvalid"/> will be invoked.</remarks>
    public static TRef ConvertFromMapping<TRef, T>(string name, T? value) where TRef : IReferenceData, new() where T : IComparable<T?>, IEquatable<T?>
    {
        if (value is not null && HasCurrent)
        {
            var rdc = Current.GetByType<TRef>();
            if (rdc is not null && rdc.TryGetByMapping(name, value, out var rd))
                return (TRef)rd!;
        }

        var rdx = new TRef();
        ((IReferenceData)rdx).SetInvalid();
        return rdx;
    }

    /// <summary>
    /// Gets all the <see cref="IReferenceData"/> types in the same namespace as <typeparamref name="TNamespace"/>.
    /// </summary>
    /// <typeparam name="TNamespace">The <see cref="Type"/> to infer the namespace.</typeparam>
    /// <returns>The <see cref="Type"/> list.</returns>
    public static IEnumerable<Type> GetAllTypesInNamespace<TNamespace>()
        => typeof(TNamespace).Assembly.GetTypes().Where(t => t.Namespace == typeof(TNamespace).Namespace && t.IsClass && !t.IsAbstract && typeof(IReferenceData).IsAssignableFrom(t));
}