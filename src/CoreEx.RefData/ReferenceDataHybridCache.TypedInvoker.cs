using System.Linq.Expressions;
using System.Reflection;

namespace CoreEx.RefData;

public partial class ReferenceDataHybridCache
{
    /*
     * This functionality is required as the underlying cache *may* leverage serialization, and as such, we have to get it in a typed manner as IReferenceDataCollection (interface) is not valid.
     */

    private static readonly MethodInfo TryGetByKeyAsync_OpenGeneric = typeof(IHybridCache).GetMethod(nameof(IHybridCache.TryGetByKeyAsync)) ?? throw new InvalidOperationException($"{nameof(IHybridCache)}.{nameof(IHybridCache.TryGetByKeyAsync)} public instance method not found.");
    private static readonly ConcurrentDictionary<Type, TryGetByKeyInvoker> _invokers = new();

    private delegate Task<(bool Exists, object? Value)> TryGetByKeyInvoker(IHybridCache cache, string key, HybridCacheEntryOptions options, CancellationToken cancellationToken);

    /// <summary>
    /// Gets (or adds) the <see cref="TryGetByKeyInvoker"/> for the specified type.
    /// </summary>
    private static TryGetByKeyInvoker GetInvokerForType(Type type) => _invokers.GetOrAdd(type, type =>
    {
        // Close the generic: TryGetByKeyAsync<T>
        var closed = TryGetByKeyAsync_OpenGeneric.MakeGenericMethod(type);

        // Parameters: (cache, key, options, cancellationToken) =>
        var cacheParam = Expression.Parameter(typeof(IHybridCache), "cache");
        var keyParam = Expression.Parameter(typeof(string), "key");
        var optParam = Expression.Parameter(typeof(HybridCacheEntryOptions), "options");
        var ctParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

        // Expression: cache.TryGetByKeyAsync<TVal>(key, options, ct)
        var call = Expression.Call(cacheParam, closed, keyParam, optParam, ctParam);

        // Build method body: ToTupleTask<T>(call).
        var method = typeof(ReferenceDataHybridCache).GetMethod(nameof(ToTupleTask), BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(type);
        var body = Expression.Call(method, call);
        var lambda = Expression.Lambda<TryGetByKeyInvoker>(body, cacheParam, keyParam, optParam, ctParam);
        return lambda.Compile();
    });

    /// <summary>
    /// Underlying method to invoke the typed <see cref="IHybridCache.TryGetByKeyAsync{T}"/>.
    /// </summary>
    private static async Task<(bool Exists, object? Value)> ToTupleTask<T>(Task<(bool Exists, T? Value)> task) => await task.ConfigureAwait(false);
}