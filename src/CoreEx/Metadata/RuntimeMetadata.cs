namespace CoreEx.Metadata;

/// <summary>
/// Provides the underlying <see cref="IPropertyRuntimeMetadata"/> capabilities.
/// </summary>
/// <remarks>
/// The capabilities within are provided to primarily target common <i>contract</i> patterns in an <i>CoreEx</i>-opinionated manner. As such, there are limitations to the functionality included.
/// <para>This provides both compile-based (<see cref="IContract{T}"/>) and reflection-based implementations enabling a mix of usage enabling greater flexibility and consistency.</para></remarks>
public static partial class RuntimeMetadata
{
    /// <summary>
    /// Gets the underlying caching sliding expiration <see cref="TimeSpan"/>.
    /// </summary>
    /// <remarks>Defaults to <c>30</c> minutes.</remarks>
    public static TimeSpan SlidingExpirationTimespan => Internal.GetConfigurationValue("CoreEx:Runtime:Metadata:SlidingExpirationTimespan", TimeSpan.FromMinutes(30));

    /// <summary>
    /// Gets the <see cref="IPropertyRuntimeMetadata"/> for the specified <paramref name="propertyExpression"/>, preferring static metadata with a cached reflection fallback.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="propertyExpression">The property expression.</param>
    /// <returns>The corresponding <see cref="IPropertyRuntimeMetadata"/>.</returns>
    public static IPropertyRuntimeMetadata GetForExpression<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression) where TEntity : IContract<TEntity>
    {
        if (propertyExpression.ThrowIfNull().Body.NodeType != ExpressionType.MemberAccess)
            throw new ArgumentException("Only member access expressions are supported.", nameof(propertyExpression));

        var me = (MemberExpression)propertyExpression.Body;
        if (me.Member is PropertyInfo property)
            return GetForProperty(typeof(TEntity), property);

        throw new InvalidOperationException("The underlying property metadata cannot be retrieved for the property expression.");
    }

    /// <summary>
    /// Gets the <see cref="IPropertyRuntimeMetadata"/> for the specified <paramref name="propertyExpression"/>, preferring static metadata with a cached reflection fallback.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="propertyExpression">The property expression.</param>
    /// <param name="unused">An <i>unused</i> parameter needed for the compiler to differentiate same named method.</param>
    /// <returns>The corresponding <see cref="IPropertyRuntimeMetadata"/>.</returns>
#pragma warning disable IDE0060 // Remove unused parameter; unused and needed to differentiate same named methods.
    public static IPropertyRuntimeMetadata GetForExpression<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression, object? unused = null) where TEntity : class
