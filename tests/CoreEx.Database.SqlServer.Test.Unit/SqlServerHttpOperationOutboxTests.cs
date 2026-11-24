namespace CoreEx.Database.SqlServer.Test.Unit;

[TestFixture, NonParallelizable]
public sealed class SqlServerHttpOperationOutboxTests : HttpOperationOutboxTestsBase
{
    private readonly string _schema = $"HttpProof_{Guid.NewGuid():N}";
    private string _connectionString = null!;
    private bool _schemaCreated;

    [OneTimeSetUp]
    public async Task SetUp()
    {
        _connectionString = new ConfigurationBuilder().AddJsonFile("appsettings.unittest.json").Build().GetValue<string>("Aspire:Microsoft:Data:SqlClient:ConnectionString")!;
        await ExecuteAsync($"CREATE SCHEMA [{_schema}]");
        _schemaCreated = true;
        var operations = await OperationScriptTestHelper.ExportAsync(typeof(CoreEx.Database.SqlServer.Test.Console.Program), OperationScriptTestHelper.SqlServerOfflineConnection, _schema, "Operation");
        await ExecuteAsync(operations);
        await ExecuteAsync((await ReadResourceAsync("OutboxTables.sql")).Replace("Shopping", _schema, StringComparison.Ordinal));
        await ExecuteAsync((await ReadResourceAsync("OutboxEnqueue.sql")).Replace("Shopping", _schema, StringComparison.Ordinal));
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        if (!_schemaCreated)
            return;

        await ExecuteAsync($"DROP PROCEDURE IF EXISTS [{_schema}].[spOutboxEnqueue]; DROP TABLE IF EXISTS [{_schema}].[Outbox]; DROP TABLE IF EXISTS [{_schema}].[OutboxLease]; DROP TABLE IF EXISTS [{_schema}].[Operation]; DROP SCHEMA [{_schema}];");
    }

    protected override ServiceProvider CreateProvider(OutboxControl control)
    {
        var services = new ServiceCollection();
        ConfigureCommon(services);
        services.AddScoped(_ => new SqlConnection(_connectionString));
        services.AddScoped(sp => new SqlServerDatabase(sp.GetRequiredService<SqlConnection>()));
        services.AddSqlServerOperationStore(schema: _schema);
        services.AddScoped<CoreEx.Events.Publishing.IEventPublisher>(sp => new ProofOutbox(sp.GetRequiredService<SqlServerDatabase>(), sp.GetRequiredService<IHostSettings>(), control, _schema));
        services.AddSqlServerUnitOfWork();
        return services.BuildServiceProvider();
    }

    protected override void AssertSameDatabase(IServiceProvider services)
    {
        var database = services.GetRequiredService<SqlServerDatabase>();
        services.GetRequiredService<SqlServerOperationStore>().Database.Should().BeSameAs(database);
        services.GetRequiredService<SqlServerUnitOfWork>().Database.Should().BeSameAs(database);
        var publisher = services.GetRequiredService<CoreEx.Events.Publishing.IEventPublisher>().Should().BeOfType<ProofOutbox>().Subject;
        publisher.ProofDatabase.Should().BeSameAs(database);
        services.GetRequiredService<SqlServerUnitOfWork>().Events.Should().BeSameAs(publisher);
    }

    protected override async Task<(int Operations, int Events, string? Event)> ReadPersistedAsync()
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand($"SELECT (SELECT COUNT(*) FROM [{_schema}].[Operation]), (SELECT COUNT(*) FROM [{_schema}].[Outbox]), (SELECT TOP 1 [Event] FROM [{_schema}].[Outbox] ORDER BY [OutboxId])", connection);
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return (reader.GetInt32(0), reader.GetInt32(1), reader.IsDBNull(2) ? null : reader.GetString(2));
    }

    private async Task ExecuteAsync(string sql)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private sealed class ProofOutbox : CoreEx.Database.SqlServer.Outbox.SqlServerOutboxPublisher
    {
        private readonly OutboxControl _control;

        public ProofOutbox(SqlServerDatabase database, IHostSettings settings, OutboxControl control, string schema)
            : base(database, formatter: new CoreEx.Events.EventFormatter(settings))
        {
            ProofDatabase = database;
            _control = control;
            SetStatementByConvention(schema);
        }

        public SqlServerDatabase ProofDatabase { get; }

        protected override async Task OnPublishAsync(CoreEx.Events.Publishing.DestinationEvent[] events, CancellationToken cancellationToken = default)
        {
            await base.OnPublishAsync(events, cancellationToken);
            _control.FlushCount++;
            if (_control.FailAfterInsert)
                throw new InvalidOperationException("Injected failure after outbox insert.");
        }
    }
}
