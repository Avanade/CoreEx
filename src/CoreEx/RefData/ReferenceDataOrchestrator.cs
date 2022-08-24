// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Entities;
using CoreEx.Json;
using CoreEx.WebApis;
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
    /// <para>To improve performance the reference data should be cached where possible. The <see cref="ReferenceDataOrchestrator"/> supports a default <see cref="IMemoryCache"/> implementation; however, this can be overridden and/or extended by
    /// overriding the <see cref="OnGetOrCreateAsync(Type, Func{Type, CancellationToken, Task{IReferenceDataCollection}}, CancellationToken)"/> method. The <i>create</i> is executed in the context of a <see cref="ServiceProviderServiceExtensions.CreateScope(IServiceProvider)"/>
    /// to limit/minimize any impact on the processing of the current request by isolating all scoped services.</para></remarks>
    public class ReferenceDataOrchestrator
    {
        /// <summary>
        /// Gets the error message where the <see cref="IReferenceData.Text"/> <see cref="Wildcard"/> value is invalid.
        /// </summary>
        public const string TextWildcardErrorMessage = "Text contains invalid or unsupported wildcard selection.";

        private readonly object _lock = new();
        private readonly ConcurrentDictionary<Type, Type> _typeToProvider = new();
        private readonly ConcurrentDictionary<string, Type> _nameToType = new(StringComparer.OrdinalIgnoreCase);
        private readonly IMemoryCache? _cache;
        private readonly Lazy<ILogger> _logger;

        /// <summary>
        /// Gets the current <see cref="ReferenceDataOrchestrator"/> from the <see cref="IServiceProvider"/> within the <see cref="ExecutionContext.Current"/> <see cref="ExecutionContext"/> scope (see <see cref="ExecutionContext.GetService(Type)"/>) and will throw an <see cref="InvalidOperationException"/> where not found.
        /// </summary>
        public static ReferenceDataOrchestrator Current => 
            ExecutionContext.GetService<ReferenceDataOrchestrator>() ?? throw new InvalidOperationException($"To access {nameof(ReferenceDataOrchestrator)}.{nameof(Current)} it must be added as a Dependency Injection service ({nameof(IServiceProvider)}) and the request must be mande within an {nameof(ExecutionContext)} scope.");

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceDataOrchestrator"/> class.
        /// </summary>
        /// <param name="serivceProvider">The <see cref="IServiceProvider"/> needed to instantiated the registered providers.</param>
        /// <param name="cache">The optional <see cref="IMemoryCache"/> to be used where not specifically overridden by <see cref="OnGetOrCreateAsync(Type, Func{Type, CancellationToken, Task{IReferenceDataCollection}}, CancellationToken)"/>.</param>
        public ReferenceDataOrchestrator(IServiceProvider serivceProvider, IMemoryCache? cache)
        {
            ServiceProvider = serivceProvider ?? throw new ArgumentNullException(nameof(serivceProvider));
            _cache = cache;
            _logger = new Lazy<ILogger>(ServiceProvider.GetRequiredService<ILogger<ReferenceDataOrchestrator>>);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceDataOrchestrator"/> class.
        /// </summary>
        /// <param name="serivceProvider">The <see cref="IServiceProvider"/> needed to instantiated the registered providers.</param>
        /// <param name="useDefaultMemoryCache">Indicates whether to use a default <see cref="MemoryCache"/>; otherwise, no <see cref="IMemoryCache"/> at all.</param>
        public ReferenceDataOrchestrator(IServiceProvider serivceProvider, bool useDefaultMemoryCache = true) : this(serivceProvider, useDefaultMemoryCache ? new MemoryCache(new MemoryCacheOptions()) : null) { }

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/>.
        /// </summary>
        public IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Gets the <see cref="ILogger"/>.
        /// </summary>
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public ILogger Logger => _logger.Value;

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
        /// <param name="name"></param>
        /// <returns><c>true</c> indicates that it exists; otherwise, <c>false</c>.</returns>
        public bool ContainsName(string name) => _nameToType.ContainsKey(name);

        /// <summary>
        /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> <see cref="Type"/> synchronously. 
        /// </summary>
        /// <param name="type">The <see cref="IReferenceData"/> <see cref="Type"/>.</param>
        /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, <c>null</c>.</returns>
        public IReferenceDataCollection? this[Type type] => GetByTypeAsync(type).GetAwaiter().GetResult();

        /// <summary>
        /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> name (see <see cref="IReferenceData"/> <see cref="Type"/> <see cref="System.Reflection.MemberInfo.Name"/>) synchronously. 
        /// </summary>
        /// <param name="name">The reference data name.</param>
        /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, <c>null</c>.</returns>
        public IReferenceDataCollection? this[string name] => GetByNameAsync(name).GetAwaiter().GetResult();

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
        public IReferenceDataCollection? GetByType(Type type) => GetByTypeAsync(type).GetAwaiter().GetResult();

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
        public IReferenceDataCollection GetByTypeRequired(Type type) => GetByTypeRequiredAsync(type).GetAwaiter().GetResult();

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
            if (!_typeToProvider.TryGetValue(type ?? throw new ArgumentNullException(nameof(type)), out var providerType))
                return null;

            var coll = await OnGetOrCreateAsync(type, async (t, ct) =>
            {
                Logger.LogDebug("Reference data type {RefDataType} cache load start: Creating new ServiceProvider Scope to support underlying cache data get.", type.FullName);
                var sw = Stopwatch.StartNew();
                using var scope = ServiceProvider.CreateScope();
                var provider = (IReferenceDataProvider)scope.ServiceProvider.GetRequiredService(providerType);
                var coll = await provider.GetAsync(t, ct).ConfigureAwait(false);
                coll.ETag = ETagGenerator.Generate(ServiceProvider.GetRequiredService<IJsonSerializer>(), coll)!;
                sw.Stop();
                Logger.LogInformation("Reference data type {RefDataType} cache load finish: {ItemCount} items cached [{Elapsed}ms]", type.FullName, coll.Count, sw.Elapsed.TotalMilliseconds);
                return coll;
            }, cancellationToken).ConfigureAwait(false);

            return coll ?? throw new InvalidOperationException($"The {nameof(IReferenceDataCollection)} returned for Type '{type.FullName}' from Provider '{providerType.FullName}' must not be null.");
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
        /// <remarks>Invokes the <see cref="OnCreateCacheEntry(ICacheEntry)"/> prior to invoking <paramref name="getCollAsync"/> on <i>create</i>. This should be overridden where the default <see cref="IMemoryCache"/> capabilities are not
        /// sufficient. The <paramref name="getCollAsync"/> contains the logic to invoke the underlying <see cref="IReferenceDataProvider.GetAsync(Type, System.Threading.CancellationToken)"/>; this is executed in the context of a 
        /// <see cref="ServiceProviderServiceExtensions.CreateScope(IServiceProvider)"/> to limit/minimize any impact on the processing of the current request by isolating all scoped services.</remarks>
        protected async virtual Task<IReferenceDataCollection> OnGetOrCreateAsync(Type type, Func<Type, CancellationToken, Task<IReferenceDataCollection>> getCollAsync, CancellationToken cancellationToken = default)
        {
            if (_cache == null)
                return await getCollAsync(type, cancellationToken).ConfigureAwait(false);
            else
                return await _cache.GetOrCreateAsync(type, async entry => 
                { 
                    OnCreateCacheEntry(entry); 
                    return await getCollAsync(type, cancellationToken).ConfigureAwait(false); 
                }).ConfigureAwait(false);
        }

        /// <summary>
        /// Provides an opportunity to the maintain the <see cref="ICacheEntry"/> data prior to the cache <i>create</i> function being invoked (as a result of <see cref="OnGetOrCreateAsync(Type, Func{Type, CancellationToken, Task{IReferenceDataCollection}}, CancellationToken)"/>).
        /// </summary>
        /// <param name="entry">The <see cref="ICacheEntry"/>.</param>
        /// <remarks>Note: the <see cref="ICacheEntry.Key"/> is the <see cref="IReferenceData"/> <see cref="Type"/>.
        /// <para>The default behaviour sets the following: <see cref="ICacheEntry.AbsoluteExpirationRelativeToNow"/> = 2 hours, and <see cref="ICacheEntry.SlidingExpiration"/> = 30 minutes. This should be overridden where this default
        /// behaviour needs to change.</para></remarks>
        protected virtual void OnCreateCacheEntry(ICacheEntry entry)
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2);
            entry.SlidingExpiration = TimeSpan.FromMinutes(30);
        }

        /// <summary>
        /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> name (see <see cref="IReferenceData"/> <see cref="Type"/> <see cref="System.Reflection.MemberInfo.Name"/>). 
        /// </summary>
        /// <param name="name">The reference data name.</param>
        /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, <c>null</c>.</returns>
        public IReferenceDataCollection? GetByName(string name) => GetByNameAsync(name).GetAwaiter().GetResult();

        /// <summary>
        /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> name (see <see cref="IReferenceData"/> <see cref="Type"/> <see cref="System.Reflection.MemberInfo.Name"/>). 
        /// </summary>
        /// <param name="name">The reference data name.</param>
        /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, <c>null</c>.</returns>
        public IReferenceDataCollection GetByNameRequired(string name) => GetByNameRequiredAsync(name).GetAwaiter().GetResult();

        /// <summary>
        /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> name (see <see cref="IReferenceData"/> <see cref="Type"/> <see cref="System.Reflection.MemberInfo.Name"/>). 
        /// </summary>
        /// <param name="name">The reference data name.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, <c>null</c>.</returns>
        public Task<IReferenceDataCollection?> GetByNameAsync(string name, CancellationToken cancellationToken = default) 
            => _nameToType.TryGetValue(name ?? throw new ArgumentNullException(nameof(name)), out var type) ? GetByTypeAsync(type, cancellationToken) : Task.FromResult<IReferenceDataCollection?>(null);

        /// <summary>
        /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> name (see <see cref="IReferenceData"/> <see cref="Type"/> <see cref="System.Reflection.MemberInfo.Name"/>). 
        /// </summary>
        /// <param name="name">The reference data name.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, <c>null</c>.</returns>
        public Task<IReferenceDataCollection> GetByNameRequiredAsync(string name, CancellationToken cancellationToken = default) 
            => _nameToType.TryGetValue(name ?? throw new ArgumentNullException(nameof(name)), out var type) ? GetByTypeRequiredAsync(type, cancellationToken) : throw new InvalidOperationException($"Reference data collection for name '{name}' does not exist.");

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
                return Task.FromResult(coll.ActiveList);

            // Validate the arguments.
            if (text != null && !Wildcard.Default.Validate(text))
                throw new ValidationException(TextWildcardErrorMessage);

            // Apply the filter.
            var items = includeInactive ? coll.AllList : coll.ActiveList;
            var result = items
                .WhereWhen(x => codes.Contains(x.Code, StringComparer.OrdinalIgnoreCase), codes != null && codes.Distinct().FirstOrDefault() != null)
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
            var tasks = new List<Task>();
            if (names != null)
            {
                foreach (var name in names.Distinct())
                {
                    tasks.Add(GetByNameAsync(name, cancellationToken));
                }
            }

            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Gets the reference data items for the specified <paramref name="names"/>.
        /// </summary>
        /// <param name="names">The reference data names.</param>
        /// <param name="includeInactive">Indicates whether to include inactive (<see cref="IReferenceData.IsActive"/> equal <c>false</c>) entries.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="ReferenceDataMultiCollection"/>.</returns>
        /// <remarks>Will return an empty collection where no <paramref name="names"/> are specified.</remarks>
        public async Task<ReferenceDataMultiCollection> GetNamedAsync(IEnumerable<string> names, bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            var mc = new ReferenceDataMultiCollection();

            if (names != null)
            {
                await PrefetchAsync(names, cancellationToken).ConfigureAwait(false);

                foreach (var name in names.Where(x => ContainsName(x)).Distinct())
                {
                    mc.Add(new ReferenceDataMultiItem(_nameToType[name].Name, await GetWithFilterAsync(name, includeInactive: includeInactive, cancellationToken: cancellationToken).ConfigureAwait(false)));
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
        /// <returns>The <see cref="ReferenceDataMultiCollection"/>.</returns>
        /// <remarks>Will return an empty collection where no <paramref name="namesAndCodes"/> are specified.</remarks>
        public async Task<ReferenceDataMultiCollection> GetNamedAsync(IEnumerable<KeyValuePair<string, List<string>>> namesAndCodes, bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            var mc = new ReferenceDataMultiCollection();

            if (namesAndCodes != null)
            {
                await PrefetchAsync(namesAndCodes.Select(x => x.Key).AsEnumerable(), cancellationToken).ConfigureAwait(false);

                foreach (var kvp in namesAndCodes.Where(x => ContainsName(x.Key)))
                {
                    mc.Add(new ReferenceDataMultiItem(_nameToType[kvp.Key].Name, await GetWithFilterAsync(kvp.Key, codes: kvp.Value, includeInactive: includeInactive, cancellationToken: cancellationToken).ConfigureAwait(false)));
                }
            }

            return mc;
        }

        /// <summary>
        /// Gets the reference data items for the specified names and related codes (see <see cref="IReferenceData.Code"/>) from the <paramref name="requestOptions"/>.
        /// </summary>
        /// <param name="requestOptions">The <see cref="WebApiRequestOptions"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="ReferenceDataMultiCollection"/>.</returns>
        /// <remarks>The reference data names and codes are specified as part of the query string. Either '<c>?names=RefA,RefB,RefX</c>' or <c>?RefA,RefB=CodeA,CodeB,RefX=CodeX</c> or any combination thereof.</remarks>
        public Task<ReferenceDataMultiCollection> GetNamedAsync(WebApiRequestOptions requestOptions, CancellationToken cancellationToken = default)
        {
            var dict = new Dictionary<string, List<string>>();

            foreach (var q in requestOptions.Request.Query.Where(x => !string.IsNullOrEmpty(x.Key)))
            {
                if (string.Compare(q.Key, "names", StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    foreach (var v in SplitStringValues(q.Value.Where(x => !string.IsNullOrEmpty(x)).Distinct()))
                    {
                        dict.TryAdd(v, new List<string>());
                    }
                }
                else
                {
                    if (dict.TryGetValue(q.Key, out var codes))
                    {
                        foreach (var code in SplitStringValues(q.Value.Distinct()))
                        {
                            if (!codes.Contains(code))
                                codes.Add(code);
                        }
                    }
                    else
                        dict.Add(q.Key, new List<string>(SplitStringValues(q.Value.Distinct())));
                }
            }

            return GetNamedAsync(dict.ToList(), requestOptions.IncludeInactive, cancellationToken);
        }

        /// <summary>
        /// Perform a further split of the string values.
        /// </summary>
        private static IEnumerable<string> SplitStringValues(IEnumerable<string> values)
        {
            var list = new List<string>();
            foreach (var value in values)
            {
                list.AddRange(value.Split(',', StringSplitOptions.RemoveEmptyEntries));
            }

            return list;
        }

        /// <summary>
        /// Performs a conversion from a <see cref="IReferenceData.Code"/> to an instance of <typeparamref name="TRef"/>.
        /// </summary>
        /// <typeparam name="TRef">The <see cref="IReferenceData"/> <see cref="Type"/>.</typeparam>
        /// <param name="code">The <see cref="IReferenceData.Code"/>.</param>
        /// <returns>The <typeparamref name="TRef"/> instance.</returns>
        /// <remarks>Where the item (<see cref="IReferenceData"/>) is not found it will be created and <see cref="IReferenceData.SetInvalid"/> will be invoked.</remarks>
        [return: NotNullIfNotNull("code")]
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
        [return: NotNullIfNotNull("id")]
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
    }
}