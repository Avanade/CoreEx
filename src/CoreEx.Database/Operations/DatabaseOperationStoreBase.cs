namespace CoreEx.Database.Operations;

/// <summary>
/// Provides the provider-neutral relational <see cref="IOperationStore"/> implementation using a single purpose-built operations table per domain database.
/// </summary>
/// <typeparam name="TDatabase">The <see cref="IDatabase"/> <see cref="Type"/>.</typeparam>
/// <remarks>Runtime SQL is loaded from parameterized embedded resources (no stored routines or SQL transaction control). The identity is located by its <see cref="OperationStoreBase.ComputeKeyHash"/> and then verified ordinally, so comparison is exact regardless of database collation. All timestamps are observed by the database.
/// <para>Mutations join an existing <see cref="IDatabase.CurrentTransaction"/> or create and own an explicit <see cref="DbTransaction"/>. Create and complete use <see cref="Database"/>, allowing acceptance and completion to commit atomically with business writes and a same-database outbox. Claim, renew, release, cancellation and cleanup always execute in a short independent scope (see <see cref="CreateOperationDatabase"/>) so that ownership is never held by, or lost with, a business transaction; do not claim an operation whose creating transaction has not yet committed.</para>
/// <para>Durations are applied with millisecond precision; sub-millisecond durations are rounded up.</para></remarks>
/// <param name="database">The ambient <typeparamref name="TDatabase"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
/// <param name="settings">The <see cref="IHostSettings"/>.</param>
/// <param name="options">The optional <see cref="OperationStoreOptions"/>.</param>
/// <param name="schema">The default schema name.</param>
/// <param name="tableName">The default table name.</param>
public abstract partial class DatabaseOperationStoreBase<TDatabase>(TDatabase database, ILogger logger, IHostSettings settings, OperationStoreOptions? options, string schema, string tableName) : OperationStoreBase(logger, settings, options) where TDatabase : class, IDatabase
{
    /// <summary>
    /// Gets the expected number of state columns, in order, returned by every state-returning statement.
    /// </summary>
    public const int StateColumnCount = 23;

    private static readonly ConcurrentDictionary<(Type, string), string> _resourceCache = new();
    private readonly ConcurrentDictionary<string, SqlStatement> _statements = new();
    private string _schema = schema.ThrowIfNullOrEmpty();
    private string _tableName = tableName.ThrowIfNullOrEmpty();

    /// <summary>
    /// Gets the ambient <typeparamref name="TDatabase"/>.
    /// </summary>
    public TDatabase Database { get; } = database.ThrowIfNull();

    /// <summary>
    /// Gets or sets the schema name.
    /// </summary>
    /// <remarks>The name is delimited using <see cref="IDatabase.QuoteIdentifier(string)"/>, which escapes any embedded delimiter.</remarks>
    public string Schema
    {
        get => _schema;
        set
        {
            _schema = value.ThrowIfNullOrEmpty();
            _statements.Clear();
        }
    }

    /// <summary>
    /// Gets or sets the table name.
    /// </summary>
    /// <remarks>The name is delimited using <see cref="IDatabase.QuoteIdentifier(string)"/>, which escapes any embedded delimiter.</remarks>
    public string TableName
    {
        get => _tableName;
        set
        {
            _tableName = value.ThrowIfNullOrEmpty();
            _statements.Clear();
        }
    }

    /// <summary>
    /// Gets or sets the optional factory that creates the independent <typeparamref name="TDatabase"/> used for claim, renew, release, cancellation and cleanup.
    /// </summary>
    /// <remarks>Where <see langword="null"/>, <see cref="CreateOperationDatabase"/> is used. Each created database is released via <see cref="ReleaseOperationDatabase"/> after one operation; it must use its own connection and never the ambient connection or transaction.</remarks>
    public Func<TDatabase>? OperationDatabaseFactory { get; set; }

    /// <summary>
    /// Gets the unquoted column names, in the required <see cref="StateColumnCount"/> state order.
    /// </summary>
    /// <remarks>The order is: operation identifier, domain name, tenant identifier, operation name, instance identifier, status, initiator identifier, attempt, claim owner, claim expiry, cancellation requested, progress, error code, error message, result location, data, created, updated, started, completed, abandon after, purge after and ETag.</remarks>
    protected abstract IReadOnlyList<string> StateColumnNames { get; }

