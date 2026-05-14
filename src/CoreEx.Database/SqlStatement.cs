namespace CoreEx.Database;

/// <summary>
/// Represents a database SQL statement, including its command type and  text, for use with data access operations.
/// </summary>
public readonly record struct SqlStatement
{
    /// <summary>
    /// Gets an indeterminate <see cref="SqlStatement"/>.
    /// </summary>
    public static SqlStatement Indeterminate { get; } = new SqlStatement();

    /// <summary>
    /// Creates a stored procedure <see cref="SqlStatement"/>.
    /// </summary>
    /// <param name="storedProcedure">The stored procedure name.</param>
    /// <returns>The <see cref="SqlStatement"/>.</returns>
    public static SqlStatement StoredProcedure(string storedProcedure) => new(CommandType.StoredProcedure, storedProcedure);

    /// <summary>
    /// Creates a SQL text <see cref="SqlStatement"/> from the <paramref name="text"/>.
    /// </summary>
    /// <param name="text">The SQL statement text.</param>
    /// <returns>The <see cref="SqlStatement"/>.</returns>
    public static SqlStatement FromText(string text) => new(CommandType.Text, text);

    /// <summary>
    /// Creates a SQL text <see cref="SqlStatement"/> from the named embedded resource within the specified <paramref name="assembly"/>.
    /// </summary>
    /// <param name="resourceName">The embedded resource name (matches to the end of the fully qualifed resource name).</param>
    /// <param name="assembly">The <see cref="Assembly"/> that contains the embedded resource; defaults to <see cref="Assembly.GetCallingAssembly"/>.</param>
    /// <returns>The <see cref="SqlStatement"/>.</returns>
    public static SqlStatement FromResource(string resourceName, Assembly? assembly = null)
    {
        using var s = Resource.GetStream(resourceName, assembly ?? Assembly.GetCallingAssembly());
        using var sr = new StreamReader(s);
        return FromText(sr.ReadToEnd());
    }

    /// <summary>
    /// Creates a SQL text <see cref="SqlStatement"/> from the named embedded resource within the <see name="Assembly"/> inferred from the <typeparamref name="TResource"/> <see cref="Type"/>.
    /// </summary>
    /// <typeparam name="TResource">The <see cref="Type"/> to infer the <see cref="Assembly"/> that contains the embedded resource.</typeparam>
    /// <param name="resourceName">The embedded resource name (matches to the end of the fully qualifed resource name).</param>
    /// <returns>The <see cref="SqlStatement"/>.</returns>
    public static SqlStatement FromResource<TResource>(string resourceName) => FromResource(resourceName, typeof(TResource).Assembly);

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlStatement"/> class.
    /// </summary>
    /// <param name="commandType">The <see cref="System.Data.CommandType"/>.</param>
    /// <param name="commandText">The command text.</param>
    public SqlStatement(CommandType commandType, string commandText)
    {
        CommandType = commandType;
        CommandText = commandText.ThrowIfNullOrEmpty();
    }

    /// <summary>
    /// Gets the <see cref="System.Data.CommandType"/>.
    /// </summary>
    public CommandType CommandType { get; }

    /// <summary>
    /// Gets the command text.
    /// </summary>
    public string CommandText { get => field ?? throw new InvalidOperationException($"{nameof(CommandText)} is not initialized; the {nameof(SqlStatement)} is indeterminate."); }

    /// <summary>
    /// Indicates whether the <see cref="SqlStatement"/> is indeterminate (i.e. has not been initialized with a command text).
    /// </summary>
    public bool IsIndeterminate => CommandText is null;

    /// <summary>
    /// An implicit cast from a text <see cref="string"/> to a <see cref="SqlStatement"/> (<see cref="System.Data.CommandType.Text"/>).
    /// </summary>
    /// <param name="text">The SQL statement text.</param>
    public static implicit operator SqlStatement(string text) => FromText(text);
}