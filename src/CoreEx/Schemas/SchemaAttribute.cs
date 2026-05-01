namespace CoreEx.Schemas;

/// <summary>
/// Provides the latest schema metadata for an entity.
/// </summary>
/// <param name="version">The entity schema <see cref="System.Version"/>.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class SchemaAttribute(string? version = null) : Attribute
{
    private static readonly ConcurrentDictionary<Type, Lazy<(bool, SchemaAttribute)>> _cache = new();

    private string? _schema;
    private string? _versionString;

    /// <summary>
    /// Gets the latest entity schema <see cref="System.Version"/>.
    /// </summary>
    /// <remarks>Defaults to <see cref="Schema.DefaultVersion"/>.</remarks>
    public Version Version { get; } = ParseVersion(version);

    /// <summary>
    /// Gets the <see cref="Version"/> as a formatted <see cref="string"/>.
    /// </summary>
    public string VersionString => _versionString ??= Version.ToString();

    /// <summary>
    /// Gets or sets the entity name.
    /// </summary>
    /// <remarks>This defaults to the entity <see cref="Type"/> <see cref="MemberInfo.Name"/>.</remarks>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the entity schema <see cref="Uri"/>.
    /// </summary>
    public string? SchemaUri { get => _schema; set => _schema = value is null ? null : new Uri(value, UriKind.RelativeOrAbsolute).ToString(); }

    /// <summary>
    /// Parses the <paramref name="version"/> into a <see cref="System.Version"/> value.
    /// </summary>
    /// <param name="version">The version string.</param>
    /// <returns>The <see cref="System.Version"/>.</returns>
    internal static Version ParseVersion(string? version)
    {
        if (string.IsNullOrEmpty(version))
            return Schema.DefaultVersion;
        else if (version.Contains('.'))
            return new Version(version);
        else
            return new Version(int.Parse(version), 0);
    }

    /// <summary>
    /// Tries to get the configured <see cref="SchemaAttribute"/> for the specified <paramref name="type"/>; defaults where not found.
    /// </summary>
    /// <param name="type">The entity <see cref="Type"/>.</param>
    /// <param name="attribute">The configured <see cref="SchemaAttribute"/> where found; otherwise, a defaulted instance.</param>
    /// <returns><see langword="true"/> where found; otherwise, <see langword="false"/>.</returns>
    internal static bool TryGetCustomAttribute(Type type, out SchemaAttribute attribute)
    {
        (bool exists, SchemaAttribute sa) = _cache.GetOrAdd(type.ThrowIfNull(), type => new Lazy<(bool, SchemaAttribute)>(() =>
        {
            var ea = type.GetCustomAttribute<SchemaAttribute>();
            var exists = ea is not null;

            ea ??= new SchemaAttribute();
            ea.Name ??= type.Name;

            return (exists, ea);
        })).Value;

        attribute = sa;
        return exists;
    }
}