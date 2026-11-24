namespace CoreEx.Database.Postgres.Test.Unit;

[TestFixture, NonParallelizable]
public sealed class PostgresHttpOperationOutboxTests : HttpOperationOutboxTestsBase
{
    private readonly string _schema = $"http_proof_{Guid.NewGuid():N}";
    private string _connectionString = null!;
    private NpgsqlDataSource _dataSource = null!;
    private bool _schemaCreated;

    [OneTimeSetUp]
    public async Task SetUp()
    {
        _connectionString = new ConfigurationBuilder().AddJsonFile("appsettings.unittest.json").Build().GetValue<string>("Aspire:Npgsql:ConnectionString")!;
        _dataSource = NpgsqlDataSource.Create(_connectionString);
        await ExecuteAsync($"CREATE SCHEMA \"{_schema}\"");
        _schemaCreated = true;
        var operations = await OperationScriptTestHelper.ExportAsync(typeof(CoreEx.Database.Postgres.Test.Console.Program), OperationScriptTestHelper.PostgresOfflineConnection, _schema, "operation");
        await ExecuteAsync(operations);
        await ExecuteAsync((await ReadResourceAsync("OutboxTables.pgsql")).Replace("products", _schema, StringComparison.Ordinal));
        await ExecuteAsync((await ReadResourceAsync("OutboxEnqueue.pgsql")).Replace("products", _schema, StringComparison.Ordinal));
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        try
        {
            if (_schemaCreated)
                await ExecuteAsync($"DROP SCHEMA \"{_schema}\" CASCADE");
        }
        finally
        {
            if (_dataSource is not null)
                await _dataSource.DisposeAsync();
        }
    }

    protected override ServiceProvider CreateProvider(OutboxControl control)
    {
        var services = new ServiceCollection();
        ConfigureCommon(services);
        services.AddSingleton(_dataSource);
        services.AddScoped(_ => new PostgresDatabase(_dataSource));
        services.AddPostgresOperationStore(schema: _schema);
        services.AddScoped<CoreEx.Events.Publishing.IEventPublisher>(sp => new ProofOutbox(sp.GetRequiredService<PostgresDatabase>(), sp.GetRequiredService<IHostSettings>(), control, _schema));
        services.AddPostgresUnitOfWork();
        return services.BuildServiceProvider();
    }

    protected override void AssertSameDatabase(IServiceProvider services)
    {
        var database = services.GetRequiredService<PostgresDatabase>();
        services.GetRequiredService<PostgresOperationStore>().Database.Should().BeSameAs(database);
        services.GetRequiredService<PostgresUnitOfWork>().Database.Should().BeSameAs(database);
        var publisher = services.GetRequiredService<CoreEx.Events.Publishing.IEventPublisher>().Should().BeOfType<ProofOutbox>().Subject;
        publisher.ProofDatabase.Should().BeSameAs(database);
        services.GetRequiredService<PostgresUnitOfWork>().Events.Should().BeSameAs(publisher);
    }

    protected override async Task<(int Operations, int Events, string? Event)> ReadPersistedAsync()
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand($"SELECT (SELECT COUNT(*) FROM \"{_schema}\".\"operation\"), (SELECT COUNT(*) FROM \"{_schema}\".\"outbox\"), (SELECT \"event\" FROM \"{_schema}\".\"outbox\" ORDER BY \"outbox_id\" LIMIT 1)", connection);
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return ((int)reader.GetInt64(0), (int)reader.GetInt64(1), reader.IsDBNull(2) ? null : reader.GetString(2));
    }

    private async Task ExecuteAsync(string sql)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private sealed class ProofOutbox : CoreEx.Database.Postgres.Outbox.PostgresOutboxPublisher
    {
        private readonly OutboxControl _control;

        public ProofOutbox(PostgresDatabase database, IHostSettings settings, OutboxControl control, string schema)
            : base(database, formatter: new CoreEx.Events.EventFormatter(settings))
        {
            ProofDatabase = database;
            _control = control;
            SetStatementByConvention(schema);
        }

        public PostgresDatabase ProofDatabase { get; }

        protected override async Task OnPublishAsync(CoreEx.Events.Publishing.DestinationEvent[] events, CancellationToken cancellationToken = default)
        {
            await base.OnPublishAsync(events, cancellationToken);
            _control.FlushCount++;
            if (_control.FailAfterInsert)
                throw new InvalidOperationException("Injected failure after outbox insert.");
        }
    }
}
