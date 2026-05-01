[assembly: InternalsVisibleTo("CoreEx.AspNetCore")]
[assembly: InternalsVisibleTo("CoreEx.Azure.Messaging.ServiceBus")]
[assembly: InternalsVisibleTo("CoreEx.Database")]
[assembly: InternalsVisibleTo("CoreEx.RefData")]
[assembly: InternalsVisibleTo("CoreEx.Validation")]

namespace CoreEx.Abstractions;

/// <summary>
/// Provides shareable internal capabilities.
/// </summary>
/// <remarks>This is intended largely for internal usage only; use with caution.</remarks>
public static class Internal
{
    private static readonly IConfiguration _emptyConfig = new ConfigurationBuilder().Build();
    private static IMemoryCache? _fallbackCache;

    /// <summary>
    /// Gets an empty configuration instance that contains no settings or values.
    /// </summary>
    public static IConfiguration EmptyConfiguration => _emptyConfig;

    /// <summary>
    /// Gets the internal cache service key.
    /// </summary>
    internal static string CacheServiceKey = "__coreex_internal_cache";

    /// <summary>
    /// Gets the <b>CoreEx</b> internal <see cref="IMemoryCache"/>.
    /// </summary>
    internal static IMemoryCache MemoryCache => ExecutionContext.GetKeyedService<IMemoryCache>(CacheServiceKey) ?? (_fallbackCache ??= new MemoryCache(new MemoryCacheOptions()));

    /// <summary>
    /// Tries to get the configuration value for the specified <paramref name="key"/>.
    /// </summary>
    /// <typeparam name="T">The configuration value <see cref="Type"/>.</typeparam>
    /// <param name="key">The key of the configuration section.</param>
    /// <param name="value">The converted value.</param>
    /// <param name="configuration">The optional <see cref="IConfiguration"/>; otherwise, defaults from <see cref="ExecutionContext"/>.</param>
    /// <returns><see langword="true"/> indicates the configuration setting exists; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetConfigurationValue<T>(string key, out T? value, IConfiguration? configuration = null)
    {
        var config = configuration ?? ExecutionContext.GetService<IConfiguration>();
        if (config?.GetSection(key)?.Value is null)
        {
            value = default!;
            return false;
        }

        value = config.ThrowWhen(config => config is null).GetValue<T>(key);
        return true;
    }

    /// <summary>
    /// Gets the configuration value for the specified <paramref name="key"/>.
    /// </summary>
    /// <typeparam name="T">The configuration value <see cref="Type"/>.</typeparam>
    /// <param name="key">The key of the configuration section.</param>
    /// <param name="defaultValue">The default value to use if no value is found.</param>
    /// <param name="configuration">The optional <see cref="IConfiguration"/>; otherwise, defaults from <see cref="ExecutionContext"/>.</param>
    /// <returns>The converted value.</returns>
    public static T? GetConfigurationValue<T>(string key, T? defaultValue = default, IConfiguration? configuration = null)
        => TryGetConfigurationValue<T>(key, out var value, configuration) ? value : defaultValue;

    /// <summary>
    /// Gets the configuration value for the specified <paramref name="key"/> and where not found uses the alternate <paramref name="fallbackKey"/>.
    /// </summary>
    /// <typeparam name="T">The configuration value <see cref="Type"/>.</typeparam>
    /// <param name="key">The key of the configuration section.</param>
    /// <param name="fallbackKey">The alternate key of the configuration section.</param>
    /// <param name="defaultValue">The default value to use if no value is found.</param>
    /// <param name="configuration">The optional <see cref="IConfiguration"/>; otherwise, defaults from <see cref="ExecutionContext"/>.</param>
    /// <returns>The converted value.</returns>
    public static T? GetConfigurationValueWithFallback<T>(string key, string fallbackKey, T? defaultValue = default, IConfiguration? configuration = null)
        => GetConfigurationValueWithFallback(key, () => fallbackKey, defaultValue, configuration);

    /// <summary>
    /// Gets the configuration value for the specified <paramref name="key"/> and where not found uses the alternate <paramref name="getFallbackKey"/>.
    /// </summary>
    /// <typeparam name="T">The configuration value <see cref="Type"/>.</typeparam>
    /// <param name="key">The key of the configuration section.</param>
    /// <param name="getFallbackKey">The function to get the alternate key of the configuration section (only where needed).</param>
    /// <param name="defaultValue">The default value to use if no value is found.</param>
    /// <param name="configuration">The optional <see cref="IConfiguration"/>; otherwise, defaults from <see cref="ExecutionContext"/>.</param>
    /// <returns>The converted value.</returns>
    /// <remarks>This is intended to minimize the need to generate a fallback key until actually needed.</remarks>
    public static T? GetConfigurationValueWithFallback<T>(string key, Func<string> getFallbackKey, T? defaultValue = default, IConfiguration? configuration = null)
    {
        var config = configuration ?? ExecutionContext.GetService<IConfiguration>();
        if (config is null)
            return defaultValue;

        if (TryGetConfigurationValue<T>(key, out var value, config))
            return value;

        return GetConfigurationValue(getFallbackKey.ThrowIfNull()(), defaultValue, config);
    }

