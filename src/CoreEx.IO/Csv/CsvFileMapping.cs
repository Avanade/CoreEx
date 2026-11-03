namespace CoreEx.IO.Csv;

/// <summary>Declares named or positional typed mappings with sanitized, structured conversion diagnostics.</summary>
/// <typeparam name="TRecord">The wire record type.</typeparam>
/// <remarks>Create a fresh mapping for each pass. Explicit converters distinguish blank input from invalid conversion and requiredness. Only current-record diagnostics are retained.</remarks>
public sealed class CsvFileMapping<TRecord> where TRecord : class, new()
{
    private readonly List<Column> _columns = [];

    /// <summary>Gets whether at least one named selector requires a column-name header.</summary>
    public bool RequiresHeader => _columns.Any(x => !x.Index.HasValue);

    /// <summary>Maps a named header or zero-based positional field to a writable property.</summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="name">The declared column name, also used in diagnostics.</param>
    /// <param name="property">The writable target property expression.</param>
    /// <param name="converter">The explicit non-throwing field converter.</param>
    /// <param name="index">The zero-based field index; null selects the name from a case-insensitive header.</param>
    /// <returns>This mapping for fluent configuration.</returns>
    public CsvFileMapping<TRecord> MapProperty<TProperty>(string name, Expression<Func<TRecord, TProperty>> property, CsvFieldConverter<TProperty> converter, int? index = null)
    {
        name.ThrowIfNullOrWhiteSpace();
        property.ThrowIfNull();
        converter.ThrowIfNull();
        if (index.HasValue)
            index.Value.ThrowIfLessThanZero(paramName: nameof(index));

        if (_columns.Any(x => StringComparer.OrdinalIgnoreCase.Equals(x.Name, name)))
            throw new ArgumentException("Column names must be unique.", nameof(name));

        if (property.Body is not MemberExpression member || member.Member is not System.Reflection.PropertyInfo)
            throw new ArgumentException("The expression must select a writable property.", nameof(property));

        var value = Expression.Parameter(typeof(TProperty), "value");
        var assign = Expression.Lambda<Action<TRecord, TProperty>>(Expression.Assign(property.Body, value), property.Parameters[0], value).Compile();
        _columns.Add(new(name, index, member.Member.Name, (record, text) =>
        {
            if (!converter(text, out var converted))
                return false;

            assign(record, converted);
            return true;
        }));

        return this;
    }

    /// <summary>Creates isolated parsing state for this mapping definition.</summary>
    internal Func<CsvReader, CsvInputRecord<TRecord>> CreateMapping()
    {
        if (_columns.Count == 0)
            throw new InvalidOperationException("At least one CSV column must be mapped.");

        return new InputMapping([.. _columns]).Map;
    }

    /// <summary>Converts and assigns one decoded field.</summary>
    private delegate bool AssignField(TRecord record, ReadOnlySpan<char> text);

    /// <summary>Contains one declared selector and its typed assignment.</summary>
    private sealed record Column(string Name, int? Index, string PropertyPath, AssignField Assign);

    /// <summary>Resolves headers and maps decoded fields without exception control flow for content errors.</summary>
    private sealed class InputMapping(Column[] columns)
    {
        private readonly bool _requiresHeader = columns.Any(x => !x.Index.HasValue);
        private int[]? _indices;
        private bool _headerRead;

        /// <summary>Maps the current parser record before its buffer advances.</summary>
        internal CsvInputRecord<TRecord> Map(CsvReader reader)
        {
            if (_requiresHeader && !_headerRead)
            {
                _headerRead = true;
                var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var diagnostics = new List<FileDiagnostic>();
                for (var index = 0; index < reader.FieldCount; index++)
                    if (!headers.TryAdd(reader.GetString(index), index))
                        diagnostics.Add(new(FileDiagnosticCategory.Structure, "csv.header.duplicate", "A column-name header is duplicated.", location: new(reader.LineNumber, fieldIndex: index)));

                foreach (var column in columns.Where(x => !x.Index.HasValue))
                    if (!headers.ContainsKey(column.Name))
                        diagnostics.Add(new(FileDiagnosticCategory.Structure, "csv.header.missing", "A required column-name header is missing.", location: new(reader.LineNumber, fieldName: column.Name, propertyPath: column.PropertyPath)));

                if (diagnostics.Count == 0)
                    _indices = columns.Select(column => column.Index ?? headers[column.Name]).ToArray();

                return new(null, true, diagnostics);
            }

            if (_indices is null)
            {
                if (_requiresHeader)
                    throw new InvalidOperationException("Cannot map data after an invalid header.");

                _indices = columns.Select(column => column.Index.GetValueOrDefault()).ToArray();
            }

            var record = new TRecord();
            var errors = new List<FileDiagnostic>();
            for (var i = 0; i < columns.Length; i++)
            {
                var column = columns[i];
                var index = _indices[i];
                var missing = index >= reader.FieldCount;
                if (missing || !column.Assign(record, reader.GetString(index).AsSpan()))
                    errors.Add(new(FileDiagnosticCategory.Mapping, missing ? "csv.field.missing" : "csv.field.invalid",
                        missing ? "A declared field is absent from this record." : "A field could not be converted to its declared type.",
                        location: new(reader.LineNumber, fieldIndex: index, fieldName: column.Name, propertyPath: column.PropertyPath)));
            }

            return new(errors.Count == 0 ? record : null, false, errors);
        }
    }
}
