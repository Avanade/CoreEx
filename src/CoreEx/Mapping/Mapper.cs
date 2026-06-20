namespace CoreEx.Mapping;

/// <summary>
/// Provides utility capabilities for mapping.
/// </summary>
public static class Mapper
{
    /// <summary>
    /// Maps the standard properties from a <paramref name="source"/> value into a new <typeparamref name="TDestination"/> value.
    /// </summary>
    /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
    /// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
    /// <param name="destination">The destination value.</param>
    /// <param name="source">The source value.</param>
    /// <param name="mapChangeLog">Indicates whether to map the <i>change log</i> properties.</param>
    /// <returns>The <paramref name="destination"/> to support fluent-style method-chaining.</returns>
    /// <remarks>Standard properties are mapped based on whether both the <paramref name="source"/> and <paramref name="destination"/> implement the following respectively: 
    ///   <list type="bullet">
    ///     <item><see cref="IReadOnlyIdentifier"/> -> <see cref="IIdentifier"/></item>
    ///     <item><see cref="IReadOnlyETag"/> -> <see cref="IETag"/></item>
    ///     <item><see cref="IReadOnlyTenantId"/> -> <see cref="ITenantId"/></item>
    ///     <item><see cref="IReadOnlyPartitionKey"/> -> <see cref="IPartitionKey"/></item>
    ///     <item><see cref="IReadOnlyLogicallyDeleted"/> -> <see cref="ILogicallyDeleted"/></item>
    ///     <item><see cref="IReadOnlyTypeDiscriminator"/> -> <see cref="ITypeDiscriminator"/></item>
    ///     <item><see cref="IReadOnlyChangeLog"/> -> <see cref="IChangeLog"/> or <see cref="IChangeLogEx"/></item>
    ///     <item><see cref="IReadOnlyChangeLogEx"/> -> <see cref="IChangeLog"/> or <see cref="IChangeLogEx"/></item>
    ///   </list>
    ///   <para>See also <see cref="MapChangeLogInto{TSource, TDestination}(TSource, TDestination)"/>.</para>
    /// </remarks>
    public static TDestination MapStandardFrom<TSource, TDestination>(this TDestination destination, TSource source, bool mapChangeLog = true) where TSource : class where TDestination : class
    {
        MapStandardInto<TSource, TDestination>(source, destination, mapChangeLog);
        return destination;
    }

    /// <summary>
    /// Maps the standard properties from a <paramref name="source"/> value into an existing <paramref name="destination"/> value.
    /// </summary>
    /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
    /// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination value.</param>
    /// <param name="mapChangeLog">Indicates whether to map the <i>change log</i> properties.</param>
    /// <remarks>Standard properties are mapped based on whether both the <paramref name="source"/> and <paramref name="destination"/> implement the following respectively: 
    ///   <list type="bullet">
    ///     <item><see cref="IReadOnlyIdentifier"/> -> <see cref="IIdentifier"/></item>
    ///     <item><see cref="IReadOnlyETag"/> -> <see cref="IETag"/></item>
    ///     <item><see cref="IReadOnlyTenantId"/> -> <see cref="ITenantId"/></item>
    ///     <item><see cref="IReadOnlyPartitionKey"/> -> <see cref="IPartitionKey"/></item>
    ///     <item><see cref="IReadOnlyLogicallyDeleted"/> -> <see cref="ILogicallyDeleted"/></item>
    ///     <item><see cref="IReadOnlyTypeDiscriminator"/> -> <see cref="ITypeDiscriminator"/></item>
    ///     <item><see cref="IReadOnlyChangeLog"/> -> <see cref="IChangeLog"/> or <see cref="IChangeLogEx"/></item>
    ///     <item><see cref="IReadOnlyChangeLogEx"/> -> <see cref="IChangeLog"/> or <see cref="IChangeLogEx"/></item>
    ///   </list>
    ///   <para>See also <see cref="MapChangeLogInto{TSource, TDestination}(TSource, TDestination)"/>.</para>
    /// </remarks>
    public static void MapStandardInto<TSource, TDestination>(TSource source, TDestination destination, bool mapChangeLog = true) where TSource : class where TDestination : class
    {
        if (ReferenceEquals(source.ThrowIfNull(), destination.ThrowIfNull()))
            return;

        if (source is IReadOnlyIdentifier si && destination is IIdentifier di && si.IdType == di.IdType)
            di.Id = si.Id;

        if (source is IReadOnlyETag setag && destination is IETag detag)
            detag.ETag = setag.ETag;

        if (source is IReadOnlyTenantId sti && destination is ITenantId dti)
            dti.TenantId = sti.TenantId;

        if (source is IReadOnlyPartitionKey spk && destination is IPartitionKey dpk)
            dpk.PartitionKey = spk.PartitionKey;

        if (source is IReadOnlyLogicallyDeleted sld && destination is ILogicallyDeleted dld)
            dld.IsDeleted = sld.IsDeleted;

        if (source is IReadOnlyTypeDiscriminator std && destination is ITypeDiscriminator dtd)
            dtd.TypeDiscriminator = std.TypeDiscriminator;

        if (mapChangeLog)
            MapChangeLogInto<TSource, TDestination>(source, destination);
    }

