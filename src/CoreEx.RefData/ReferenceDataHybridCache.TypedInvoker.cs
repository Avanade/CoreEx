using System.Linq.Expressions;
using System.Reflection;

namespace CoreEx.RefData;

public partial class ReferenceDataHybridCache
{
    /*
     * This functionality is required as the underlying cache *may* leverage serialization, and as such, we have to get it in a typed manner as IReferenceDataCollection (interface) is not valid.
     * This applies equally to both reading (TryGetByKeyAsync) and getting-or-creating (GetOrCreateByKeyAsync) - relying on C# generic type inference for the latter would infer the factory
     * delegate's declared IReferenceDataCollection return type rather than the concrete collection type, which a serializing (e.g. distributed) cache cannot deserialize back.
     */

    private static readonly MethodInfo _tryGetByKeyAsync_OpenGeneric = typeof(IHybridCache).GetMethod(nameof(IHybridCache.TryGetByKeyAsync)) ?? throw new InvalidOperationException($"{nameof(IHybridCache)}.{nameof(IHybridCache.TryGetByKeyAsync)} public instance method not found.");
    private static readonly MethodInfo _getOrCreateTypedAsync_OpenGeneric = typeof(ReferenceDataHybridCache).GetMethod(nameof(GetOrCreateTypedAsync), BindingFlags.NonPublic | BindingFlags.Static) ?? throw new InvalidOperationException($"{nameof(ReferenceDataHybridCache)}.{nameof(GetOrCreateTypedAsync)} private static method not found.");
    private static readonly ConcurrentDictionary<Type, TryGetByKeyInvoker> _invokers = new();
    private static readonly ConcurrentDictionary<Type, GetOrCreateByKeyInvoker> _getOrCreateInvokers = new();

    private delegate Task<(bool Exists, object? Value)> TryGetByKeyInvoker(IHybridCache cache, string key, HybridCacheEntryOptions options, CancellationToken cancellationToken);

    private delegate Task<IReferenceDataCollection> GetOrCreateByKeyInvoker(IHybridCache cache, string key, Func<Type, CancellationToken, Task<IReferenceDataCollection>> factory, Type type, HybridCacheEntryOptions options, CancellationToken cancellationToken);

    /// <summary>
    /// Gets (or adds) the <see cref="TryGetByKeyInvoker"/> for the specified type.
    /// </summary>
    private static TryGetByKeyInvoker GetInvokerForType(Type type) => _invokers.GetOrAdd(type, type =>
    {
        // Close the generic: TryGetByKeyAsync<T>
        var closed = _tryGetByKeyAsync_OpenGeneric.MakeGenericMethod(type);

        // Parameters: (cache, key, options, cancellationToken) =>
        var cacheParam = Expression.Parameter(typeof(IHybridCache), "cache");
        var keyParam = Expression.Parameter(typeof(string), "key");
        var optParam = Expression.Parameter(typeof(HybridCacheEntryOptions), "options");
        var ctParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

        // Expression: cache.TryGetByKeyAsync<TVal>(key, options, ct)
        var call = Expression.Call(cacheParam, closed, keyParam, optParam, ctParam);

        // Build method body: ToTupleAsync<T>(call).
        var method = typeof(ReferenceDataHybridCache).GetMethod(nameof(ToTupleAsync), BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(type);
        var body = Expression.Call(method, call);
        var lambda = Expression.Lambda<TryGetByKeyInvoker>(body, cacheParam, keyParam, optParam, ctParam);
        return lambda.Compile();
    });

    /// <summary>
    /// Underlying method to invoke the typed <see cref="IHybridCache.TryGetByKeyAsync{T}"/>.
    /// </summary>
    private static async Task<(bool Exists, object? Value)> ToTupleAsync<T>(Task<(bool Exists, T? Value)> task) => await task.ConfigureAwait(false);

    /// <summary>
    /// Gets (or adds) the <see cref="GetOrCreateByKeyInvoker"/> for the specified type.
    /// </summary>
    private static GetOrCreateByKeyInvoker GetOrCreateInvokerForType(Type type) => _getOrCreateInvokers.GetOrAdd(type, type =>
    {
        // Close the generic: GetOrCreateTypedAsync<T>.
        var closed = _getOrCreateTypedAsync_OpenGeneric.MakeGenericMethod(type);
        return (GetOrCreateByKeyInvoker)Delegate.CreateDelegate(typeof(GetOrCreateByKeyInvoker), closed);
    });

    /// <summary>
    /// Underlying method to invoke the typed <see cref="IHybridCache.GetOrCreateByKeyAsync{T}"/> ensuring the concrete <typeparamref name="T"/> (as opposed to the <see cref="IReferenceDataCollection"/>
    /// interface) is used as the generic type argument; this is essential where the underlying cache leverages serialization (e.g. a distributed cache), as an interface/abstract collection type cannot
    /// be deserialized.
    /// </summary>
    private static async Task<IReferenceDataCollection> GetOrCreateTypedAsync<T>(IHybridCache cache, string key, Func<Type, CancellationToken, Task<IReferenceDataCollection>> factory, Type type, HybridCacheEntryOptions options, CancellationToken cancellationToken) where T : IReferenceDataCollection
        => await cache.GetOrCreateByKeyAsync<T>(key, async ct =>
        {
            var value = await factory(type, ct).ConfigureAwait(false) ?? throw new InvalidOperationException($"The '{type.Name}' (reference data) collection returned from the factory must not be null.");
            return (T)value;
        }, options, cancellationToken).ConfigureAwait(false);
}