    /// <summary>
    /// Loads the raw text for the named runtime statement using <see cref="SqlStatement.FromResource{TResource}(string)"/>.
    /// </summary>
    /// <param name="name">The statement name; for example <c>Create</c>.</param>
    /// <returns>The raw statement text including the <c>{{Table}}</c> and <c>{{Columns}}</c> placeholders.</returns>
    /// <remarks>The result is cached per store <see cref="Type"/> and <paramref name="name"/>.</remarks>
    protected abstract string LoadStatementText(string name);

    /// <summary>
    /// Creates the independent <typeparamref name="TDatabase"/>, with its own connection, used for claim, renew, release, cancellation and cleanup.
    /// </summary>
    /// <returns>The independent <typeparamref name="TDatabase"/>.</returns>
    protected abstract TDatabase CreateOperationDatabase();

    /// <summary>
    /// Releases an independent <typeparamref name="TDatabase"/> after use.
    /// </summary>
    /// <param name="database">The independent <typeparamref name="TDatabase"/>.</param>
    /// <remarks>Disposes the <paramref name="database"/> where <see cref="IDisposable"/>; override to also release a connection that the database does not own.</remarks>
    protected virtual void ReleaseOperationDatabase(TDatabase database) => (database as IDisposable)?.Dispose();

    /// <inheritdoc/>
    protected override async Task<OperationState?> OnGetByIdAsync(Guid operationId, CancellationToken cancellationToken)
    {
        var rows = await SelectAsync(Database, "GetById", p => p.AddParameter("OperationId", operationId, DbType.Guid), false, cancellationToken).ConfigureAwait(false);
        return rows.Count == 0 ? null : rows[0].State;
    }

    /// <inheritdoc/>
    protected override async Task<OperationState?> OnGetByKeyAsync(string domainName, string? tenantId, string operationName, string instanceId, CancellationToken cancellationToken)
    {
        var hash = ComputeKeyHash(domainName, tenantId, operationName, instanceId);
        var rows = await SelectAsync(Database, "GetByKey", p => p.AddParameter("KeyHash", hash, DbType.Binary), false, cancellationToken).ConfigureAwait(false);
        return rows.Count == 0 ? null : Verify(rows[0].State, domainName, tenantId, operationName, instanceId);
    }

