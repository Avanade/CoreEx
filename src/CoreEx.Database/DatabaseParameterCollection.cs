using CoreEx.Mapping.Converters;

namespace CoreEx.Database;

/// <summary>
/// Provides a <see cref="DbParameter"/> collection used for a <see cref="DbCommand"/>.
/// </summary>
/// <param name="database">The <see cref="IDatabase"/>.</param>
public sealed class DatabaseParameterCollection(IDatabase database) : ICollection<DbParameter>, IDatabaseParameters<DatabaseParameterCollection>
{
    private readonly List<DbParameter> _parameters = [];

    /// <summary>
    /// Gets the underlying <see cref="IDatabase"/>.
    /// </summary>
    public IDatabase Database { get; } = database.ThrowIfNull();

    /// <inheritdoc/>
    DatabaseParameterCollection IDatabaseParameters<DatabaseParameterCollection>.Parameters => this;

    /// <inheritdoc/>
    public int Count => _parameters.Count;

    /// <inheritdoc/>
    bool ICollection<DbParameter>.IsReadOnly => false;

    /// <summary>
    /// Indicates whether a <see cref="DbParameter"/> with the specified <paramref name="name"/> exists in the collection.
    /// </summary>
    /// <param name="name">The parameter name.</param>
    /// <returns><see langword="true"/> indicates that the parameter exists in the collection; otherwise, <see langword="false"/>.</returns>
    public bool Contains(string name) => _parameters.Any(x => x.ParameterName == name);

    /// <summary>
    /// Adds the named parameter and value, using the specified <paramref name="direction"/>, to the <see cref="DbCommand.Parameters"/>.
    /// </summary>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The parameter value.</param>
    /// <param name="dbType">The parameter <see cref="DbType"/>.</param>
    /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
    /// <returns>The <see cref="DbParameter"/>.</returns>
    public DbParameter AddParameter(string name, object? value = null, DbType? dbType = null, ParameterDirection direction = ParameterDirection.Input)
    {
        var p = Database.CreateParameter();
        p.ParameterName = ParameterizeName(name);
        p.Value = ConvertToDbValue(value, Database);
        p.Direction = direction;
        if (dbType.HasValue)
            p.DbType = dbType.Value;

        _parameters.Add(p);
        return p;
    }

    /// <summary>
    /// Adds the named parameter and value, using the specified <paramref name="direction"/>, to the <see cref="DbCommand.Parameters"/>.
    /// </summary>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The parameter value.</param>
    /// <param name="dbType">The parameter <see cref="DbType"/>.</param>
    /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
    /// <returns>The <see cref="DbParameter"/>.</returns>
    public DbParameter AddParameter<T>(string name, T? value, DbType? dbType = null, ParameterDirection direction = ParameterDirection.Input)
    {
        var p = Database.CreateParameter();
        p.ParameterName = ParameterizeName(name);
        p.Value = ConvertToDbValue(value, Database);
        p.Direction = direction;
        if (dbType.HasValue)
            p.DbType = dbType.Value;

        _parameters.Add(p);
        return p;
    }

    /// <summary>
    /// Adds the named parameter and value, using the specified <paramref name="dbType"/>, <paramref name="size"/> and <paramref name="direction"/>, to the <see cref="DbCommand.Parameters"/>.
    /// </summary>
    /// <param name="name">The parameter name.</param>
    /// <param name="dbType">The parameter <see cref="DbType"/>.</param>
    /// <param name="size">The optional maximum size (in bytes).</param>
    /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
    /// <returns>The <see cref="DbParameter"/>.</returns>
    public DbParameter AddParameter(string name, DbType dbType, int? size = null, ParameterDirection direction = ParameterDirection.Input)
    {
        var p = Database.CreateParameter();
        p.ParameterName = ParameterizeName(name);
        p.DbType = dbType;
        p.Direction = direction;
        if (size.HasValue)
            p.Size = size.Value;

        _parameters.Add(p);
        return p;
    }

    /// <summary>
    /// Adds the named <paramref name="wildcard"/> parameter to the <see cref="DbCommand.Parameters"/>.
    /// </summary>
    /// <param name="name">The parameter name.</param>
    /// <param name="wildcard">The wildcard value.</param>
    /// <returns>The <see cref="DbParameter"/>.</returns>
    public DbParameter AddWildcardParameter(string name, string? wildcard) => AddParameter(name, wildcard is null ? null : Database.Wildcard.Replace(wildcard));

