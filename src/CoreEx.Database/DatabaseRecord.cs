using CoreEx.Mapping.Converters;

namespace CoreEx.Database;

/// <summary>
/// Encapsulates the <see cref="DbDataReader"/> to provide requisite column value capabilities.
/// </summary>
/// <param name="database">The owning <see cref="IDatabase"/>.</param>
/// <param name="dataReader">The underlying <see cref="DbDataReader"/>.</param>
public class DatabaseRecord(IDatabase database, DbDataReader dataReader)
{
    private Dictionary<string, int>? _fields;

    /// <summary>
    /// Gets the owning <see cref="IDatabase"/>.
    /// </summary>
    public IDatabase Database { get; } = database.ThrowIfNull();

    /// <summary>
    /// Gets the underlying <see cref="DbDataReader"/>.
    /// </summary>
    public DbDataReader DataReader { get; } = dataReader.ThrowIfNull();

    /// <summary>
    /// Gets the named column value.
    /// </summary>
    /// <param name="columnName">The column name.</param>
    /// <returns>The value.</returns>
    public object? GetValue(string columnName) => GetValue(DataReader.GetOrdinal(columnName.ThrowIfNull()));

    /// <summary>
    /// Gets the specified column value.
    /// </summary>
    /// <param name="ordinal">The ordinal index.</param>
    /// <returns>The value.</returns>
    public object? GetValue(int ordinal)
    {
        // Handle DBNull.
        if (DataReader.IsDBNull(ordinal))
            return default;

        // Good to go!
        var val = DataReader.GetValue(ordinal);
        return val is DateTime dt ? Cleaner.Clean(dt, Database.DateTimeTransform) : val;
    }

    /// <summary>
    /// Gets the named column value.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="columnName">The column name.</param>
    /// <returns>The value.</returns>
    public T GetValue<T>(string columnName) => GetValue<T>(DataReader.GetOrdinal(columnName.ThrowIfNull()));

    /// <summary>
    /// Gets the specified column value.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="ordinal">The ordinal index.</param>
    /// <returns>The value.</returns>
    public T GetValue<T>(int ordinal)
    {
        // Handle DBNull.
        if (DataReader.IsDBNull(ordinal))
            return default!;

        // Handle Nullable<DateOnly>/Nullable<TimeOnly> as it is not directly supported by ADO.NET.
        if (typeof(T) == typeof(Nullable<DateOnly>))
            return Internal.Cast<DateOnly, T>(DataReader.GetFieldValue<DateOnly>(ordinal));
        else if (typeof(T) == typeof(Nullable<TimeOnly>))
            return Internal.Cast<TimeOnly, T>(DataReader.GetFieldValue<TimeOnly>(ordinal));
        else if (typeof(T) == typeof(JsonElement))
            return Internal.Cast<JsonElement?, T>(JsonElementStringConverter.Default.ConvertToSource(DataReader.GetFieldValue<string>(ordinal)));

        // Good to go!
        T val = DataReader.GetFieldValue<T>(ordinal);
        return val is DateTime dt ? Internal.Cast<DateTime, T>(Cleaner.Clean(dt, Database.DateTimeTransform)) : val;
    }

    /// <summary>
    /// Tries to get the specified column value.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="columnName">The column name.</param>
    /// <param name="val">The corresponding value where found.</param>
    /// <returns><see langword="true"/> indicates that the column was found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetValue<T>(string columnName, out T val)
    {
        if (TryGetOrdinal(columnName, out var ordinal))
        {
            val = GetValue<T>(ordinal);
            return true;
        }

        val = default!;
        return false;
    }

    /// <summary>
    /// Gets the specified column value or the default value where not found.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="columnName">The column name.</param>
    /// <param name="defaultValue">The optional default value.</param>
    /// <returns>Tthe specified column value or the default value where not found.</returns>
    public T GetValueOrDefault<T>(string columnName, T defaultValue = default!)
    {
        if (TryGetOrdinal(columnName, out var ordinal))
            return GetValue<T>(ordinal);

        return defaultValue;
    }

    /// <summary>
    /// Gets the named column value as JSON deserialized to the specified <see cref="Type"/>.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="columnName">The column name.</param>
    /// <returns>The value.</returns>
    public T? GetValueFromJson<T>(string columnName) => GetValueFromJson<T>(DataReader.GetOrdinal(columnName.ThrowIfNull()));

    /// <summary>
    /// Gets the specified column value as JSON deserialized to the specified <see cref="Type"/>.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="ordinal">The ordinal index.</param>
    /// <returns>The value.</returns>
    public T? GetValueFromJson<T>(int ordinal)
    {
        if (DataReader.IsDBNull(ordinal))
            return default!;

        var json = DataReader.GetFieldValue<string>(ordinal);
        return json is null ? default! : JsonSerializer.Deserialize<T>(json, Database.JsonSerializerOptions) ?? default!;
    }

    /// <summary>
    /// Indicates whether the named column is <see cref="DBNull"/>.
    /// </summary>
    /// <param name="columnName">The column name.</param>
    /// <param name="ordinal">The corresponding ordinal for the column name.</param>
    /// <returns><see langword="true"/> indicates that the column value has a <see cref="DBNull"/> value; otherwise, <see langword="false"/>.</returns>
    public bool IsDBNull(string columnName, out int ordinal)
    {
        ordinal = DataReader.GetOrdinal(columnName.ThrowIfNull());
        return DataReader.IsDBNull(ordinal);
    }

    /// <summary>
    /// Gets the named <c>RowVersion</c> column as a <see cref="string"/>.
    /// </summary>
    /// <param name="columnName">The name of the column; otherwise, uses <see cref="DatabaseColumns.RowVersionName"/>.</param>
    /// <returns>The resultant value.</returns>
    /// <remarks>The <b>RowVersion</b> column will be converted to a <see cref="string"/> using the <see cref="IDatabase.RowVersionConverter"/>.</remarks>
    public string? GetRowVersion(string? columnName = null)
    {
        var i = DataReader.GetOrdinal(!string.IsNullOrEmpty(columnName) ? columnName : Database.NamedColumns.RowVersionName);
        var v = DataReader.GetValue(i);
        return Database.RowVersionConverter.ConvertToSource(v) ?? null;
    }

    /// <summary>
    /// Tries to get the ordinal for the specified column name.
    /// </summary>
    /// <param name="columnName">The name of the column.</param>
    /// <param name="ordinal">The corresponding ordinal index where found.</param>
    /// <returns><see langword="true"/> indicates that the column was found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetOrdinal(string columnName, out int ordinal)
    {
        // Load the fields dictionary on first use.
        if (_fields is null)
        {
            _fields = [];
            for (int i = 0; i < DataReader.FieldCount; i++)
            {
                _fields[DataReader.GetName(i)] = i;
            }
        }

        // Try to get the ordinal.
        return _fields.TryGetValue(columnName, out ordinal);
    }
}