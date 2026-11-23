namespace CoreEx.Database.Postgres.Test.Unit;

[TestFixture, NonParallelizable]
public class PostgresOperationStoreTests : OperationStoreTestsBase
{
    private const string Schema = "ops";
    private readonly List<PostgresDatabase> _databases = [];
    private readonly string _businessTable = $"operation_proof_{Guid.NewGuid():N}";
    private bool _businessTableCreated;
    private string _connectionString = null!;
    private NpgsqlDataSource _dataSource = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _connectionString = new ConfigurationBuilder().AddJsonFile("appsettings.unittest.json").Build().GetValue<string>("Aspire:Npgsql:ConnectionString")!;

        var csb = new NpgsqlConnectionStringBuilder(_connectionString);
        var dbName = csb.Database;
        csb.Database = "postgres";
        await using (var admin = new NpgsqlConnection(csb.ConnectionString))
        {
            await admin.OpenAsync();
            await using var check = new NpgsqlCommand($"SELECT 1 FROM pg_database WHERE datname = '{dbName}'", admin);
            if (await check.ExecuteScalarAsync() is null)
                await ExecuteAsync(csb.ConnectionString, $"CREATE DATABASE \"{dbName}\"");
        }

        _dataSource = NpgsqlDataSource.Create(_connectionString);