    /// <inheritdoc/>
    protected override Task<OperationCreateResult> OnCreateAsync(string domainName, string? tenantId, string operationName, string instanceId, string? initiatorId, string? data, CancellationToken cancellationToken)
        => ExecuteInTransactionAsync(Database, async () =>
        {
            var hash = ComputeKeyHash(domainName, tenantId, operationName, instanceId);
            var rows = await SelectAsync(Database, "Create", p =>
            {
                p.AddParameter("KeyHash", hash, DbType.Binary);
                // Each incarnation requires a fresh identifier independent of application identifier generators.
                p.AddParameter("OperationId", Guid.NewGuid(), DbType.Guid);
                p.AddParameter("DomainName", domainName, DbType.String);
                p.AddParameter("TenantId", tenantId, DbType.String);
                p.AddParameter("OperationName", operationName, DbType.String);
                p.AddParameter("InstanceId", instanceId, DbType.String);
                p.AddParameter("InitiatorId", initiatorId, DbType.String);
                p.AddParameter("Data", data, DbType.String);
                p.AddParameter("InactivityMs", ToMilliseconds(Options.InactivityTimeout), DbType.Int64);
            }, true, cancellationToken).ConfigureAwait(false);

            if (rows.Count > 0)
                return new OperationCreateResult(rows[0].Extra != 0, Verify(rows[0].State, domainName, tenantId, operationName, instanceId));

            // A concurrent change can leave no row visible when the create result is read.
            var existing = await OnGetByKeyAsync(domainName, tenantId, operationName, instanceId, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The operation could neither be created nor found after a concurrent change.");

            return new OperationCreateResult(false, existing);
        }, cancellationToken);

    /// <inheritdoc/>
    protected override Task<OperationClaimResult> OnClaimAsync(Guid operationId, string owner, TimeSpan duration, CancellationToken cancellationToken) => WithOperationDatabaseAsync(db => ExecuteInTransactionAsync(db, async () =>
    {
        var rows = await SelectAsync(db, "Claim", p =>
        {
            p.AddParameter("OperationId", operationId, DbType.Guid);
            p.AddParameter("Owner", owner, DbType.String);
            p.AddParameter("DurationMs", ToMilliseconds(duration), DbType.Int64);
            p.AddParameter("InactivityMs", ToMilliseconds(Options.InactivityTimeout), DbType.Int64);
        }, true, cancellationToken).ConfigureAwait(false);

        if (rows.Count == 0)
            return new OperationClaimResult(OperationClaimOutcome.NotFound, null, null);

        var (state, outcome) = rows[0];
        return (OperationClaimOutcome)outcome switch
        {
            OperationClaimOutcome.Claimed => new OperationClaimResult(OperationClaimOutcome.Claimed, state, ToClaim(state)),
            OperationClaimOutcome.Finished => new OperationClaimResult(OperationClaimOutcome.Finished, state, null),
            OperationClaimOutcome.Busy => new OperationClaimResult(OperationClaimOutcome.Busy, state, null),
            _ => throw new InvalidOperationException($"The database returned an unexpected operation claim outcome: {outcome}.")
        };
    }, cancellationToken));

    /// <inheritdoc/>
    protected override Task<OperationClaim?> OnRenewAsync(OperationClaim claim, TimeSpan duration, string? progress, CancellationToken cancellationToken) => WithOperationDatabaseAsync(db => ExecuteInTransactionAsync(db, async () =>
    {
        var rows = await SelectAsync(db, "Renew", p =>
        {
            AddHeldParameters(p, claim);
            p.AddParameter("DurationMs", ToMilliseconds(duration), DbType.Int64);
            p.AddParameter("InactivityMs", ToMilliseconds(Options.InactivityTimeout), DbType.Int64);
            p.AddParameter("Progress", progress, DbType.String);
        }, false, cancellationToken).ConfigureAwait(false);

        return rows.Count == 0 ? null : ToClaim(rows[0].State);
    }, cancellationToken));

    /// <inheritdoc/>
    protected override Task<bool> OnReleaseAsync(OperationClaim claim, string? errorCode, string? errorMessage, CancellationToken cancellationToken) => WithOperationDatabaseAsync(db => ExecuteInTransactionAsync(db, async () =>
    {
        var count = await Command(db, "Release", p =>
        {
            AddHeldParameters(p, claim);
            p.AddParameter("ErrorCode", errorCode, DbType.String);
            p.AddParameter("ErrorMessage", errorMessage, DbType.String);
            p.AddParameter("InactivityMs", ToMilliseconds(Options.InactivityTimeout), DbType.Int64);
        }).ScalarAsync<int>(cancellationToken).ConfigureAwait(false);

        return count > 0;
    }, cancellationToken));

    /// <inheritdoc/>
    protected override Task<OperationState?> OnCompleteAsync(OperationClaim claim, OperationCompletion completion, CancellationToken cancellationToken)
        => ExecuteInTransactionAsync(Database, async () =>
        {
            var rows = await SelectAsync(Database, "Complete", p =>
            {
                AddHeldParameters(p, claim);
                p.AddParameter("Status", (short)completion.Status, DbType.Int16);
                p.AddParameter("ResultLocation", completion.ResultLocation, DbType.String);
                p.AddParameter("ErrorCode", completion.ErrorCode, DbType.String);
                p.AddParameter("ErrorMessage", completion.ErrorMessage, DbType.String);
                p.AddParameter("RetentionMs", ToMilliseconds(Options.TerminalRetention), DbType.Int64);
            }, false, cancellationToken).ConfigureAwait(false);

            return rows.Count == 0 ? null : rows[0].State;
        }, cancellationToken);

    /// <inheritdoc/>
    protected override Task<OperationState?> OnRequestCancelAsync(Guid operationId, CancellationToken cancellationToken) => WithOperationDatabaseAsync(db => ExecuteInTransactionAsync(db, async () =>
    {
        var rows = await SelectAsync(db, "Cancel", p =>
        {
            p.AddParameter("OperationId", operationId, DbType.Guid);
            p.AddParameter("RetentionMs", ToMilliseconds(Options.TerminalRetention), DbType.Int64);
        }, false, cancellationToken).ConfigureAwait(false);

        return rows.Count == 0 ? null : rows[0].State;
    }, cancellationToken));

    /// <inheritdoc/>
    protected override async Task<OperationCleanupResult> OnCleanupAsync(int maxCount, CancellationToken cancellationToken)
    {
        var expired = await WithOperationDatabaseAsync(db => SelectAsync(db, "CleanupExpire", p =>
        {
            p.AddParameter("MaxCount", maxCount, DbType.Int32);
            p.AddParameter("RetentionMs", ToMilliseconds(Options.TerminalRetention), DbType.Int64);
        }, true, cancellationToken)).ConfigureAwait(false);

        foreach (var (state, previousStatus) in expired)
        {
            LogExpired(state, (OperationStatus)previousStatus);
        }

        var purged = await WithOperationDatabaseAsync(db => Command(db, "CleanupPurge", p => p.AddParameter("MaxCount", maxCount, DbType.Int32)).ScalarAsync<int>(cancellationToken)).ConfigureAwait(false);
        return new OperationCleanupResult(expired.Count, purged);
    }

    /// <summary>
    /// Gets the <see cref="SqlStatement"/> for the named runtime statement with the placeholders replaced.
    /// </summary>
    /// <param name="name">The statement name.</param>
    /// <returns>The <see cref="SqlStatement"/>.</returns>
    protected SqlStatement GetStatement(string name) => _statements.GetOrAdd(name, n =>
    {
        var text = _resourceCache.GetOrAdd((GetType(), n), k => LoadStatementText(k.Item2));
        var columns = StateColumnNames;
        if (columns.Count != StateColumnCount)
            throw new InvalidOperationException($"Exactly {StateColumnCount} state column names are required.");

        text = text.Replace("{{Table}}", $"{Database.QuoteIdentifier(_schema)}.{Database.QuoteIdentifier(_tableName)}", StringComparison.Ordinal);
        text = ColumnsRegex().Replace(text, m =>
        {
            var prefix = m.Groups[1].Success ? $"{m.Groups[1].Value}." : string.Empty;
            return string.Join(", ", columns.Select(c => prefix + Database.QuoteIdentifier(c)));
        });

        return SqlStatement.FromText(text);
    });

    /// <summary>
    /// Converts the <paramref name="duration"/> to whole milliseconds, rounding up.
    /// </summary>
    private static long ToMilliseconds(TimeSpan duration) => (long)Math.Ceiling(duration.TotalMilliseconds);

    /// <summary>
    /// Adds the parameters that identify a held claim.
    /// </summary>
    private static void AddHeldParameters(DatabaseParameterCollection p, OperationClaim claim)
    {
        p.AddParameter("OperationId", claim.OperationId, DbType.Guid);
        p.AddParameter("Owner", claim.Owner, DbType.String);
        p.AddParameter("Attempt", claim.Attempt, DbType.Int32);
    }

    /// <summary>
    /// Creates the <see cref="OperationClaim"/> from the held <paramref name="state"/>.
    /// </summary>
    private static OperationClaim ToClaim(OperationState state) => new(state.OperationId, state.ClaimOwner!, state.Attempt, state.ClaimExpiresOn!.Value, state.CancelRequestedOn is not null);

    /// <summary>
    /// Verifies that the located <paramref name="state"/> has exactly the requested identity, guarding against a hash collision.
    /// </summary>
    private static OperationState Verify(OperationState state, string domainName, string? tenantId, string operationName, string instanceId)
        => string.Equals(state.DomainName, domainName, StringComparison.Ordinal) && string.Equals(state.TenantId, tenantId, StringComparison.Ordinal)
            && string.Equals(state.OperationName, operationName, StringComparison.Ordinal) && string.Equals(state.InstanceId, instanceId, StringComparison.Ordinal)
            ? state : throw new InvalidOperationException("The operation key hash matched an operation with a different identity.");

    /// <summary>
    /// Executes the <paramref name="func"/> using an independent database.
    /// </summary>
    /// <remarks>The separate connection does not inherit <see cref="IDatabase.CurrentTransaction"/>, isolating these operations from the caller's explicit unit-of-work transaction.</remarks>
    private async Task<T> WithOperationDatabaseAsync<T>(Func<TDatabase, Task<T>> func)
    {
        var db = OperationDatabaseFactory is null ? CreateOperationDatabase() : OperationDatabaseFactory() ?? throw new InvalidOperationException("The operation database factory returned null.");
        try
        {
            return await func(db).ConfigureAwait(false);
        }
        finally
        {
            ReleaseOperationDatabase(db);
        }
    }

    /// <summary>
    /// Executes the <paramref name="func"/> in the existing <see cref="IDatabase.CurrentTransaction"/> or a new transaction.
    /// </summary>
    private static async Task<T> ExecuteInTransactionAsync<T>(IDatabase db, Func<Task<T>> func, CancellationToken cancellationToken)
    {
        if (db.CurrentTransaction is not null)
            return await func().ConfigureAwait(false);

        var connection = await db.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(db.DbArgs.IsolationLevel, cancellationToken).ConfigureAwait(false);

        try
        {
            db.UseTransaction(transaction);
            var result = await func().ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch (Exception exception)
        {
            try
            {
                await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception rollbackException)
            {
                throw new AggregateException("The operation transaction failed and could not be rolled back.", exception, rollbackException);
            }

            throw;
        }
        finally
        {
            db.UseTransaction(null);
        }
    }

    /// <summary>
    /// Creates the <see cref="DatabaseCommand"/> for the named statement and parameters.
    /// </summary>
    private DatabaseCommand Command(TDatabase db, string name, Action<DatabaseParameterCollection> parameters)
    {
        var cmd = db.Statement(GetStatement(name));
        parameters(cmd.Parameters);
        return cmd;
    }

    /// <summary>
    /// Selects the state rows, with the optional trailing integer column.
    /// </summary>
    private async Task<List<(OperationState State, int Extra)>> SelectAsync(TDatabase db, string name, Action<DatabaseParameterCollection> parameters, bool hasExtra, CancellationToken cancellationToken)
    {
        var rows = new List<(OperationState, int)>();
        await Command(db, name, parameters).SelectAsync(r =>
        {
            rows.Add((ReadState(r), hasExtra ? Convert.ToInt32(r.GetValue(StateColumnCount), CultureInfo.InvariantCulture) : 0));
            return true;
        }, cancellationToken).ConfigureAwait(false);

        return rows;
    }

    /// <summary>
    /// Reads the <see cref="OperationState"/> from the fixed column ordinals.
    /// </summary>
    /// <remarks>The ETag uses the provider <see cref="IDatabase.RowVersionConverter"/> (via <see cref="DatabaseRecord.GetRowVersion(int)"/>).</remarks>
    private static OperationState ReadState(DatabaseRecord r) => new()
    {
        OperationId = r.GetValue<Guid>(0),
        DomainName = r.GetValue<string>(1),
        TenantId = r.GetValue<string?>(2),
        OperationName = r.GetValue<string>(3),
        InstanceId = r.GetValue<string>(4),
        Status = (OperationStatus)Convert.ToInt32(r.GetValue(5), CultureInfo.InvariantCulture),
        InitiatorId = r.GetValue<string?>(6),
        Attempt = Convert.ToInt32(r.GetValue(7), CultureInfo.InvariantCulture),
        ClaimOwner = r.GetValue<string?>(8),
        ClaimExpiresOn = GetUtc(r, 9),
        CancelRequestedOn = GetUtc(r, 10),
        Progress = r.GetValue<string?>(11),
        ErrorCode = r.GetValue<string?>(12),
        ErrorMessage = r.GetValue<string?>(13),
        ResultLocation = r.GetValue<string?>(14),
        Data = r.GetValue<string?>(15),
        CreatedOn = GetUtc(r, 16) ?? throw new InvalidOperationException("The operation created timestamp must not be null."),
        UpdatedOn = GetUtc(r, 17) ?? throw new InvalidOperationException("The operation updated timestamp must not be null."),
        StartedOn = GetUtc(r, 18),
        CompletedOn = GetUtc(r, 19),
        AbandonAfter = GetUtc(r, 20),
        PurgeAfter = GetUtc(r, 21),
        ETag = r.GetRowVersion(22) ?? throw new InvalidOperationException("The operation ETag must not be null.")
    };

    /// <summary>
    /// Gets the nullable UTC instant at the <paramref name="ordinal"/>, independent of the configured <see cref="IDatabase.DateTimeTransform"/>.
    /// </summary>
    private static DateTimeOffset? GetUtc(DatabaseRecord r, int ordinal) => r.GetDateTime(ordinal, DateTimeTransform.DateTimeUtc) is DateTime dt ? new DateTimeOffset(dt) : null;

    /// <summary>
    /// Gets the columns placeholder <see cref="Regex"/>; an optional <c>:alias</c> qualifies each column.
    /// </summary>
    [GeneratedRegex(@"\{\{Columns(?::([A-Za-z_][A-Za-z0-9_]*))?\}\}")]
    private static partial Regex ColumnsRegex();
}
