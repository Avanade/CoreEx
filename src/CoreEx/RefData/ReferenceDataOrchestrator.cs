// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using CoreEx.Entities;
using CoreEx.Invokers;
using CoreEx.RefData.Caching;
using CoreEx.Wildcards;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.RefData
{
    /// <summary>
    /// Provides the centralized reference data orchestration. Primarily responsible for the management of one or more <see cref="IReferenceDataProvider"/> instances.  
    /// </summary>
    /// <remarks>Provides <i>cached</i> access to the underlying reference data collections via the likes of <see cref="GetByTypeAsync{TRef}"/>, <see cref="GetByTypeAsync(Type, CancellationToken)"/> or <see cref="GetByNameAsync(string, CancellationToken)"/>.
    /// <para>To improve performance the reference data is cached. The <see cref="ReferenceDataOrchestrator"/> enables this via an <see cref="IMemoryCache"/> implementation; default is <see cref="MemoryCache"/> where not explicitly specified.
    /// The underlying reference data loading is executed in the context of a <see cref="ServiceProviderServiceExtensions.CreateScope(IServiceProvider)"/> to limit/minimize any impact on the processing of the current request by isolating all scoped services.</para></remarks>
    public class ReferenceDataOrchestrator
    {
        /// <summary>
        /// Gets the error message where the <see cref="IReferenceData.Text"/> <see cref="Wildcard"/> value is invalid.
        /// </summary>
        public const string TextWildcardErrorMessage = "Text contains invalid or unsupported wildcard selection.";

        private const string InvokerCacheType = "refdata.cachetype";
        private const string InvokerCacheState = "refdata.cachestate";
        private const string InvokerCacheCount = "refdata.cachecount";

        private static readonly AsyncLocal<ReferenceDataOrchestrator?> _asyncLocal = new();

        private readonly object _lock = new();
        private readonly ConcurrentDictionary<Type, Type> _typeToProvider = new();
        private readonly ConcurrentDictionary<string, Type> _nameToType = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<object, SemaphoreSlim> _semaphores = new();
        private readonly Lazy<ILogger> _logger;
        private readonly Lazy<SettingsBase?> _settings;

        /// <summary>
        /// Gets or sets the current <see cref="ReferenceDataOrchestrator"/> for the executing thread graph (see <see cref="AsyncLocal{T}"/>)
        /// </summary>
        public static ReferenceDataOrchestrator Current
        {
            get
            {
                if (_asyncLocal.Value is not null)
                    return _asyncLocal.Value;

                if (ExecutionContext.HasCurrent)
                {
                    try
                    {
                        var rdo = ExecutionContext.GetService<ReferenceDataOrchestrator>();
                        if (rdo is not null)
                            return rdo;
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Unable to get an instance of the {nameof(ReferenceDataOrchestrator)} from the {nameof(ExecutionContext)}. It is recommended that the {nameof(ReferenceDataOrchestrator)}.{nameof(SetCurrent)} is used to set globally; this can be performed using the IApplicationBuilder.UseReferenceDataOrchestrator method during start-up where applicable.", ex);
                    }
                }

                throw new InvalidOperationException($"Unable to get an instance of the {nameof(ReferenceDataOrchestrator)} from the {nameof(ExecutionContext)}. It is recommended that the {nameof(ReferenceDataOrchestrator)}.{nameof(SetCurrent)} is used to set globally; this can be performed using the IApplicationBuilder.UseReferenceDataOrchestrator method during start-up where applicable.");
            }
        }

        /// <summary>
        /// Indicates whether the <see cref="ReferenceDataOrchestrator"/> <see cref="Current"/> has a value.
        /// </summary>
        public static bool HasCurrent => _asyncLocal != null;

        /// <summary>
        /// Sets (or overriddes) the <see cref="Current"/> instance.
        /// </summary>
        /// <param name="orchestrator">The <see cref="ReferenceDataOrchestrator"/>.</param>
        public static void SetCurrent(ReferenceDataOrchestrator? orchestrator = null) => _asyncLocal.Value = orchestrator;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceDataOrchestrator"/> class.
        /// </summary>
        /// <param name="serivceProvider">The <see cref="IServiceProvider"/> needed to instantiated the registered providers, etc.</param>
        /// <param name="cache">The <see cref="IMemoryCache"/>. Defaults to new <see cref="MemoryCache"/> instance.</param>
        /// <param name="cacheEntryConfig">The <see cref="ICacheEntryConfig"/>. Defaults to new <see cref="SettingsBasedCacheEntry"/> instance.</param>
        public ReferenceDataOrchestrator(IServiceProvider serivceProvider, IMemoryCache? cache = null, ICacheEntryConfig? cacheEntryConfig = null)
        {
            ServiceProvider = serivceProvider.ThrowIfNull(nameof(serivceProvider));
#if NET7_0_OR_GREATER
            Cache = cache ?? new MemoryCache(new MemoryCacheOptions { TrackStatistics = true });
#else
            Cache = cache ?? new MemoryCache(new MemoryCacheOptions());
#endif
            CacheEntryConfig = cacheEntryConfig ?? new SettingsBasedCacheEntry(ServiceProvider.GetService<SettingsBase>());
            _logger = new Lazy<ILogger>(ServiceProvider.GetRequiredService<ILogger<ReferenceDataOrchestrator>>);
            _settings = new Lazy<SettingsBase?>(ServiceProvider.GetService<SettingsBase>);
        }

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/>.
        /// </summary>
        public IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Gets the underlying <see cref="IMemoryCache"/>.
        /// </summary>
        public IMemoryCache Cache { get; }

        /// <summary>
        /// Gets the underlying <see cref="ICacheEntryConfig"/>.
        /// </summary>
        public ICacheEntryConfig CacheEntryConfig { get; }

        /// <summary>
        /// Gets the <see cref="ILogger"/>.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public ILogger Logger => _logger.Value;

        /// <summary>
        /// Gets the <see cref="SettingsBase"/>.
        /// </summary>
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public SettingsBase Settings => _settings.Value!;

#if NET6_0_OR_GREATER
        /// <summary>
        /// Gets or sets the <see cref="ParallelOptions.MaxDegreeOfParallelism"/> to use when performing a <see cref="PrefetchAsync(IEnumerable{string}, CancellationToken)"/>.
        /// </summary>
        /// <remarks>Defaults to <c>-1</c>. The <see cref="PrefetchAsync"/> uses <see cref="Parallel.ForEachAsync{TSource}(IAsyncEnumerable{TSource}, ParallelOptions, Func{TSource, CancellationToken, ValueTask})"/> and as such 
        /// these setting will equal the equivalent of the <see cref="Environment.ProcessorCount"/>; see this <see href="https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.paralleloptions.maxdegreeofparallelism#remarks">article</see>.</remarks>
        public int PrefetchMaxDegreeOfParallelism { get; set; } = -1;
#endif

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

            foreach (var type in provider.Types.Where(x => x != null).Distinct())
            {
                lock (_lock)
                {
                    if (_nameToType.ContainsKey(type.Name))
                        throw new InvalidOperationException($"Type '{type.FullName}' cannot be added as name '{type.Name}' already associated with previously added Type '{_nameToType.GetValueOrDefault(type.Name)?.FullName}'.");

                    if (!_typeToProvider.TryAdd(type, typeof(TProvider)))
                        throw new InvalidOperationException($"Type '{type.FullName}' cannot be added as already associated with previously added Provider '{_typeToProvider.GetValueOrDefault(type)?.GetType().FullName}'.");

                    _nameToType.TryAdd(type.Name, type);
                }
            }

            return this;
        }

        /// <summary>
        /// Determines whether the <see cref="ReferenceDataOrchestrator"/> contains the specified <see cref="IReferenceData"/> <see cref="Type"/>.
        /// </summary>
        /// <typeparam name="TRef">The <see cref="IReferenceData"/> <see cref="Type"/>.</typeparam>
        /// <returns><c>true</c> indicates that it exists; otherwise, <c>false</c>.</returns>
        public bool ContainsType<TRef>() => ContainsType(typeof(TRef));

        /// <summary>
        /// Determines whether the <see cref="ReferenceDataOrchestrator"/> contains the specified <see cref="IReferenceData"/> <see cref="Type"/>.
        /// </summary>
        /// <param name="type">The <see cref="IReferenceData"/> <see cref="Type"/>.</param>
        /// <returns><c>true</c> indicates that it exists; otherwise, <c>false</c>.</returns>
        public bool ContainsType(Type type) => _typeToProvider.ContainsKey(type);

        /// <summary>
        /// Determines whether the <see cref="ReferenceDataOrchestrator"/> contains the specified <see cref="IReferenceData"/> name (see <see cref="IReferenceData"/> <see cref="Type"/> <see cref="System.Reflection.MemberInfo.Name"/>). 
        /// </summary>
        /// <param name="name">The reference data name.</param>
        /// <returns><c>true</c> indicates that it exists; otherwise, <c>false</c>.</returns>
        public bool ContainsName(string name) => _nameToType.ContainsKey(name);

        /// <summary>
        /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> <see cref="Type"/> synchronously. 
        /// </summary>
        /// <param name="type">The <see cref="IReferenceData"/> <see cref="Type"/>.</param>
        /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, <c>null</c>.</returns>
        public IReferenceDataCollection? this[Type type] => GetByType(type);

        /// <summary>
        /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> name (see <see cref="IReferenceData"/> <see cref="Type"/> <see cref="System.Reflection.MemberInfo.Name"/>) synchronously. 
        /// </summary>
        /// <param name="name">The reference data name.</param>
        /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, <c>null</c>.</returns>
        public IReferenceDataCollection? this[string name] => GetByName(name);

        /// <summary>
        /// Gets a <see cref="Type"/> list for the registered <see cref="IReferenceData"/> types.
        /// </summary>
        /// <returns>The <see cref="Type"/> list.</returns>
        public IEnumerable<Type> GetAllTypes() => _typeToProvider.Keys;

        /// <summary>
        /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> <see cref="Type"/>. 
        /// </summary>
        /// <typeparam name="TRef">The <see cref="IReferenceData"/> <see cref="Type"/>.</typeparam>
        /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, <c>null</c>.</returns>
        public IReferenceDataCollection? GetByType<TRef>() => GetByType(typeof(TRef));

        /// <summary>
        /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> <see cref="Type"/>. 
        /// </summary>
        /// <param name="type">The <see cref="IReferenceData"/> <see cref="Type"/>.</param>
        /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, <c>null</c>.</returns>
        public IReferenceDataCollection? GetByType(Type type) => Cache.TryGetValue(OnGetCacheKey(type), out IReferenceDataCollection? coll) ? coll! : Invoker.RunSync(() => GetByTypeAsync(type));

        /// <summary>
        /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> <see cref="Type"/> (will throw <see cref="InvalidOperationException"/> where not found).
        /// </summary>
        /// <typeparam name="TRef">The <see cref="IReferenceData"/> <see cref="Type"/>.</typeparam>
        /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, will throw an <see cref="InvalidOperationException"/>.</returns>
        public IReferenceDataCollection GetByTypeRequired<TRef>() => GetByTypeRequired(typeof(TRef));

        /// <summary>
        /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> <see cref="Type"/> (will throw <see cref="InvalidOperationException"/> where not found).
        /// </summary>
        /// <param name="type">The <see cref="IReferenceData"/> <see cref="Type"/>.</param>
        /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, will throw an <see cref="InvalidOperationException"/>.</returns>
        public IReferenceDataCollection GetByTypeRequired(Type type) => Cache.TryGetValue(OnGetCacheKey(type), out IReferenceDataCollection? coll) ? coll! : Invoker.RunSync(() => GetByTypeRequiredAsync(type));

        /// <summary>
        /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> <see cref="Type"/>. 
        /// </summary>
        /// <typeparam name="TRef">The <see cref="IReferenceData"/> <see cref="Type"/>.</typeparam>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, <c>null</c>.</returns>
        public Task<IReferenceDataCollection?> GetByTypeAsync<TRef>(CancellationToken cancellationToken = default) => GetByTypeAsync(typeof(TRef), cancellationToken);

        /// <summary>
        /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> <see cref="Type"/>. 
        /// </summary>
        /// <param name="type">The <see cref="IReferenceData"/> <see cref="Type"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, <c>null</c>.</returns>
        public async Task<IReferenceDataCollection?> GetByTypeAsync(Type type, CancellationToken cancellationToken = default)
        {
            if (!_typeToProvider.TryGetValue(type.ThrowIfNull(nameof(type)), out var providerType))
                return null;

            var coll = await OnGetOrCreateAsync(type, (t, ct) =>
            {
                return ReferenceDataOrchestratorInvoker.Current.InvokeAsync(this, async (ia, ct) =>
                {
                    if (ia.Activity is not null)
                    {
                        ia.Activity.AddTag(InvokerCacheType, type.ToString());
                        ia.Activity.AddTag(InvokerCacheState, "TaskRun");
                    }

                    Logger.LogDebug("Reference data type {RefDataType} cache load start: ServiceProvider.CreateScope and Threading.ExecutionContext.SuppressFlow to support underlying cache data get.", type.FullName);
                    using var ec = ExecutionContext.Current.CreateCopy();
                    var rdo = this;

                    using var scope = ServiceProvider.CreateScope();
                    Task<IReferenceDataCollection> task;
                    using (System.Threading.ExecutionContext.SuppressFlow())
                    {
                        task = Task.Run(async () => await GetByTypeInNewScopeAsync(rdo, ec, scope, t, providerType, ia, ct).ConfigureAwait(false));
                    }

#if NET6_0_OR_GREATER
                    return await task.WaitAsync(ct).ConfigureAwait(false);
#else
                    task.Wait(ct);
                    await Task.CompletedTask.ConfigureAwait(false);
                    return task.Result;
#endif
                }, cancellationToken, nameof(GetByTypeAsync));
            }, cancellationToken).ConfigureAwait(false);

            return coll ?? throw new InvalidOperationException($"The {nameof(IReferenceDataCollection)} returned for Type '{type.FullName}' from Provider '{providerType.FullName}' must not be null.");
        }

        /// <summary>
        /// Performs the actual reference data load in a new thread context / scope.
        /// </summary>
        private async Task<IReferenceDataCollection> GetByTypeInNewScopeAsync(ReferenceDataOrchestrator rdo, ExecutionContext executionContext, IServiceScope scope, Type type, Type providerType, InvokeArgs invokeArgs, CancellationToken cancellationToken)
        {
            _asyncLocal.Value = rdo;

            executionContext.ServiceProvider = scope.ServiceProvider;
            ExecutionContext.SetCurrent(executionContext);

            // Start related activity as this "work" is occuring on an unrelated different thread (by design to ensure complete separation).
            var ria = invokeArgs.StartNewRelated(invokeArgs.Invoker, rdo, nameof(GetByTypeInNewScopeAsync));
            try
            {
                if (ria.Activity is not null)
                {
                    ria.Activity.AddTag(InvokerCacheType, type.ToString());
                    ria.Activity.AddTag(InvokerCacheState, "TaskWorker");
                }

                var sw = Stopwatch.StartNew();
                var provider = (IReferenceDataProvider)scope.ServiceProvider.GetRequiredService(providerType);
                var coll = (await provider.GetAsync(type, cancellationToken).ConfigureAwait(false)).Value!;
                sw.Stop();

                Logger.LogInformation("Reference data type {RefDataType} cache load finish: {ItemCount} items cached [{Elapsed}ms]", type.ToString(), coll.Count, sw.Elapsed.TotalMilliseconds);
                ria.Activity?.AddTag(InvokerCacheCount, coll.Count);

                return ria.TraceResult(coll);
            }
            catch (Exception ex)
            {
                ria.TraceException(ex);
                throw;
            }
            finally
            {
                ria.TraceComplete();
            }
        }

        /// <summary>
        /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> <see cref="Type"/> (will throw <see cref="InvalidOperationException"/> where not found).
        /// </summary>
        /// <typeparam name="TRef">The <see cref="IReferenceData"/> <see cref="Type"/>.</typeparam>
        /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, will throw an <see cref="InvalidOperationException"/>.</returns>
        public Task<IReferenceDataCollection> GetByTypeRequiredAsync<TRef>(CancellationToken cancellationToken = default) => GetByTypeRequiredAsync(typeof(TRef), cancellationToken);

        /// <summary>
        /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> <see cref="Type"/> (will throw <see cref="InvalidOperationException"/> where not found).
        /// </summary>
        /// <param name="type">The <see cref="IReferenceData"/> <see cref="Type"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, will throw an <see cref="InvalidOperationException"/>.</returns>
        public async Task<IReferenceDataCollection> GetByTypeRequiredAsync(Type type, CancellationToken cancellationToken = default)
            => (await GetByTypeAsync(type, cancellationToken).ConfigureAwait(false)) ?? throw new InvalidOperationException($"Reference data collection for type '{type.FullName}' does not exist.");

        /// <summary>
        /// Gets where pre-existing and not expired, or (re-)creates, the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> <see cref="Type"/>. 
        /// </summary>
        /// <param name="type">The <see cref="IReferenceData"/> <see cref="Type"/>.</param>
        /// <param name="getCollAsync">The underlying function to invoke to get the <see cref="IReferenceDataCollection"/> when (re-)creating cache collection.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="IReferenceDataCollection"/>.</returns>
        /// <remarks>Invokes the <see cref="OnCreateCacheEntry(Type, ICacheEntry)"/> prior to invoking <paramref name="getCollAsync"/> on <i>create</i>. This should be overridden where the default <see cref="IMemoryCache"/> capabilities are not
        /// sufficient. The <paramref name="getCollAsync"/> contains the logic to invoke the underlying <see cref="IReferenceDataProvider.GetAsync(Type, System.Threading.CancellationToken)"/>; this is executed in the context of a 
        /// <see cref="ServiceProviderServiceExtensions.CreateScope(IServiceProvider)"/> to limit/minimize any impact on the processing of the current request by isolating all scoped services. Additionally, semaphore locks are used to
        /// manage concurrency to ensure cache loading is thread-safe.</remarks>
        private async Task<IReferenceDataCollection> OnGetOrCreateAsync(Type type, Func<Type, CancellationToken, Task<IReferenceDataCollection>> getCollAsync, CancellationToken cancellationToken = default)
        {
            // Try and get as most likely already in the cache; where exists then exit fast.
            var key = OnGetCacheKey(type);
            if (Cache.TryGetValue(key, out IReferenceDataCollection? coll))
                return coll!;

            // Get or add a new semaphore for the cache key so we can manage single concurrency for that key only.
            SemaphoreSlim semaphore = _semaphores.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

            // Use the semaphore to manage a single thread to perform the "expensive" get operation.
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Does a get or create as it may have been added as we went to lock.
                return (await Cache.GetOrCreateAsync(key, async entry =>
                {
                    OnCreateCacheEntry(type, entry);
                    return await getCollAsync(type, cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException("The returned collection must not be null.");
                }).ConfigureAwait(false))!;
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// Gets the cache key to be used (defaults to <paramref name="type"/>).
        /// </summary>
        /// <param name="type">The <see cref="IReferenceData"/> <see cref="Type"/>.</param>
        /// <returns>The cache key.</returns>
        /// <remarks>Leverages the <see cref="ICacheEntryConfig.GetCacheKey(Type)"/>.
        /// <para>To support the likes of multi-tenancy caching then the resulting cache key should be overridden to include the both the <see cref="ExecutionContext.TenantId"/> and <paramref name="type"/>.</para></remarks>
        protected virtual object OnGetCacheKey(Type type) => CacheEntryConfig.GetCacheKey(type);

        /// <summary>
        /// Provides an opportunity to the maintain the <see cref="ICacheEntry"/> data prior to the cache <i>create</i> function being invoked (as a result of <see cref="OnGetOrCreateAsync(Type, Func{Type, CancellationToken, Task{IReferenceDataCollection}}, CancellationToken)"/>).
        /// </summary>
        /// <param name="type">The <see cref="IReferenceData"/> <see cref="Type"/>.</param>
        /// <param name="entry">The <see cref="ICacheEntry"/>.</param>
        /// <remarks>Leverages the <see cref="ICacheEntryConfig.CreateCacheEntry(Type, ICacheEntry)"/>.</remarks>
        protected virtual void OnCreateCacheEntry(Type type, ICacheEntry entry) => CacheEntryConfig.CreateCacheEntry(type, entry);

        /// <summary>
        /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> name (see <see cref="IReferenceData"/> <see cref="Type"/> <see cref="System.Reflection.MemberInfo.Name"/>). 
        /// </summary>
        /// <param name="name">The reference data name.</param>
        /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, <c>null</c>.</returns>
        public IReferenceDataCollection? GetByName(string name)
            => _nameToType.TryGetValue(name.ThrowIfNull(nameof(name)), out var type) ? GetByType(type) : null;

        /// <summary>
        /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> name (see <see cref="IReferenceData"/> <see cref="Type"/> <see cref="System.Reflection.MemberInfo.Name"/>). 
        /// </summary>
        /// <param name="name">The reference data name.</param>
        /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, <c>null</c>.</returns>
        public IReferenceDataCollection GetByNameRequired(string name)
             => _nameToType.TryGetValue(name.ThrowIfNull(nameof(name)), out var type) ? GetByTypeRequired(type) : throw new InvalidOperationException($"Reference data collection for name '{name}' does not exist.");

        /// <summary>
        /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> name (see <see cref="IReferenceData"/> <see cref="Type"/> <see cref="System.Reflection.MemberInfo.Name"/>). 
        /// </summary>
        /// <param name="name">The reference data name.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, <c>null</c>.</returns>
        public Task<IReferenceDataCollection?> GetByNameAsync(string name, CancellationToken cancellationToken = default) 
            => _nameToType.TryGetValue(name.ThrowIfNull(nameof(name)), out var type) ? GetByTypeAsync(type, cancellationToken) : Task.FromResult<IReferenceDataCollection?>(null);

        /// <summary>
        /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> name (see <see cref="IReferenceData"/> <see cref="Type"/> <see cref="System.Reflection.MemberInfo.Name"/>). 
        /// </summary>
        /// <param name="name">The reference data name.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, <c>null</c>.</returns>
        public Task<IReferenceDataCollection> GetByNameRequiredAsync(string name, CancellationToken cancellationToken = default) 
            => _nameToType.TryGetValue(name.ThrowIfNull(nameof(name)), out var type) ? GetByTypeRequiredAsync(type, cancellationToken) : throw new InvalidOperationException($"Reference data collection for name '{name}' does not exist.");

        /// <summary>
        /// Gets the <see cref="IReferenceData"/> list for the specified <see cref="IReferenceData"/> <see cref="Type"/> applying the <paramref name="codes"/> and <paramref name="text"/> filter.
        /// </summary>
        /// <typeparam name="TRef">The <see cref="IReferenceData"/> <see cref="System.Type"/>.</typeparam>
        /// <param name="codes">The reference data code list.</param>
        /// <param name="text">The reference data text (including wildcards).</param>
        /// <param name="includeInactive">Indicates whether to include inactive (<see cref="IReferenceData.IsActive"/> equal <c>false</c>) entries.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The filtered collection.</returns>
        public async Task<IEnumerable<TRef>> GetWithFilterAsync<TRef>(IEnumerable<string>? codes = null, string? text = null, bool includeInactive = false, CancellationToken cancellationToken = default) where TRef : IReferenceData
            => (await GetWithFilterAsync(typeof(TRef), codes, text, includeInactive, cancellationToken).ConfigureAwait(false)).OfType<TRef>();

        /// <summary>
        /// Gets the <see cref="IReferenceData"/> list for the specified <see cref="IReferenceData"/> <see cref="Type"/> applying the <paramref name="codes"/> and <paramref name="text"/> filter.
        /// </summary>
        /// <param name="type">The <see cref="IReferenceData"/> <see cref="Type"/>.</param>
        /// <param name="codes">The reference data code list.</param>
        /// <param name="text">The reference data text (including wildcards).</param>
        /// <param name="includeInactive">Indicates whether to include inactive (<see cref="IReferenceData.IsActive"/> equal <c>false</c>) entries.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The filtered collection.</returns>
        public async Task<IEnumerable<IReferenceData>> GetWithFilterAsync(Type type, IEnumerable<string>? codes = null, string? text = null, bool includeInactive = false, CancellationToken cancellationToken = default)
            => await GetWithFilterAsync(await GetByTypeAsync(type, cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException($"Reference data collection for type '{type.FullName}' does not exist."), codes, text, includeInactive, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Gets the <see cref="IReferenceData"/> list for the specified <see cref="IReferenceData"/> name (see <see cref="IReferenceData"/> <see cref="Type"/> <see cref="System.Reflection.MemberInfo.Name"/>) applying the <paramref name="codes"/> and <paramref name="text"/> filter.
        /// </summary>
        /// <param name="name">The reference data name.</param>
        /// <param name="codes">The reference data code list.</param>
        /// <param name="text">The reference data text (including wildcards).</param>
        /// <param name="includeInactive">Indicates whether to include inactive (<see cref="IReferenceData.IsActive"/> equal <c>false</c>) entries.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The filtered collection.</returns>
        public async Task<IEnumerable<IReferenceData>> GetWithFilterAsync(string name, IEnumerable<string>? codes = null, string? text = null, bool includeInactive = false, CancellationToken cancellationToken = default)
            => await GetWithFilterAsync(await GetByNameAsync(name, cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException($"Reference data collection for name '{name}' does not exist."), codes, text, includeInactive, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Applys the selected filter to the collection.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Future proofing.")]
        private static Task<IEnumerable<IReferenceData>> GetWithFilterAsync(IReferenceDataCollection coll, IEnumerable<string>? codes = null, string? text = null, bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            if ((codes == null || !codes.Any()) && string.IsNullOrEmpty(text) && !includeInactive)
                return Task.FromResult(coll.ActiveItems);

            // Validate the arguments.
            if (text != null && !Wildcard.Default.Validate(text))
                throw new ValidationException(TextWildcardErrorMessage);

            // Apply the filter.
            var items = includeInactive ? coll.AllItems : coll.ActiveItems;
            var result = items
                .WhereWhen(x => codes!.Contains(x.Code, StringComparer.OrdinalIgnoreCase), codes != null && codes.Distinct().FirstOrDefault() != null)
                .WhereWildcard(x => x.Text, text);

            return Task.FromResult(result);
        }

        /// <summary>
        /// Prefetches all of the named <see cref="IReferenceData"/> items. 
        /// </summary>
        /// <param name="names">The list of <see cref="IReferenceData"/> names.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>As the reference data is a great candidate for caching this will force a prefetch of the cache items.</remarks>
        public Task PrefetchAsync(IEnumerable<string> names, CancellationToken cancellationToken = default)
        {
#if NET6_0_OR_GREATER
            return Parallel.ForEachAsync(names, new ParallelOptions { MaxDegreeOfParallelism = PrefetchMaxDegreeOfParallelism, CancellationToken = cancellationToken },
                async (name, ct) => await GetByNameAsync(name, ct).ConfigureAwait(false));
#else

            var tasks = new List<Task>();
            if (names != null)
            {
                foreach (var name in names.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    tasks.Add(GetByNameAsync(name, cancellationToken));
                }
            }

            return Task.WhenAll(tasks);
#endif
        }

        /// <summary>
        /// Gets the reference data items for the specified <paramref name="names"/>.
        /// </summary>
        /// <param name="names">The reference data names.</param>
        /// <param name="includeInactive">Indicates whether to include inactive (<see cref="IReferenceData.IsActive"/> equal <c>false</c>) entries.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="ReferenceDataMultiDictionary"/>.</returns>
        /// <remarks>Will return an empty collection where no <paramref name="names"/> are specified.</remarks>
        public async Task<ReferenceDataMultiDictionary> GetNamedAsync(IEnumerable<string> names, bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            var mc = new ReferenceDataMultiDictionary();

            if (names != null)
            {
                await PrefetchAsync(names, cancellationToken).ConfigureAwait(false);

                foreach (var name in names.Where(ContainsName).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    mc.Add(_nameToType[name].Name, await GetWithFilterAsync(name, includeInactive: includeInactive, cancellationToken: cancellationToken).ConfigureAwait(false));
                }
            }

            return mc;
        }

        /// <summary>
        /// Gets the reference data items for the specified names and related codes (see <see cref="IReferenceData.Code"/>).
        /// </summary>
        /// <param name="namesAndCodes">The reference data names and related codes.</param>
        /// <param name="includeInactive">Indicates whether to include inactive (<see cref="IReferenceData.IsActive"/> equal <c>false</c>) entries.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="ReferenceDataMultiDictionary"/>.</returns>
        /// <remarks>Will return an empty collection where no <paramref name="namesAndCodes"/> are specified.</remarks>
        public async Task<ReferenceDataMultiDictionary> GetNamedAsync(IEnumerable<KeyValuePair<string, List<string>>> namesAndCodes, bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            var mc = new ReferenceDataMultiDictionary();

            if (namesAndCodes != null)
            {
                await PrefetchAsync(namesAndCodes.Select(x => x.Key).AsEnumerable(), cancellationToken).ConfigureAwait(false);

                foreach (var kvp in namesAndCodes.Where(x => ContainsName(x.Key)))
                {
                    mc.Add(_nameToType[kvp.Key].Name, await GetWithFilterAsync(kvp.Key, codes: kvp.Value, includeInactive: includeInactive, cancellationToken: cancellationToken).ConfigureAwait(false));
                }
            }

            return mc;
        }

        /// <summary>
        /// Performs a conversion from a <see cref="IReferenceData.Code"/> to an instance of <typeparamref name="TRef"/>.
        /// </summary>
        /// <typeparam name="TRef">The <see cref="IReferenceData"/> <see cref="Type"/>.</typeparam>
        /// <param name="code">The <see cref="IReferenceData.Code"/>.</param>
        /// <returns>The <typeparamref name="TRef"/> instance.</returns>
        /// <remarks>Where the item (<see cref="IReferenceData"/>) is not found it will be created and <see cref="IReferenceData.SetInvalid"/> will be invoked.</remarks>
        [return: NotNullIfNotNull(nameof(code))]
        public static TRef? ConvertFromCode<TRef>(string? code) where TRef : IReferenceData, new()
        {
            if (code == null)
                return default;

            if (ExecutionContext.HasCurrent)
            {
                var rdc = Current.GetByType<TRef>();
                if (rdc != null && rdc.TryGetByCode(code, out var rd))
                    return (TRef)rd!;
            }

            var rdx = new TRef { Code = code };
            ((IReferenceData)rdx).SetInvalid();
            return rdx;
        }

        /// <summary>
        /// Performs a conversion from an <see cref="IReferenceData"/> <see cref="IIdentifier.Id"/> to an instance of <typeparamref name="TRef"/>.
        /// </summary>
        /// <typeparam name="TRef">The <see cref="IReferenceData"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="TId">The <see cref="IReferenceData"/> <see cref="IIdentifier.Id"/> <see cref="Type"/>.</typeparam>
        /// <param name="id">The <see cref="IReferenceData"/> <see cref="IIdentifier.Id"/>.</param>
        /// <returns>The <typeparamref name="TRef"/> instance.</returns>
        /// <remarks>Where the item (<see cref="IReferenceData"/>) is not found it will be created and <see cref="IReferenceData.SetInvalid"/> will be invoked.</remarks>
        [return: NotNullIfNotNull(nameof(id))]
        public static TRef? ConvertFromId<TRef, TId>(TId? id) where TRef : IReferenceData<TId>, new() where TId : IComparable<TId>, IEquatable<TId>
        {
            if (id == null)
                return default;

            if (ExecutionContext.HasCurrent)
            {
                var rdc = Current.GetByType<TRef>();
                if (rdc != null && rdc.TryGetById(id, out var rd))
                    return (TRef)rd!;
            }

            var rdx = new TRef { Id = id };
            ((IReferenceData)rdx).SetInvalid();
            return rdx;
        }

        /// <summary>
        /// Performs a conversion from an <see cref="IReferenceData"/> <see cref="IIdentifier.Id"/> to an instance of <typeparamref name="TRef"/>.
        /// </summary>
        /// <typeparam name="TRef">The <see cref="IReferenceData"/> <see cref="Type"/>.</typeparam>
        /// <param name="id">The <see cref="IReferenceData"/> <see cref="IIdentifier.Id"/>.</param>
        /// <returns>The <typeparamref name="TRef"/> instance.</returns>
        /// <remarks>Where the item (<see cref="IReferenceData"/>) is not found it will be created and <see cref="IReferenceData.SetInvalid"/> will be invoked.</remarks>
        [return: NotNullIfNotNull(nameof(id))]
        public static TRef? ConvertFromId<TRef>(object? id) where TRef : IReferenceData, new()
        {
            if (id == null)
                return default;

            if (ExecutionContext.HasCurrent)
            {
                var rdc = Current.GetByType<TRef>();
                if (rdc != null && rdc.TryGetById(id, out var rd))
                    return (TRef)rd!;
            }

            var rdx = new TRef { Id = id };
            ((IReferenceData)rdx).SetInvalid();
            return rdx;
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
            if (value != null && ExecutionContext.HasCurrent)
            {
                var rdc = Current.GetByType<TRef>();
                if (rdc != null && rdc.TryGetByMapping(name, value, out var rd))
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
        public static IEnumerable<Type> GetAllTypesInNamespace<TNamespace>() => typeof(TNamespace).Assembly.GetTypes().Where(t => t.Namespace == typeof(TNamespace).Namespace && t.IsClass && !t.IsAbstract && typeof(IReferenceData).IsAssignableFrom(t));

    }
}