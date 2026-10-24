namespace CoreEx.Metadata;

public static partial class RuntimeMetadata
{
    private static readonly ConcurrentDictionary<Type, Func<object, CleanArgs, object>> _wrappedValueCleaners = new();
    private static readonly ConcurrentDictionary<Type, bool> _wrappedValueCleanerTypes = new();

    /// <summary>
    /// Indicates whether a type implements its self-typed wrapper cleaning contract.
    /// </summary>
    internal static bool IsWrappedValueCleaner(Type type) => _wrappedValueCleanerTypes.GetOrAdd(type,
        static t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IWrappedValueCleaner<>) && i.GenericTypeArguments[0] == t));

    /// <summary>
    /// Cleans a wrapper through a constrained call without boxing its receiver or result.
    /// </summary>
    [return: NotNullIfNotNull(nameof(value))]
    private static TWrapper? CleanWrapper<TWrapper>(TWrapper? value, CleanArgs args) where TWrapper : IWrappedValueCleaner<TWrapper>
    {
        if (!typeof(TWrapper).IsValueType && value is null)
            return value;

        // Null references returned above; the remaining value-type path cannot be null.
        var wrapper = value!;
        var cleaned = wrapper.Clean(args);
        if (!typeof(TWrapper).IsValueType && (cleaned is null || cleaned.GetType() != wrapper.GetType()))
            throw new InvalidOperationException($"The wrapped-value cleaner for '{wrapper.GetType()}' must return a non-null value of the same runtime wrapper type.");

        return cleaned;
    }

    /// <summary>
    /// Adapts a wrapper whose type was erased by an object or interface boundary.
    /// </summary>
    private static object CleanErasedWrapper<TWrapper>(object value, CleanArgs args) where TWrapper : IWrappedValueCleaner<TWrapper> => CleanWrapper((TWrapper)value, args);

    /// <summary>
    /// Caches typed wrapper dispatch once per declared value type.
    /// </summary>
    private static class WrappedValueCleaner<T>
    {
        /// <summary>
        /// Gets the constrained cleaning delegate when the type supports wrapper cleaning.
        /// </summary>
        internal static readonly Func<T, CleanArgs, T>? Clean = IsWrappedValueCleaner(typeof(T))
            ? typeof(RuntimeMetadata).GetMethod(nameof(CleanWrapper), BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(typeof(T)).CreateDelegate<Func<T, CleanArgs, T>>()
            : null;
    }
}
