namespace CoreEx.Database.SqlServer.Test.Unit;

[TestFixture, NonParallelizable]
public class SqlServerOperationStoreTests : OperationStoreTestsBase
{
    private string Schema { get; } = $"OpsTest_{Guid.NewGuid():N}";
    private readonly List<SqlServerDatabase> _databases = [];
    private readonly string _businessTable = $"OperationProof_{Guid.NewGuid():N}";
    private bool _businessTableCreated;
    private string _connectionString = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _connectionString = new ConfigurationBuilder().AddJsonFile("appsettings.unittest.json").Build().GetValue<string>("Aspire:Microsoft:Data:SqlClient:ConnectionString")!;

        var csb = new SqlConnectionStringBuilder(_connectionString);
        var dbName = csb.InitialCatalog;
        csb.InitialCatalog = "master";
        await ExecuteAsync(csb.ConnectionString, $"IF DB_ID(N'{dbName}') IS NULL CREATE DATABASE [{dbName}]");

        var script = await OperationScriptTestHelper.ExportAsync(typeof(CoreEx.Database.SqlServer.Test.Console.Program), OperationScriptTestHelper.SqlServerOfflineConnection, Schema, SqlServerOperationStore.DefaultTableName);
        await ExecuteAsync(_connectionString, script);
        await ExecuteAsync(_connectionString, $"CREATE TABLE [{Schema}].[{_businessTable}] ([Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY)");
        _businessTableCreated = true;
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (_businessTableCreated)
            await ExecuteAsync(_connectionString, $"DROP TABLE [{Schema}].[{_businessTable}]; DROP TABLE IF EXISTS [{Schema}].[OperationAlt]; DROP TABLE [{Schema}].[{SqlServerOperationStore.DefaultTableName}]; DROP SCHEMA [{Schema}];");
    }

    [TearDown]
    public void DisposeDatabases()
    {
        foreach (var db in _databases)
        {
            var connection = ((IDatabase)db).Connection;
            db.Dispose();
            connection.Dispose();
        }

        _databases.Clear();
    }

    protected override OperationStoreBase CreateStore(OperationStoreOptions options) => CreateStore(options, CreateDatabase());

    protected override OperationStoreBase CreateConcurrentStore(OperationStoreBase store) => CreateStore(SuiteOptions(), CreateDatabase());

    protected override async Task AdvanceAsync(TimeSpan duration)
    {
        // Eligibility is decided by the database clock, which may drift from the host (e.g. a container VM); wait on the database clock itself.
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand("SELECT SYSUTCDATETIME()", conn);
        var target = (DateTime)(await cmd.ExecuteScalarAsync())! + duration;
        await Task.Delay(duration);
        while ((DateTime)(await cmd.ExecuteScalarAsync())! < target)
            await Task.Delay(50);
    }

    private SqlServerOperationStore CreateStore(OperationStoreOptions options, SqlServerDatabase database) => new(database, CreateLogger<SqlServerOperationStore>(), Settings, options, Schema);

    private SqlServerDatabase CreateDatabase()
    {
        var db = new SqlServerDatabase(new SqlConnection(_connectionString));
        lock (_databases)
            _databases.Add(db);

        return db;
    }

    private static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    [Test]
    public void Registration_Passes_Explicit_Schema_Before_Configuring_The_Store()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Settings);
        services.AddScoped(_ => new SqlConnection(_connectionString));
        services.AddScoped(sp => new SqlServerDatabase(sp.GetRequiredService<SqlConnection>()));
        services.AddSqlServerOperationStore((_, store) => store.TableName = "OperationAlt", schema: Schema);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<SqlServerOperationStore>();
        store.Schema.Should().Be(Schema);
        store.TableName.Should().Be("OperationAlt");
        store.Should().BeSameAs(scope.ServiceProvider.GetRequiredService<IOperationStore>());
        store.Database.Should().BeSameAs(scope.ServiceProvider.GetRequiredService<SqlServerDatabase>());
    }

    [Test]
    public async Task Independent_Database_Factory_Must_Not_Return_Null()
    {
        var store = CreateStore(SuiteOptions(), CreateDatabase());
        store.OperationDatabaseFactory = () => null!; // Simulates a broken factory violating its non-nullable contract.

        Func<Task> claim = async () => await store.ClaimAsync(Guid.NewGuid(), "worker", ClaimDuration);
        await claim.Should().ThrowAsync<InvalidOperationException>().WithMessage("The operation database factory returned null.");
    }

    [Test]
    public async Task Migration_Script_Is_Idempotent_For_Schema_And_Creates_Distinct_Tables()
    {
        await ExecuteAsync(_connectionString, $"DROP TABLE IF EXISTS [{Schema}].[OperationAlt]");
        var script = await OperationScriptTestHelper.ExportAsync(typeof(CoreEx.Database.SqlServer.Test.Console.Program), OperationScriptTestHelper.SqlServerOfflineConnection, Schema, "OperationAlt");
        await ExecuteAsync(_connectionString, script);

        var store = CreateStore(SuiteOptions(), CreateDatabase());
        store.TableName = "OperationAlt";
        var created = await store.CreateAsync(OperationName, NewKey());

        created.Created.Should().BeTrue();
        (await CreateStore(SuiteOptions()).GetByIdAsync(created.State.OperationId)).Should().BeNull();

        await ExecuteAsync(_connectionString, $"DROP TABLE [{Schema}].[OperationAlt]");
    }

    [Test]
    public async Task RowVersion_Column_Maps_To_Operation_ETag()
    {
        var store = CreateStore(SuiteOptions(), CreateDatabase());
        var created = await store.CreateAsync(OperationName, NewKey());

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand($"SELECT [RowVersion] FROM [{Schema}].[{SqlServerOperationStore.DefaultTableName}] WHERE [OperationId] = @OperationId", connection);
        command.Parameters.AddWithValue("@OperationId", created.State.OperationId);
        var initialVersion = await command.ExecuteScalarAsync() as byte[] ?? throw new InvalidOperationException("The operation row version was not found.");
        created.State.ETag.Should().Be(Convert.ToBase64String(initialVersion));

        var claimed = await store.ClaimAsync(created.State.OperationId, "worker", ClaimDuration);
        claimed.Outcome.Should().Be(OperationClaimOutcome.Claimed);
        var claimedState = claimed.State ?? throw new InvalidOperationException("The claimed operation state was not returned.");
        var updatedVersion = await command.ExecuteScalarAsync() as byte[] ?? throw new InvalidOperationException("The operation row version was not found.");
        claimedState.ETag.Should().Be(Convert.ToBase64String(updatedVersion)).And.NotBe(created.State.ETag);
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
        var blockerConnection = (SqlConnection)await blockerDb.GetConnectionAsync();
        await using var transaction = (SqlTransaction)await blockerConnection.BeginTransactionAsync();
        blockerDb.UseTransaction(transaction);
        (await CreateStore(options, blockerDb).CreateAsync(OperationName, key)).Created.Should().BeTrue();

        var applicationName = $"coreex-create-{Guid.NewGuid():N}";
        var csb = new SqlConnectionStringBuilder(_connectionString) { ApplicationName = applicationName };
        var contenderDb = new SqlServerDatabase(new SqlConnection(csb.ConnectionString));
        lock (_databases)
            _databases.Add(contenderDb);

        var pending = CreateStore(options, contenderDb).CreateAsync(OperationName, key);
        DateTimeOffset releasedAt;
        try
        {
            await WaitForBlockedRequestsAsync(applicationName, 1);
            await AdvanceAsync(PastInactivity);
            await using var clock = new SqlCommand("SELECT SYSUTCDATETIME()", blockerConnection, transaction);
            releasedAt = new DateTimeOffset(DateTime.SpecifyKind((DateTime)(await clock.ExecuteScalarAsync())!, DateTimeKind.Utc));
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
        services.AddScoped(_ => new SqlConnection(_connectionString));
        services.AddScoped(sp => new SqlServerDatabase(sp.GetRequiredService<SqlConnection>()));
        services.AddSqlServerUnitOfWork();
        services.AddSqlServerOperationStore(schema: Schema);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SqlServerDatabase>();
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
            var rows = await db.Statement($"INSERT INTO [{Schema}].[{_businessTable}] ([Id]) VALUES (@Id)").Param("Id", businessId).NonQueryAsync(ct);
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
        var count = await observerDb.Statement($"SELECT COUNT(*) FROM [{Schema}].[{_businessTable}] WHERE [Id] = @Id").Param("Id", businessId).ScalarAsync<int>();
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

        await using var blocker = new SqlConnection(_connectionString);
        await blocker.OpenAsync();
        await using var transaction = (SqlTransaction)await blocker.BeginTransactionAsync();
        await using (var command = new SqlCommand($"UPDATE [{Schema}].[{SqlServerOperationStore.DefaultTableName}] SET [Status] = [Status] WHERE [OperationId] IN (@id0, @id1, @id2, @id3)", blocker, transaction))
        {
            for (var i = 0; i < claims.Length; i++)
                command.Parameters.AddWithValue($"@id{i}", claims[i].OperationId);

            (await command.ExecuteNonQueryAsync()).Should().Be(claims.Length);
        }

        var applicationName = $"coreex-fencing-{Guid.NewGuid():N}";
        var csb = new SqlConnectionStringBuilder(_connectionString) { ApplicationName = applicationName };
        var db = new SqlServerDatabase(new SqlConnection(csb.ConnectionString));
        lock (_databases)
            _databases.Add(db);

        var store = CreateStore(SuiteOptions(), db);
        var completion = store.CompleteAsync(claims[0], OperationCompletion.Succeeded());
        var renewal = store.RenewAsync(claims[1], ClaimDuration);
        var release = store.ReleaseAsync(claims[2]);
        var takeover = store.ClaimAsync(claims[3].OperationId, "worker-b", ClaimDuration);

        try
        {
            await WaitForBlockedRequestsAsync(applicationName, claims.Length);
            await using var clock = new SqlCommand("SELECT SYSUTCDATETIME()", blocker, transaction);
            var now = DateTime.SpecifyKind((DateTime)(await clock.ExecuteScalarAsync())!, DateTimeKind.Utc);
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
        await using var clockAfter = new SqlCommand("SELECT SYSUTCDATETIME()", blocker);
        var after = DateTime.SpecifyKind((DateTime)(await clockAfter.ExecuteScalarAsync())!, DateTimeKind.Utc);
        takeoverResult.Claim.ExpiresOn.Should().BeAfter(new DateTimeOffset(after));
    }

    private async Task WaitForBlockedRequestsAsync(string applicationName, int count)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("SELECT COUNT(*) FROM sys.dm_exec_requests r JOIN sys.dm_exec_sessions s ON r.session_id = s.session_id WHERE s.program_name = @name AND r.blocking_session_id <> 0", connection);
        command.Parameters.AddWithValue("@name", applicationName);
        for (var i = 0; i < 200; i++)
        {
            if ((int)(await command.ExecuteScalarAsync())! == count)
                return;

            await Task.Delay(25);
        }

        throw new TimeoutException($"Expected {count} blocked SQL Server requests.");
    }
}
