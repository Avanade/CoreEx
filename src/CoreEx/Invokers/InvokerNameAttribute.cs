namespace CoreEx.Invokers;

/// <summary>
/// Provides an attribute to override the <see cref="IInvoker.Name"/> name.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class InvokerNameAttribute(string name) : Attribute
{
    private static readonly ConcurrentDictionary<Type, InvokerNameAttribute> _cache = [];

    /// <summary>
    /// Gets the name.
    /// </summary>
    public string Name { get; } = name.ThrowIfNullOrEmpty();

    /// <summary>
    /// Gets the name for the specified <typeparamref name="T"/> using the <see cref="InvokerNameAttribute"/> where defined; otherwise, uses the formatted name from the <see cref="Type"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Type"/>.</typeparam>
    /// <returns>The name.</returns>
    public static string GetName<T>() => GetName(typeof(T));

    /// <summary>
    /// Gets the name for the specified <paramref name="type"/> using the <see cref="InvokerNameAttribute"/> where defined; otherwise, uses the formatted name from the <see cref="Type"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/>.</param>
    /// <returns>The name.</returns>
    public static string GetName(Type type) => _cache.GetOrAdd(type.ThrowIfNull(), t => t.GetCustomAttribute<InvokerNameAttribute>(true) ?? new InvokerNameAttribute(Internal.GetNamespaceFormattedName(t))).Name;
}