    /// <summary>
    /// Adds the named parameter and value serialized as a JSON <see cref="string"/> to the <see cref="DbCommand.Parameters"/>.
    /// </summary>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The parameter value.</param>
    /// <returns>The <see cref="DbParameter"/>.</returns>
    /// <remarks>Where the <paramref name="value"/> is <see langword="null"/> then <see cref="DBNull.Value"/> will be used.</remarks>
    public DbParameter AddJsonParameter<T>(string name, T? value)
    {
        var p = Database.CreateParameter();
        p.ParameterName = ParameterizeName(name);
        if (value is null)
            p.Value = DBNull.Value;
        else
            p.Value = JsonSerializer.Serialize(value, Database.JsonSerializerOptions);

        _parameters.Add(p);
        return p;
    }

    /// <summary>
    /// Adds an <see cref="int"/> <see cref="ParameterDirection.ReturnValue"/> parameter to the <see cref="DbCommand.Parameters"/>
    /// </summary>
    /// <returns>The <see cref="DbParameter"/>.</returns>
    public DbParameter AddReturnValueParameter()
    {
        var p = Database.CreateParameter();
        p.ParameterName = ParameterizeName(Database.NamedColumns.ReturnValueName);
        p.DbType = DbType.Int32;
        p.Direction = ParameterDirection.ReturnValue;

        _parameters.Add(p);
        return p;
    }

    /// <summary>
    /// Adds a named parameter (<see cref="DatabaseColumns.ReselectRecordName"/>) to <paramref name="reselect"/> the data to the <see cref="DbCommand.Parameters"/>
    /// </summary>
    /// <param name="reselect">Indicates whether to reselect after the operation.</param>
    /// <returns>The <see cref="DbParameter"/>.</returns>
    public DbParameter AddReselectRecordParam(bool reselect = true) => AddParameter(Database.NamedColumns.ReselectRecordName, reselect);

    /// <summary>
    /// Adds a named parameter (<see cref="DatabaseColumns.RowVersionName"/>) using the <see cref="IReadOnlyETag.ETag"/> <see cref="IDatabase.RowVersionConverter"/> to the <see cref="DbCommand.Parameters"/>.
    /// </summary>
    /// <param name="value">The <see langword="string"/>-based <see cref="IReadOnlyETag.ETag"/> representation of the row version.</param>
    /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
    /// <returns>The <see cref="DbParameter"/>.</returns>
    public DbParameter AddRowVersionParam(string? value, ParameterDirection direction = ParameterDirection.Input) 
        => AddParameter(Database.NamedColumns.RowVersionName, Database.RowVersionConverter.ConvertToDestination(value), direction: direction);

    /// <summary>
    /// Parameterizes the name by ensuring it starts with an '@' character.
    /// </summary>
    /// <param name="name">The parameter name.</param>
    /// <returns>The parameterized name.</returns>
    public static string ParameterizeName(string name) => name.ThrowIfNull().StartsWith('@') ? name : $"@{name}";

    /// <summary>
    /// Converts the specified <paramref name="value"/> to a database-compatible value.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <param name="database">The <see cref="IDatabase"/>.</param>
    /// <returns>The converted database-compatible value.</returns>
    public static object? ConvertToDbValue<T>(T value, IDatabase database) => value is null
        ? DBNull.Value
        : (value is DateTimeOffset dto && database.DateTimeOffsetTransform ? dto.ToUniversalTime() // Convert to UTC. https://www.tinybird.co/blog/database-timestamps-timezone
            : (value is JsonElement je ? JsonElementStringConverter.Default.ConvertToDestination(je) : value));

    /// <summary>
    /// Gets or sets the <see cref="DbParameter"/> at the specified <paramref name="index"/>.
    /// </summary>
    /// <param name="index">The zero-based index.</param>
    /// <returns>The <see cref="DbParameter"/>.</returns>
    public DbParameter this[int index] => _parameters[index];

    /// <inheritdoc/>
    public void Add(DbParameter item) => _parameters.Add(item);

    /// <summary>
    /// Adds <see cref="DbParameter"/> <paramref name="list"/>.
    /// </summary>
    /// <param name="list">The <see cref="DbParameter"/> list to add.</param>
    public void AddRange(IEnumerable<DbParameter> list) => _parameters.AddRange(list);

    /// <inheritdoc/>
    public void Clear() => _parameters.Clear();

    /// <inheritdoc/>
    public bool Contains(DbParameter item) => _parameters.Contains(item);

    /// <inheritdoc/>
    public void CopyTo(DbParameter[] array, int arrayIndex) => _parameters.CopyTo(array, arrayIndex);

    /// <inheritdoc/>
    public bool Remove(DbParameter item) => _parameters.Remove(item);

    /// <inheritdoc/>
    public IEnumerator<DbParameter> GetEnumerator() => _parameters.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}