#pragma warning restore IDE0060
    {
        if (propertyExpression.ThrowIfNull().Body.NodeType != ExpressionType.MemberAccess)
            throw new ArgumentException("Only member access expressions are supported.", nameof(propertyExpression));

        var me = (MemberExpression)propertyExpression.Body;
        if (me.Member is PropertyInfo property)
            return GetForProperty(typeof(TEntity), property);

        throw new InvalidOperationException("The underlying property metadata cannot be retrieved for the property expression.");
    }

    /// <summary>
    /// Resolves explicitly requested properties omitted by static metadata without changing the contract's property collection.
    /// </summary>
    private static IPropertyRuntimeMetadata GetForProperty(Type type, PropertyInfo property)
    {
        if (GetCachedProperties(type).TryGetValue(property.Name, out var metadata))
            return metadata;

        if (property.GetMethod is not { IsPublic: true, IsStatic: false } || property.GetIndexParameters().Length != 0)
            throw new InvalidOperationException("The underlying property metadata cannot be retrieved for the property expression.");

        return Internal.MemoryCache.GetOrCreate(("RuntimeMetadata_Property", type, property), entry =>
        {
            entry.SlidingExpiration = SlidingExpirationTimespan;
            return CreateReflectedPropertyMetadata(type, property);
        })!;
    }

    /// <summary>
    /// Gets the cached <see cref="IPropertyRuntimeMetadata"/> dictionary for the entity <typeparamref name="T"/> <see cref="Type"/> using <see cref="IRuntimeMetadata.GetStaticPropertyRuntimeMetadata"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="IContract{T}"/> <see cref="Type"/>.</typeparam>
    /// <returns>The cached <see cref="IPropertyRuntimeMetadata"/> dictionary.</returns>
    public static IDictionary<string, IPropertyRuntimeMetadata> GetCachedProperties<T>() where T : IContract<T> => GetCachedProperties(typeof(T));

    /// <summary>
    /// Gets the cached property metadata, preferring static <see cref="IRuntimeMetadata"/> metadata with reflection as the fallback.
    /// </summary>
    /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
    /// <param name="ignoreProperties">The list of properties to ignore.</param>
    /// <remarks>Metadata source selection and creation occur only on a cache miss. No entity instance is constructed.</remarks>
    public static IDictionary<string, IPropertyRuntimeMetadata> GetCachedProperties<T>(params IEnumerable<string> ignoreProperties) => GetCachedProperties(typeof(T), ignoreProperties);

    /// <summary>
    /// Gets the cached property metadata, preferring static <see cref="IRuntimeMetadata"/> metadata with reflection as the fallback.
    /// </summary>
    /// <param name="type">The <see cref="Type"/>.</param>
    /// <param name="ignoreProperties">The list of properties to ignore.</param>
    /// <remarks>Metadata source selection and creation occur only on a cache miss. No entity instance is constructed.
    /// Static metadata errors propagate; reflection is used only when the type does not implement <see cref="IRuntimeMetadata"/>.</remarks>
    public static IDictionary<string, IPropertyRuntimeMetadata> GetCachedProperties(Type type, params IEnumerable<string> ignoreProperties)
    {
        var cacheKey = $"RuntimeMetadata_{type.ThrowIfNull().FullName}";

        if (type.IsValueType)
            return new Dictionary<string, IPropertyRuntimeMetadata>();

        var dict = Internal.MemoryCache.GetOrCreate<IDictionary<string, IPropertyRuntimeMetadata>>(cacheKey, entry =>
        {
            var dict = new Dictionary<string, IPropertyRuntimeMetadata>();
            var properties = typeof(IRuntimeMetadata).IsAssignableFrom(type) ? GetStaticMetadataForType(type) : GetPropertyRuntimeMetadata(type);
            foreach (var p in properties)
                dict[p.Name] = p;

            entry.SlidingExpiration = SlidingExpirationTimespan;
            return dict;
        })!;

        return ignoreProperties.Any() ? dict.Where(kvp => !ignoreProperties.Contains(kvp.Key)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value) : dict;
    }

    /// <summary>
    /// Dispatches to the static interface metadata provider for a runtime type.
    /// </summary>
    private static IEnumerable<IPropertyRuntimeMetadata> GetStaticMetadataForType(Type type)
    {
        var method = typeof(RuntimeMetadata).GetMethod(nameof(GetStaticMetadata), BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("The static metadata dispatcher cannot be found.");

        var provider = method.MakeGenericMethod(type).CreateDelegate<Func<IEnumerable<IPropertyRuntimeMetadata>>>();
        return provider();
    }

    /// <summary>
    /// Invokes the static metadata interface, including explicit implementations.
    /// </summary>
    private static IEnumerable<IPropertyRuntimeMetadata> GetStaticMetadata<T>() where T : IRuntimeMetadata => T.GetStaticPropertyRuntimeMetadata();

    /// <summary>
    /// Gets the <see cref="IPropertyRuntimeMetadata"/> for each property of the entity <typeparamref name="T"/> <see cref="Type"/> using <see cref="IRuntimeMetadata.GetStaticPropertyRuntimeMetadata"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="IContract{T}"/> <see cref="Type"/>.</typeparam>
    /// <returns>Also, consider the <see cref="GetCachedProperties{T}()"/> equivalent where applicable. Note, the <see cref="IContract{T}"/> types do not require <b>reflection</b>.</returns>
    public static IEnumerable<IPropertyRuntimeMetadata> GetPropertyRuntimeMetadata<T>() where T : IContract<T>
    {
        foreach (var p in T.GetStaticPropertyRuntimeMetadata())
            yield return p;
    }

    /// <summary>
    /// Gets the <see cref="IPropertyRuntimeMetadata"/> for each property of the entity <typeparamref name="T"/> <see cref="Type"/> using <b>reflection</b>.
    /// </summary>
    /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
    /// <param name="ignoreProperties">The list of properties to ignore.</param>
    /// <remarks>This implementation leverages <i>System.Reflection</i>; as such, consider implementing type as <see cref="IContract{T}"/> and use <see cref="GetPropertyRuntimeMetadata{T}()"/> where performance is a premium.
    /// <para>This method bypasses the internal cache; therefore, consider <see cref="GetCachedProperties{T}(IEnumerable{string})"/> to minimize repeated <b>reflection</b> costs.</para></remarks>
    public static IEnumerable<IPropertyRuntimeMetadata> GetPropertyRuntimeMetadata<T>(params IEnumerable<string> ignoreProperties) => GetPropertyRuntimeMetadata(typeof(T), ignoreProperties);

    /// <summary>
    /// Gets the <see cref="IPropertyRuntimeMetadata"/> for each property of the entity <see cref="Type"/> using reflection.
    /// </summary>
    /// <param name="type">The entity <see cref="Type"/></param>
    /// <param name="ignoreProperties">The list of properties to ignore.</param>
    /// <remarks>This implementation leverages <i>System.Reflection</i>; as such, consider implementing type as <see cref="IContract{T}"/> and use <see cref="GetPropertyRuntimeMetadata{T}()"/> where performance is a premium.
    /// <para>This method bypasses the internal cache; therefore, consider <see cref="GetCachedProperties"/> to minimize repeated <b>reflection</b> costs.</para></remarks>
    public static IEnumerable<IPropertyRuntimeMetadata> GetPropertyRuntimeMetadata(Type type, params IEnumerable<string> ignoreProperties)
    {
        if (!type.ThrowIfNull().IsClass || type == typeof(string))
            throw new ArgumentException($"The type '{type.FullName}' must be a class.", nameof(type));

        foreach (var pi in type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.GetMethod is not null && p.GetMethod.IsPublic))
        {
            if (ignoreProperties.Contains(pi.Name))
                continue;

            yield return CreateReflectedPropertyMetadata(type, pi);
        }
    }

    /// <summary>
    /// Creates typed reflected metadata for a single property without invoking its getter.
    /// </summary>
    private static IPropertyRuntimeMetadata CreateReflectedPropertyMetadata(Type type, PropertyInfo property)
    {
        var method = typeof(PropertyRuntimeMetadataReflector).GetMethod(nameof(PropertyRuntimeMetadataReflector.CreatePropertyRuntimeMetadata), BindingFlags.Static | BindingFlags.Public)!;
        var genericMethod = method.MakeGenericMethod(type, property.PropertyType);
        return (IPropertyRuntimeMetadata)genericMethod.Invoke(null, [property])!;
    }
}