        await ExecuteAsync(_connectionString, $"DROP TABLE IF EXISTS \"{Schema}\".\"{PostgresOperationStore.DefaultTableName}\"");
        var script = await OperationScriptTestHelper.ExportAsync(typeof(CoreEx.Database.Postgres.Test.Console.Program), OperationScriptTestHelper.PostgresOfflineConnection, Schema, PostgresOperationStore.DefaultTableName);
        await ExecuteAsync(_connectionString, script);
        await ExecuteAsync(_connectionString, $"CREATE TABLE \"{Schema}\".\"{_businessTable}\" (id uuid PRIMARY KEY)");
        _businessTableCreated = true;
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        try
        {
            if (_businessTableCreated)
                await ExecuteAsync(_connectionString, $"DROP TABLE \"{Schema}\".\"{_businessTable}\"");
        }
        finally
        {
            if (_dataSource is not null)
                await _dataSource.DisposeAsync();
        }
    }

    [TearDown]
    public void DisposeDatabases()
    {
        _databases.ForEach(x => x.Dispose());
        _databases.Clear();
    }

    protected override OperationStoreBase CreateStore(OperationStoreOptions options) => CreateStore(options, CreateDatabase());

    protected override OperationStoreBase CreateConcurrentStore(OperationStoreBase store) => CreateStore(SuiteOptions(), CreateDatabase());

    protected override async Task AdvanceAsync(TimeSpan duration)
    {
        // Eligibility is decided by the database clock, which may drift from the host (e.g. a container VM); wait on the database clock itself.
        await using var cmd = _dataSource.CreateCommand("SELECT statement_timestamp()");
        var target = (DateTime)(await cmd.ExecuteScalarAsync())! + duration;
        await Task.Delay(duration);
        while ((DateTime)(await cmd.ExecuteScalarAsync())! < target)
            await Task.Delay(50);
    }

    private PostgresOperationStore CreateStore(OperationStoreOptions options, PostgresDatabase database) => new(database, _dataSource, CreateLogger<PostgresOperationStore>(), Settings, options, Schema);

    private PostgresDatabase CreateDatabase()
    {
        var db = new PostgresDatabase(_dataSource);
        lock (_databases)
            _databases.Add(db);

        return db;
    }

    private static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    [Test]
    public void Registration_Passes_Explicit_Schema_Before_Configuring_The_Store()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Settings);
        services.AddSingleton(_dataSource);
        services.AddScoped(_ => new PostgresDatabase(_dataSource));
        services.AddPostgresOperationStore((_, store) => store.TableName = "operation_alt", schema: Schema);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<PostgresOperationStore>();
        store.Schema.Should().Be(Schema);
        store.TableName.Should().Be("operation_alt");
        store.Should().BeSameAs(scope.ServiceProvider.GetRequiredService<IOperationStore>());
        store.Database.Should().BeSameAs(scope.ServiceProvider.GetRequiredService<PostgresDatabase>());
    }

    [Test]
    public async Task Migration_Script_Is_Idempotent_For_Schema_And_Creates_Distinct_Tables()
    {
        await ExecuteAsync(_connectionString, $"DROP TABLE IF EXISTS \"{Schema}\".\"operation_alt\"");
        var script = await OperationScriptTestHelper.ExportAsync(typeof(CoreEx.Database.Postgres.Test.Console.Program), OperationScriptTestHelper.PostgresOfflineConnection, Schema, "operation_alt");
        await ExecuteAsync(_connectionString, script);

        var store = CreateStore(SuiteOptions(), CreateDatabase());
        store.TableName = "operation_alt";
        var created = await store.CreateAsync(OperationName, NewKey());

        created.Created.Should().BeTrue();
        (await CreateStore(SuiteOptions()).GetByIdAsync(created.State.OperationId)).Should().BeNull();

        await ExecuteAsync(_connectionString, $"DROP TABLE \"{Schema}\".\"operation_alt\"");
    }

    [Test]
    public async Task Create_Joins_Ambient_Transaction_And_Rolls_Back()
    {
        var db = CreateDatabase();
        var store = CreateStore(SuiteOptions(), db);
        var key = NewKey();

        var txn = await (await db.GetConnectionAsync()).BeginTransactionAsync();
        db.UseTransaction(txn);
        (await store.CreateAsync(OperationName, key)).Created.Should().BeTrue();
        await txn.RollbackAsync();
        db.UseTransaction(null);

        (await CreateStore(SuiteOptions()).GetByKeyAsync(OperationName, key)).Should().BeNull();
    }

    [Test]
    public async Task Create_Uses_Database_Time_After_Waiting_For_A_Concurrent_Rollback()
    {
        var options = AbandonOptions();
        var key = NewKey();
        var blockerDb = CreateDatabase();
        var blockerConnection = (NpgsqlConnection)await blockerDb.GetConnectionAsync();
        await using var transaction = await blockerConnection.BeginTransactionAsync();
        blockerDb.UseTransaction(transaction);
        (await CreateStore(options, blockerDb).CreateAsync(OperationName, key)).Created.Should().BeTrue();

        var applicationName = $"coreex-create-{Guid.NewGuid():N}";
        var csb = new NpgsqlConnectionStringBuilder(_connectionString) { ApplicationName = applicationName };
        await using var dataSource = NpgsqlDataSource.Create(csb.ConnectionString);
        using var contenderDb = new PostgresDatabase(dataSource);
        var contender = new PostgresOperationStore(contenderDb, dataSource, CreateLogger<PostgresOperationStore>(), Settings, options, Schema);
        var pending = contender.CreateAsync(OperationName, key);
        DateTimeOffset releasedAt;
        try
        {
            await WaitForBlockedRequestsAsync(applicationName, 1);
            await AdvanceAsync(PastInactivity);
            await using var clock = new NpgsqlCommand("SELECT clock_timestamp()", blockerConnection, transaction);
            releasedAt = new DateTimeOffset((DateTime)(await clock.ExecuteScalarAsync())!);
        }
        finally
        {
            await transaction.RollbackAsync();
            blockerDb.UseTransaction(null);
        }

        var created = await pending;
        created.Created.Should().BeTrue();
        created.State.CreatedOn.Should().BeOnOrAfter(releasedAt);
        created.State.AbandonAfter.Should().Be(created.State.CreatedOn + options.InactivityTimeout);
    }

    [Test]
    public async Task Create_Rolls_Back_When_Timestamp_Finalization_Fails()
    {
        var tableName = $"atomic_{Guid.NewGuid():N}";
        var script = await OperationScriptTestHelper.ExportAsync(typeof(CoreEx.Database.Postgres.Test.Console.Program), OperationScriptTestHelper.PostgresOfflineConnection, Schema, tableName);
        await ExecuteAsync(_connectionString, script);
        try
        {
            await ExecuteAsync(_connectionString, $"ALTER TABLE \"{Schema}\".\"{tableName}\" ADD COLUMN initial_created_on timestamptz NOT NULL DEFAULT statement_timestamp(), ADD CONSTRAINT create_time_guard CHECK (created_on = initial_created_on)");
            var key = NewKey();
            var hash = KeyHash.Compute(Settings.DomainName, TenantId, OperationName, key.ToCanonicalString());
            await using var blocker = new NpgsqlConnection(_connectionString);
            await blocker.OpenAsync();
            await using var transaction = await blocker.BeginTransactionAsync();
            await using (var command = new NpgsqlCommand($"INSERT INTO \"{Schema}\".\"{tableName}\" (key_hash, operation_id, domain_name, tenant_id, operation_name, instance_id, status, created_on, updated_on) VALUES (@hash, @id, @domain, @tenant, @name, @instance, 1, statement_timestamp(), statement_timestamp())", blocker, transaction))
            {
                command.Parameters.AddWithValue("@hash", hash);
                command.Parameters.AddWithValue("@id", Guid.NewGuid());
                command.Parameters.AddWithValue("@domain", Settings.DomainName);
                command.Parameters.AddWithValue("@tenant", TenantId);
                command.Parameters.AddWithValue("@name", OperationName);
                command.Parameters.AddWithValue("@instance", key.ToCanonicalString());
                await command.ExecuteNonQueryAsync();
            }

            var applicationName = $"coreex-create-fail-{Guid.NewGuid():N}";
            var csb = new NpgsqlConnectionStringBuilder(_connectionString) { ApplicationName = applicationName };
            await using var dataSource = NpgsqlDataSource.Create(csb.ConnectionString);
            using var db = new PostgresDatabase(dataSource);
            var store = new PostgresOperationStore(db, dataSource, CreateLogger<PostgresOperationStore>(), Settings, AbandonOptions(), Schema) { TableName = tableName };
            var pending = store.CreateAsync(OperationName, key);
            try
            {
                await WaitForBlockedRequestsAsync(applicationName, 1);
                await Task.Delay(100);
            }
            finally
            {
                await transaction.RollbackAsync();
            }

            var failure = await FluentActions.Awaiting(() => pending).Should().ThrowAsync<PostgresException>();
            failure.Which.SqlState.Should().Be("23514");
            (await store.GetByKeyAsync(OperationName, key)).Should().BeNull();
        }
        finally
        {
            await ExecuteAsync(_connectionString, $"DROP TABLE \"{Schema}\".\"{tableName}\"");
        }
    }

    [Test]
    public void QuoteIdentifier_Escapes_Delimiters() => CreateDatabase().QuoteIdentifier("a\"b c").Should().Be("\"a\"\"b c\"");

    [Test]
    public async Task Complete_Joins_Ambient_Transaction_And_Rolls_Back()
    {
        var db = CreateDatabase();
        var store = CreateStore(SuiteOptions(), db);
        var claim = await CreateAndClaimAsync(store);

        var txn = await (await db.GetConnectionAsync()).BeginTransactionAsync();
        db.UseTransaction(txn);
        (await store.CompleteAsync(claim, OperationCompletion.Succeeded()))!.Status.Should().Be(OperationStatus.Succeeded);
        await txn.RollbackAsync();
        db.UseTransaction(null);

        (await CreateStore(SuiteOptions()).GetByIdAsync(claim.OperationId))!.Status.Should().Be(OperationStatus.Running);
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task UnitOfWork_Commits_Or_Rolls_Back_Business_And_Operation_Together(bool complete, bool commit)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Settings);
        services.AddSingleton(SuiteOptions());
        services.AddSingleton(_dataSource);
        services.AddScoped(_ => new PostgresDatabase(_dataSource));
        services.AddPostgresUnitOfWork();
        services.AddPostgresOperationStore(schema: Schema);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PostgresDatabase>();
        var store = scope.ServiceProvider.GetRequiredService<IOperationStore>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var key = NewKey();
        var businessId = Guid.NewGuid();
        OperationClaim? claim = null;
        if (complete)
        {
            var created = await store.CreateAsync(OperationName, key);
            var result = await store.ClaimAsync(created.State.OperationId, "worker", TimeSpan.FromMinutes(1));
            result.Outcome.Should().Be(OperationClaimOutcome.Claimed);
            claim = result.Claim ?? throw new InvalidOperationException("The claimed operation was not returned.");
        }

        Func<Task> work = () => unitOfWork.TransactionAsync(async ct =>
        {
            var rows = await db.Statement($"INSERT INTO \"{Schema}\".\"{_businessTable}\" (id) VALUES (@Id)").Param("Id", businessId).NonQueryAsync(ct);
            rows.Should().Be(1);
            if (claim is null)
                (await store.CreateAsync(OperationName, key, cancellationToken: ct)).Created.Should().BeTrue();
            else
            {
                var completed = await store.CompleteAsync(claim, OperationCompletion.Succeeded(), ct) ?? throw new InvalidOperationException("The completion fence was lost.");
                completed.Status.Should().Be(OperationStatus.Succeeded);
            }

            if (!commit)
                throw new InvalidOperationException("Rollback proof.");
        });

        if (commit)
            await work();
        else
            await work.Should().ThrowAsync<InvalidOperationException>().WithMessage("Rollback proof.");

        var observerDb = CreateDatabase();
        var count = await observerDb.Statement($"SELECT COUNT(*)::int FROM \"{Schema}\".\"{_businessTable}\" WHERE id = @Id").Param("Id", businessId).ScalarAsync<int>();
        count.Should().Be(commit ? 1 : 0);
        var state = await CreateStore(SuiteOptions(), observerDb).GetByKeyAsync(OperationName, key);
        if (!complete && !commit)
            state.Should().BeNull();
        else
        {
            var actual = state ?? throw new InvalidOperationException("The operation was not found.");
            actual.Status.Should().Be(complete ? (commit ? OperationStatus.Succeeded : OperationStatus.Running) : OperationStatus.Accepted);
        }
    }

    [Test]
    public async Task Claim_Uses_Independent_Scope_Outside_Ambient_Transaction()
    {
        var created = await CreateStore(SuiteOptions()).CreateAsync(OperationName, NewKey());
        var db = CreateDatabase();
        var store = CreateStore(SuiteOptions(), db);

        var txn = await (await db.GetConnectionAsync()).BeginTransactionAsync();
        db.UseTransaction(txn);
        (await store.ClaimAsync(created.State.OperationId, "worker-a", ClaimDuration)).Outcome.Should().Be(OperationClaimOutcome.Claimed);
        await txn.RollbackAsync();
        db.UseTransaction(null);

        var state = (await CreateStore(SuiteOptions()).GetByIdAsync(created.State.OperationId))!;
        state.Status.Should().Be(OperationStatus.Running);
        state.Attempt.Should().Be(1);
    }

    [Test]
    public async Task Fencing_Uses_Database_Time_After_Waiting_For_A_Row_Lock()
    {
        var arrange = CreateStore(SuiteOptions());
        var shortLived = await arrange.CreateAsync(OperationName, NewKey());
        var shortClaim = (await arrange.ClaimAsync(shortLived.State.OperationId, "worker-a", TimeSpan.FromMilliseconds(1))).Claim!;
        await AdvanceAsync(TimeSpan.FromMilliseconds(50));
        var claims = new[]
        {
            await CreateAndClaimAsync(arrange),
            await CreateAndClaimAsync(arrange),
            await CreateAndClaimAsync(arrange),
            shortClaim
        };

        await using var blocker = new NpgsqlConnection(_connectionString);
        await blocker.OpenAsync();
        await using var transaction = await blocker.BeginTransactionAsync();
        await using (var command = new NpgsqlCommand($"SELECT operation_id FROM \"{Schema}\".\"{PostgresOperationStore.DefaultTableName}\" WHERE operation_id = ANY(@ids) FOR UPDATE", blocker, transaction))
        {
            command.Parameters.AddWithValue("@ids", claims.Select(x => x.OperationId).ToArray());
            await using var reader = await command.ExecuteReaderAsync();
            var count = 0;
            while (await reader.ReadAsync())
                count++;

            count.Should().Be(claims.Length);
        }

        var applicationName = $"coreex-fencing-{Guid.NewGuid():N}";
        var csb = new NpgsqlConnectionStringBuilder(_connectionString) { ApplicationName = applicationName };
        await using var dataSource = NpgsqlDataSource.Create(csb.ConnectionString);
        using var db = new PostgresDatabase(dataSource);
        var store = new PostgresOperationStore(db, dataSource, CreateLogger<PostgresOperationStore>(), Settings, SuiteOptions(), Schema);
        var completion = store.CompleteAsync(claims[0], OperationCompletion.Succeeded());
        var renewal = store.RenewAsync(claims[1], ClaimDuration);
        var release = store.ReleaseAsync(claims[2]);
        var takeover = store.ClaimAsync(claims[3].OperationId, "worker-b", ClaimDuration);

        try
        {
            await WaitForBlockedRequestsAsync(applicationName, claims.Length);
            await using var clock = new NpgsqlCommand("SELECT clock_timestamp()", blocker, transaction);
            var now = (DateTime)(await clock.ExecuteScalarAsync())!;
            claims.Take(3).Min(x => x.ExpiresOn).Should().BeAfter(new DateTimeOffset(now));
            await AdvanceAsync(PastClaim);
        }
        finally
        {
            await transaction.RollbackAsync();
        }

        var completionResult = await completion;
        var renewalResult = await renewal;
        var releaseResult = await release;
        var takeoverResult = await takeover;
        completionResult.Should().BeNull();
        renewalResult.Should().BeNull();
        releaseResult.Should().BeFalse();
        takeoverResult.Outcome.Should().Be(OperationClaimOutcome.Claimed);
        takeoverResult.Claim!.Attempt.Should().Be(claims[3].Attempt + 1);
        await using var clockAfter = new NpgsqlCommand("SELECT clock_timestamp()", blocker);
        var after = (DateTime)(await clockAfter.ExecuteScalarAsync())!;
        takeoverResult.Claim.ExpiresOn.Should().BeAfter(new DateTimeOffset(after));
    }

    [Test]
    public async Task Cancel_Returns_Current_State_When_Completion_Commits_While_It_Waits()
    {
        var db = CreateDatabase();
        var store = CreateStore(SuiteOptions(), db);
        var claim = await CreateAndClaimAsync(store);
        await using var transaction = await (await db.GetConnectionAsync()).BeginTransactionAsync();
        db.UseTransaction(transaction);
        (await store.CompleteAsync(claim, OperationCompletion.Succeeded()))!.Status.Should().Be(OperationStatus.Succeeded);

        var applicationName = $"coreex-cancel-{Guid.NewGuid():N}";
        var csb = new NpgsqlConnectionStringBuilder(_connectionString) { ApplicationName = applicationName };
        await using var dataSource = NpgsqlDataSource.Create(csb.ConnectionString);
        using var cancelDb = new PostgresDatabase(dataSource);
        var cancelStore = new PostgresOperationStore(cancelDb, dataSource, CreateLogger<PostgresOperationStore>(), Settings, SuiteOptions(), Schema);
        var cancellation = cancelStore.RequestCancelAsync(claim.OperationId);

        var committed = false;
        try
        {
            await WaitForBlockedRequestsAsync(applicationName, 1);
            await transaction.CommitAsync();
            committed = true;
        }
        finally
        {
            if (!committed)
                await transaction.RollbackAsync();

            db.UseTransaction(null);
        }

        var result = await cancellation;
        result!.Status.Should().Be(OperationStatus.Succeeded);
        result.CancelRequestedOn.Should().BeNull();
    }

    private async Task WaitForBlockedRequestsAsync(string applicationName, int count)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT COUNT(*)::int FROM pg_stat_activity WHERE application_name = @name AND wait_event_type = 'Lock' AND cardinality(pg_blocking_pids(pid)) > 0", connection);
        command.Parameters.AddWithValue("@name", applicationName);
        for (var i = 0; i < 200; i++)
        {
            if ((int)(await command.ExecuteScalarAsync())! == count)
                return;

            await Task.Delay(25);
        }

        throw new TimeoutException($"Expected {count} blocked PostgreSQL requests.");
    }

    private abstract class KeyHash() : OperationStoreBase(null!, null!)
    {
        public static byte[] Compute(string domainName, string? tenantId, string operationName, string instanceId) => ComputeKeyHash(domainName, tenantId, operationName, instanceId);
    }
}