    /// <summary>
    /// Maps the standard change log properties from a <paramref name="source"/> value into an existing <paramref name="destination"/> changing shape as required.
    /// </summary>
    /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
    /// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination value.</param>
    public static void MapChangeLogInto<TSource, TDestination>(TSource source, TDestination destination) where TSource : class where TDestination : class
    {
        if (ReferenceEquals(source.ThrowIfNull(), destination.ThrowIfNull()))
            return;

        if (destination is IChangeLog dcl)
            MapChangeLogInto<TSource, TDestination>(source, dcl);

        if (destination is IChangeLogEx dclex)
            MapChangeLogInto<TSource, TDestination>(source, dclex);
    }

    /// <summary>
    /// Maps the standard change log properties from a <paramref name="source"/> value into an existing <paramref name="destination"/> (<see cref="IChangeLog"/>) value (where applicable).
    /// </summary>
    private static void MapChangeLogInto<TSource, TDestination>(TSource source, IChangeLog destination) where TSource : class
    {
        if (source is IReadOnlyChangeLog scl)
            destination.ChangeLog = new ChangeLog(scl);
        else if (source is IReadOnlyChangeLogEx sclex)
            destination.ChangeLog = new ChangeLog(sclex);

        if (destination.ChangeLog?.IsDefault() ?? false)
            destination.ChangeLog = null;
    }

    /// <summary>
    /// Maps the standard change log properties from a <paramref name="source"/> value into an existing <paramref name="destination"/> (<see cref="IChangeLogEx"/> value (where applicable).
    /// </summary>
    private static void MapChangeLogInto<TSource, TDestination>(TSource source, IChangeLogEx destination) where TSource : class
    {
        static void MapInto(IReadOnlyChangeLogEx scl, IChangeLogEx dcl)
        {
            dcl.CreatedBy = scl.CreatedBy;
            dcl.CreatedOn = scl.CreatedOn;
            dcl.UpdatedBy = scl.UpdatedBy;
            dcl.UpdatedOn = scl.UpdatedOn;
        }

        if (source is IReadOnlyChangeLogEx sclex)
            MapInto(sclex, destination);
        else if (source is IReadOnlyChangeLog scl)
            MapInto(scl?.ChangeLog is null ? ChangeLog.Empty : scl.ChangeLog, destination);
    }

    /// <summary>
    /// Creates a <see cref="OneOffMapper{TSource, TDestination}"/> using the specified <paramref name="map"/> function.
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TDestination"></typeparam>
    /// <param name="map">The mapping function.</param>
    /// <returns>The <see cref="OneOffMapper{TSource, TDestination}"/>.</returns>
    public static OneOffMapper<TSource, TDestination> Create<TSource, TDestination>(Func<TSource, TDestination> map) where TSource : class where TDestination : class => new(map);

    /// <summary>
    /// Provides a one-off runtime instantiated <see cref="IMapper"/>.
    /// </summary>
    /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
    /// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
    public sealed class OneOffMapper<TSource, TDestination> : Mapper<TSource, TDestination> where TSource : class where TDestination : class
    {
        private readonly Func<TSource, TDestination> _map;

        /// <summary>
        /// Initializes a new instance of the <see cref="OneOffMapper{TSource, TDestination}"/> class.
        /// </summary>
        /// <param name="map">The mapping function.</param>
        internal OneOffMapper(Func<TSource, TDestination> map) => _map = map.ThrowIfNull();

        /// <inheritdoc/>
        protected override TDestination OnMap(TSource source) => _map(source);
    }

    /// <summary>
    /// Creates a <see cref="OneOffIntoMapper{TSource, TDestination}"/> using the specified <paramref name="map"/> action.
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TDestination"></typeparam>
    /// <param name="map">The mapping action.</param>
    /// <returns>The <see cref="OneOffMapper{TSource, TDestination}"/>.</returns>
    public static OneOffIntoMapper<TSource, TDestination> CreateInto<TSource, TDestination>(Action<TSource, TDestination> map) where TSource : class where TDestination : class => new(map);

    /// <summary>
    /// Provides a one-off runtime instantiated <see cref="IIntoMapper"/>.
    /// </summary>
    /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
    /// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
    public sealed class OneOffIntoMapper<TSource, TDestination> : IntoMapper<TSource, TDestination> where TSource : class where TDestination : class
    {
        private readonly Action<TSource, TDestination> _map;

        /// <summary>
        /// Initializes a new instance of the <see cref="OneOffMapper{TSource, TDestination}"/> class.
        /// </summary>
        /// <param name="map">The mapping action.</param>
        internal OneOffIntoMapper(Action<TSource, TDestination> map) => _map = map.ThrowIfNull();

        /// <inheritdoc/>
        protected override void OnMapInto(TSource source, TDestination destination) => _map(source, destination);
    }
}