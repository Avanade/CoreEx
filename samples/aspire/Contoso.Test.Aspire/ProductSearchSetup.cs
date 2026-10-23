namespace Contoso.Test.Aspire;

internal static class ProductSearchSetup
{
    public static async Task RebuildAsync(DistributedApplication app, CancellationToken cancellationToken = default)
    {
        var connectionString = await AspireTesterBase.GetConnectionStringAsync(app, "Postgres").ConfigureAwait(false)
            ?? throw new InvalidOperationException("The Postgres resource has no connection string.");
        await RebuildAsync(app.Services.GetRequiredService<IConfiguration>(), connectionString, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<int> RebuildAsync(IConfiguration configuration, string connectionString, CancellationToken cancellationToken = default)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddConfiguration(configuration);
        builder.Configuration["ConnectionStrings:Postgres"] = connectionString;
        builder.Configuration.GetRequiredSection("Ollama");
        builder.AddHostSettings(solutionName: "Contoso", domainName: "Products");
        builder.AddAzureNpgsqlDataSource("Postgres");
        builder.AddProductEmbeddings();
        builder.Services
            .AddPrecisionTimeProvider()
            .AddExecutionContext()
            .AddMemoryCache()
            .AddMemoryOnlyHybridCache()
            .AddReferenceDataOrchestrator()
            .AddDynamicServicesUsing<Contoso.Products.Application.ReferenceDataService, ReferenceDataRepository>()
            .AddPostgresDatabase()
            .AddPostgresUnitOfWork()
            .AddDbContext<ProductsDbContext>()
            .AddEfDb<ProductsEfDb>();

        using var host = builder.Build();
        await using var scope = host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<CoreEx.ExecutionContext>();
        try
        {
            return await scope.ServiceProvider.GetRequiredService<ProductSearchIndex>().RebuildAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            CoreEx.ExecutionContext.Reset();
        }
    }
}
