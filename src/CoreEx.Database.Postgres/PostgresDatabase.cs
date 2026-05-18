namespace CoreEx.Database.Postgres;

/// <summary>
/// Provides the <see href="https://www.npgsql.org/">Npgsql (PostgreSQL)</see> <see cref="IDatabase"/> functionality.
/// </summary>
/// <remarks>The <see cref="OnDbException(DbException)"/> override implements transformation of pre-determined PostgreSQL error codes, as follows:
///  <list type="bullet">
///   <item><c>56001</c> -> <see cref="ValidationException"/>.</item>
///   <item><c>56002</c> -> <see cref="BusinessException"/>.</item>
///   <item><c>56003</c> -> <see cref="AuthorizationException"/>.</item>
///   <item><c>56004</c> -> <see cref="ConcurrencyException"/>.</item>
///   <item><c>56005</c> -> <see cref="NotFoundException"/>.</item>
///   <item><c>56006</c> -> <see cref="ConflictException"/>.</item>
///   <item><c>56007</c> -> <see cref="DuplicateException"/>.</item>
///   <item><c>56010</c> -> <see cref="DataConsistencyException"/>.</item>
///  </list>
///  <para>This is in addition to the <see cref="CheckDuplicateErrorNumbers"/> with the corresponding <see cref="DuplicateErrorNumbers"/> that will also result in a <see cref="DuplicateException"/>.</para>
///  <para>This class also implements the <see cref="IUnitOfWork"/> including a <see href="https://microservices.io/patterns/data/transactional-outbox.html">transactional outbox</see>. The <see cref="IUnitOfWork"/> 
///  functionality is enabled by the <see cref="PostgresUnitOfWorkInvoker"/>; note, this is not thread-safe.</para>
/// </remarks>
/// <param name="dataSource">The <see cref="NpgsqlDataSource"/>.</param>
/// <param name="jsonSerializerOptions">The optional <see cref="JsonSerializerOptions"/>.</param>
/// <param name="logger">The optional <see cref="ILogger"/>.</param>
public partial class PostgresDatabase(NpgsqlDataSource dataSource, JsonSerializerOptions? jsonSerializerOptions = null, ILogger<PostgresDatabase>? logger = null) : Database<NpgsqlConnection, PostgresCommand, PostgresDatabaseArgs, PostgresDatabaseColumns>(dataSource.CreateConnection(), PostgresInvoker.Default, PostgresDatabaseColumns.Default, jsonSerializerOptions, logger)
{
    /// <summary>
    /// Gets the default <see cref="DuplicateErrorNumbers"/>.
    /// </summary>
    /// <remarks>See <see href="https://www.postgresql.org/docs/current/errcodes-appendix.html"/>.</remarks>
    public static string[] DefaultDuplicateErrorNumbers { get; } = ["23505"];

    /// <inheritdoc/>
    public override ISourceConverter<string?> RowVersionConverter => EncodedStringToUInt32Converter.Default;

    /// <summary>
    /// Indicates whether to check the <see cref="DuplicateErrorNumbers"/> when catching the <see cref="PostgresException"/>.
    /// </summary>
    public bool CheckDuplicateErrorNumbers { get; set; } = true;

    /// <summary>
    /// Gets or sets the list of known <see cref="PostgresException.SqlState"/> values that are considered a duplicate error.
    /// </summary>
    /// <remarks>Overrides the <see cref="DefaultDuplicateErrorNumbers"/>.</remarks>
    public string[]? DuplicateErrorNumbers { get; set; }

    /// <inheritdoc/>
    public override PostgresCommand Statement(SqlStatement statement) => new(this, statement);

    /// <inheritdoc/>
    public override DbParameter CreateParameter() => new NpgsqlParameter();

    /// <inheritdoc/>
    protected override Exception? OnDbException(DbException dbex)
    {
        if (dbex is PostgresException pex)
        {
            var msg = pex.Message?.TrimEnd();
            if (string.IsNullOrEmpty(msg))
                msg = null;

            switch (pex.SqlState)
            {
                case "56001": return new ValidationException(msg, pex);
                case "56002": return new BusinessException(msg, pex);
                case "56003": return new AuthorizationException(msg, pex);
                case "56004": return new ConcurrencyException(msg, pex);
                case "56005": return new NotFoundException(msg, pex);
                case "56006": return new ConflictException(msg, pex);
                case "56007": return new DuplicateException(msg, pex);
                case "56010": return new DataConsistencyException(msg, pex);

                default:
                    if (CheckDuplicateErrorNumbers && (DuplicateErrorNumbers ?? DefaultDuplicateErrorNumbers).Contains(pex.SqlState))
                        return new DuplicateException(null, pex);

                    break;
            }
        }

        return base.OnDbException(dbex);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            ((IDatabase)this).Connection?.Dispose(); // We created it, so we dispose of it!

        base.Dispose(disposing);
    }
}