    /// <summary>
    /// Gets the value from the provided <paramref name="valueOrKey"/> where it is a reference to a <paramref name="configuration"/> key; otherwise, returns the value as-is.
    /// </summary>
    /// <param name="valueOrKey">The value or key.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/>.</param>
    /// <returns>The configured value or value as-is.</returns>
    /// <remarks>Supports the retrieval of the value from <paramref name="configuration"/> where prefixed with '<c>config:</c>' or '<c>^</c>', or is wrapped with '<c>%</c>'.</remarks>
    public static string GetValueFromConfigurationWhereApplicable(string valueOrKey, IConfiguration? configuration = null)
    {
        const string configPrefix = "config:";
        bool isConfigReference = false;

        if (valueOrKey.ThrowIfNullOrEmpty().StartsWith(configPrefix, StringComparison.OrdinalIgnoreCase))
        {
            valueOrKey = valueOrKey[configPrefix.Length..];
            isConfigReference = true;
        }
        else if (valueOrKey.StartsWith('^'))
        {
            valueOrKey = valueOrKey[1..];
            isConfigReference = true;
        }
        else if (valueOrKey.StartsWith('%') && valueOrKey.EndsWith('%'))
        {
            valueOrKey = valueOrKey[1..^1];
            isConfigReference = true;
        }

        return isConfigReference
            ? GetConfigurationValue<string>(valueOrKey, configuration: configuration) ?? throw new InvalidOperationException($"The required configuration key '{valueOrKey}' was not found.")
            : valueOrKey;
    }

    /// <summary>
    /// Casts a <paramref name="value"/> of type <typeparamref name="TFrom"/> to type <typeparamref name="TTo"/> (where they must be the same type).
    /// </summary>
    /// <typeparam name="TFrom">The from <see cref="Type"/>.</typeparam>
    /// <typeparam name="TTo">The to <see cref="Type"/>.</typeparam>
    /// <param name="value">The value to cast.</param>
    /// <param name="trustMe">Indicates whether to bypass the type check; use with extreme caution!</param>
    /// <returns>The casted value.</returns>
    /// <remarks>This uses a <see cref="Unsafe.As{TFrom, TTo}(ref TFrom)"/> to perform as it is fastest means possible. Where there is a mismatch of types an <see cref="InvalidCastException"/> will be thrown.</remarks>
    internal static TTo Cast<TFrom, TTo>(TFrom value, bool trustMe = false)
    {
        if (value is null)
            return default!;

        if (value is TTo t)
            return t;

        // Must be identical types, uses Unsafe.As to avoid boxing of value types.
        if (typeof(TFrom) == typeof(TTo))
            return Unsafe.As<TFrom, TTo>(ref value);

        // Allow bypass of type check where caller is certain of compatibility (use with extreme caution).
        if (trustMe)
            return Unsafe.As<TFrom, TTo>(ref value);

        // Not compatible.
        throw new InvalidCastException($"Cannot cast from {typeof(TFrom).Name} to {typeof(TTo).Name}. Only exact matches are supported.");
    }

    /// <summary>
    /// Gets a friendly formatted <see cref="Type"/> name with namespace.
    /// </summary>
    /// <param name="type">The <see cref="Type"/>.</param>
    /// <returns>The friendly formatted <see cref="Type"/> name with namespace.</returns>
    public static string GetNamespaceFormattedName(Type type) => type.Namespace is null ? GetFormattedName(type) : $"{type.Namespace}.{GetFormattedName(type)}";

    /// <summary>
    /// Gets a friendly formatted <see cref="Type"/> name.
    /// </summary>
    /// <param name="type">The <see cref="Type"/>.</param>
    /// <returns>The friendly formatted <see cref="Type"/> name.</returns>
    public static string GetFormattedName(Type type) => type.IsGenericType ? GetGenericFormattedName(type) : type.Name;

    /// <summary>
    /// Gets the formatted name for a generic type.
    /// </summary>
    private static string GetGenericFormattedName(Type type)
    {
        var name = type.Name;
        var tick = name.IndexOf('`');
        if (tick > 0)
            name = name[..tick];

        return $"{name}<{string.Join(',', type.GetGenericArguments().Select(GetFormattedName))}>";
    }
}