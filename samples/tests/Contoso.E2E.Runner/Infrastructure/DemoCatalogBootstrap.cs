namespace Contoso.E2E.Runner.Infrastructure;

/// <summary>Loads the shared demo snapshot and prepares both domains before hosts accept demo traffic.</summary>
/// <remarks>This composition-only bridge uses the same wire contract and sync adapter as event consumption; it does not exercise the broker or outbox.</remarks>
public static class DemoCatalogBootstrap
{
    /// <summary>Resets only Products and Shopping, seeds replicas before baskets, and verifies search readiness.</summary>
    public static async Task<int> RunAsync(IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var productsConnection = RequiredConnection(configuration, "Products");
        var shoppingConnection = RequiredConnection(configuration, "Shopping");
        cancellationToken.ThrowIfCancellationRequested();

        var productsArgs = Products.Database.Program.ConfigureMigrationArgs(new MigrationArgs(MigrationCommand.ResetAndDatabase, productsConnection));
        productsArgs.DataParserArgs.NamedResources.Clear();
        productsArgs.DataParserArgs.AddNamed<Products.Database.Program>("ref-data.seed.yaml", "demo-catalog.seed.yaml");

        using (var migration = new PostgresMigration(productsArgs))
            await MigrateAsync(migration, cancellationToken).ConfigureAwait(false);

        var shoppingArgs = Shopping.Database.Program.ConfigureMigrationArgs(new MigrationArgs(MigrationCommand.ResetAndDatabase, shoppingConnection));
        using (var migration = new SqlServerMigration(shoppingArgs))
            await MigrateAsync(migration, cancellationToken).ConfigureAwait(false);

        using var products = CreateProductsHost(configuration, productsConnection);
        using var shopping = CreateShoppingHost(configuration, shoppingConnection);
        var count = await SynchronizeAsync(products, shopping, cancellationToken).ConfigureAwait(false);

        var basketArgs = Shopping.Database.Program.ConfigureMigrationArgs(new MigrationArgs(MigrationCommand.Data, shoppingConnection));
        basketArgs.AddAssembly<DatabaseMigrationSetup>().DataParserArgs.AddNamed<DatabaseMigrationSetup>("demo-baskets.seed.yaml");
        using (var migration = new SqlServerMigration(basketArgs))
            await MigrateAsync(migration, cancellationToken).ConfigureAwait(false);

        using (var cache = CreateServingCacheHost(configuration, "Products"))
            await InvalidateReferenceDataAsync(cache, [typeof(BrandCollection), typeof(CategoryCollection), typeof(SubCategoryCollection),
                typeof(Products.Contracts.UnitOfMeasureCollection), typeof(MovementKindCollection), typeof(MovementStatusCollection)], cancellationToken).ConfigureAwait(false);

        using (var cache = CreateServingCacheHost(configuration, "Shopping"))
            await InvalidateReferenceDataAsync(cache, [typeof(Shopping.Contracts.UnitOfMeasureCollection),
                typeof(Shopping.Contracts.BasketStatusCollection), typeof(Shopping.Contracts.DiscountCouponCollection)], cancellationToken).ConfigureAwait(false);

        await VerifyAsync(products, shopping, cancellationToken).ConfigureAwait(false);
        await PrepareSearchAsync(products, cancellationToken).ConfigureAwait(false);
        return count;
    }

    /// <summary>Creates an isolated Products composition using the real configured embedding client and fresh local reference-data cache.</summary>
    public static IHost CreateProductsHost(IConfiguration configuration, string connectionString)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddConfiguration(configuration);
        builder.AddProductEmbeddings();
        builder.Services.AddSingleton(_ => Npgsql.NpgsqlDataSource.Create(connectionString));

        builder.Services.AddExecutionContext().AddMemoryCache().AddMemoryOnlyHybridCache().AddDefaultCacheKeyProvider()
            .AddReferenceDataOrchestrator<Products.Application.ReferenceDataProvider>()
            .AddScoped<Products.Application.ReferenceDataProvider>()
            .AddScoped<Products.Application.Repositories.IReferenceDataRepository, Products.Infrastructure.Repositories.ReferenceDataRepository>()
            .AddScoped<Products.Application.Adapters.Embeddings.IProductEmbeddingAdapter, Products.Infrastructure.Adapters.Embeddings.ProductEmbeddingAdapter>()
            .AddPostgresDatabase().AddPostgresUnitOfWork().AddDbContext<ProductsDbContext>().AddEfDb<ProductsEfDb>()
            .AddScoped<ProductLexicalSearchIndex>().AddScoped<ProductSearchIndex>();

        return builder.Build();
    }

    /// <summary>Creates a separate Shopping scope; Products reference data is never registered in this domain.</summary>
    public static IHost CreateShoppingHost(IConfiguration configuration, string connectionString)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddConfiguration(configuration);

        builder.Services.AddExecutionContext().AddMemoryCache().AddMemoryOnlyHybridCache().AddDefaultCacheKeyProvider()
            .AddReferenceDataOrchestrator<Shopping.Application.ReferenceDataProvider>()
            .AddScoped<Shopping.Application.ReferenceDataProvider>()
            .AddScoped<Shopping.Application.Repositories.IReferenceDataRepository, Shopping.Infrastructure.Repositories.ReferenceDataRepository>()
            .AddScoped(_ => new Microsoft.Data.SqlClient.SqlConnection(connectionString))
            .AddSqlServerDatabase().AddDbContext<ShoppingDbContext>().AddEfDb<ShoppingEfDb>()
            .AddScoped<Shopping.Application.Adapters.Products.IProductSyncAdapter, Shopping.Infrastructure.Adapters.Products.ProductSyncAdapter>();

        return builder.Build();
    }

    /// <summary>Uses the serving hosts' existing Redis/FusionCache integration for targeted reference-data eviction, never a database flush.</summary>
    public static IHost CreateServingCacheHost(IConfiguration configuration, string domainName)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddConfiguration(configuration);
        builder.AddHostSettings(solutionName: "Contoso", domainName: domainName);
        builder.AddRedisDistributedCache("redis");
        builder.Services.AddExecutionContext().AddMemoryCache().AddDefaultCacheKeyProvider().AddFusionHybridCache();

        builder.Services.AddFusionCache()
            .WithRegisteredMemoryCache()
            .WithRegisteredDistributedCache()
            .WithBackplane(sp => new RedisBackplane(new RedisBackplaneOptions { Configuration = sp.GetRequiredService<IOptions<ConfigurationOptions>>().Value.ToString() }))
            .WithSystemTextJsonSerializer(JsonDefaults.SerializerOptions)
            .WithDefaultEntryOptions(options =>
            {
                options.AllowBackgroundDistributedCacheOperations = false;
                options.AllowBackgroundBackplaneOperations = false;
                options.ReThrowDistributedCacheExceptions = true;
                options.ReThrowBackplaneExceptions = true;
            });

        builder.Services.AddScoped<IReferenceDataCache, ReferenceDataHybridCache>();
        return builder.Build();
    }

    /// <summary>Reconciles actual non-deleted source rows through Shopping's normal upsert/delete adapter.</summary>
    public static async Task<int> SynchronizeAsync(IHost products, IHost shopping, CancellationToken cancellationToken = default)
    {
        var snapshot = await ReadSnapshotAsync(products, cancellationToken).ConfigureAwait(false);
        return await InScopeAsync(shopping, async services =>
        {
            var ef = services.GetRequiredService<ShoppingEfDb>();
            var adapter = services.GetRequiredService<Shopping.Application.Adapters.Products.IProductSyncAdapter>();
            var ids = snapshot.Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
            var existing = await ef.Products.Query().AsNoTracking().Select(p => p.Id).ToListAsync(cancellationToken).ConfigureAwait(false);

            foreach (var id in existing.Where(id => !ids.Contains(id)))
                (await adapter.DeleteAsync(id, cancellationToken).ConfigureAwait(false)).ThrowOnError();

            foreach (var product in snapshot)
                (await adapter.ModifyAsync(product, cancellationToken).ConfigureAwait(false)).ThrowOnError();
            return snapshot.Count;
        }).ConfigureAwait(false);
    }

    /// <summary>Verifies exact replica fields, no stale rows, local reference data, and basket product integrity.</summary>
    public static async Task VerifyAsync(IHost products, IHost shopping, CancellationToken cancellationToken = default)
    {
        var snapshot = await ReadSnapshotAsync(products, cancellationToken).ConfigureAwait(false);
        await InScopeAsync(shopping, async services =>
        {
            var ef = services.GetRequiredService<ShoppingEfDb>();
            var replicas = await ef.Products.Query().AsNoTracking().OrderBy(p => p.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
            var expected = snapshot.OrderBy(p => p.Id).Select(p => (p.Id.ThrowIfNullOrEmpty(), p.Sku.ThrowIfNullOrEmpty(), p.Text.ThrowIfNullOrEmpty(), p.UnitOfMeasureCode.ThrowIfNullOrEmpty(), p.Price, p.IsInactive, p.IsNonStocked));
            var actual = replicas.Select(p => (p.Id, p.Sku, p.Text, p.UnitOfMeasureCode, p.Price, p.IsInactive, p.IsNonStocked));
            if (!expected.SequenceEqual(actual))
                throw new InvalidOperationException("Demo Products/Shopping replica mismatch (including stale or deleted products).");

            var units = await ef.UnitsOfMeasure.Query().AsNoTracking().Select(u => u.Code).ToListAsync(cancellationToken).ConfigureAwait(false);
            if (replicas.Any(p => !units.Contains(p.UnitOfMeasureCode)))
                throw new InvalidOperationException("Demo Shopping unit-of-measure reference data is missing.");

            var ids = replicas.Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
            var items = await ef.BasketItems.Query().AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
            var baskets = await ef.Baskets.Query().AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
            if (baskets.Count == 0 || items.Count == 0)
                throw new InvalidOperationException("Demo basket seed was not loaded.");

            var basketIds = baskets.Select(b => b.Id).ToHashSet(StringComparer.Ordinal);
            if (items.Any(i => !ids.Contains(i.ProductId) || !basketIds.Contains(i.BasketId)))
                throw new InvalidOperationException("Demo basket item references a missing product or basket.");

            var statuses = await ef.BasketStatuses.Query().Select(s => s.Code).ToListAsync(cancellationToken).ConfigureAwait(false);
            var coupons = await ef.DiscountCoupons.Query().Select(c => c.Code).ToListAsync(cancellationToken).ConfigureAwait(false);
            if (items.Any(i => !units.Contains(i.UnitOfMeasureCode))
                || baskets.Any(b => !statuses.Contains(b.BasketStatusCode) || b.DiscountCouponCode is not null && !coupons.Contains(b.DiscountCouponCode)))
                throw new InvalidOperationException("Demo Shopping basket reference data is missing.");

            return true;
        }).ConfigureAwait(false);
    }

    /// <summary>Builds lexical and semantic projections with the configured real provider; failures are not swallowed or replaced by test vectors.</summary>
    public static Task<int> PrepareSearchAsync(IHost products, CancellationToken cancellationToken = default)
        => InScopeAsync(products, async services =>
        {
            var profile = services.GetRequiredService<Products.Application.Adapters.Embeddings.IProductEmbeddingAdapter>().Profile;
            try
            {
                var semantic = services.GetRequiredService<ProductSearchIndex>();
                var count = await semantic.RebuildAsync(cancellationToken).ConfigureAwait(false);
                await semantic.EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
                await services.GetRequiredService<ProductLexicalSearchIndex>().EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
                await semantic.EmbedQueryAsync("mountain bike", cancellationToken).ConfigureAwait(false);

                var expected = await services.GetRequiredService<ProductsEfDb>().Products.Model.Query().CountAsync(cancellationToken).ConfigureAwait(false);
                if (count != expected)
                    throw new InvalidOperationException($"Demo search projection is incomplete: expected {expected}, indexed {count}.");

                return count;
            }
            catch (Exception ex) when (ex is not OperationCanceledException && !cancellationToken.IsCancellationRequested)
            {
                throw new InvalidOperationException($"Demo lexical/semantic search preparation failed for embedding profile '{profile}'. Verify the configured provider/model and rerun setup.", ex);
            }
        });

    private static Task<List<Shopping.Application.Adapters.Products.Product>> ReadSnapshotAsync(IHost products, CancellationToken cancellationToken)
        => InScopeAsync(products, async services =>
        {
            await services.GetRequiredService<CoreEx.RefData.ReferenceDataOrchestrator>()
                .PrefetchAsync([typeof(Brand), typeof(Category), typeof(SubCategory), typeof(Products.Contracts.UnitOfMeasure)], cancellationToken).ConfigureAwait(false);

            var ef = services.GetRequiredService<ProductsEfDb>();
            var source = await ef.Products.Model.Query().AsNoTracking().OrderBy(p => p.Id)
                .ToMappedItemsAsync(Products.Infrastructure.Mapping.ProductMapper.From, cancellationToken).ConfigureAwait(false);

            var brands = await ef.Brands.Query().Select(b => b.Code).ToListAsync(cancellationToken).ConfigureAwait(false);
            var categories = await ef.Categories.Query().Select(c => c.Code).ToListAsync(cancellationToken).ConfigureAwait(false);
            var subcategories = await ef.SubCategories.Query().Select(s => s.Code).ToListAsync(cancellationToken).ConfigureAwait(false);
            var units = await ef.UnitsOfMeasure.Query().Select(u => u.Code).ToListAsync(cancellationToken).ConfigureAwait(false);

            if (source.Count == 0 || source.Any(p => p.BrandCode is null || !brands.Contains(p.BrandCode)
                || p.CategoryCode is null || !categories.Contains(p.CategoryCode)
                || p.SubCategoryCode is null || !subcategories.Contains(p.SubCategoryCode)
                || p.UnitOfMeasureCode is null || !units.Contains(p.UnitOfMeasureCode)))
                throw new InvalidOperationException("Demo Products catalog is empty or reference data is missing.");

            // Match event consumption's JSON contract boundary instead of maintaining another field mapper.
            return source.Select(p => JsonSerializer.Deserialize<Shopping.Application.Adapters.Products.Product>(
                JsonSerializer.Serialize(p, JsonDefaults.SerializerOptions), JsonDefaults.SerializerOptions)
                ?? throw new InvalidOperationException("Demo product snapshot deserialization failed.")).ToList();
        });

    private static async Task<T> InScopeAsync<T>(IHost host, Func<IServiceProvider, Task<T>> action)
    {
        await using var scope = host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<CoreEx.ExecutionContext>();
        try
        {
            return await action(scope.ServiceProvider).ConfigureAwait(false);
        }
        finally
        {
            CoreEx.ExecutionContext.Reset();
        }
    }

    private static string RequiredConnection(IConfiguration configuration, string domain)
        => configuration[$"E2E:{domain}:ConnectionString"].ThrowIfNullOrEmpty($"E2E:{domain}:ConnectionString");

    private static Task<bool> InvalidateReferenceDataAsync(IHost host, Type[] collections, CancellationToken cancellationToken)
        => InScopeAsync(host, async services =>
        {
            var cache = services.GetRequiredService<IReferenceDataCache>();
            foreach (var type in collections)
                await cache.RemoveAsync(type, cancellationToken).ConfigureAwait(false);
            return true;
        });

    private static async Task MigrateAsync(DatabaseMigrationBase migration, CancellationToken cancellationToken)
    {
        var (success, output) = await migration.MigrateAndLogAsync(cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (!success)
            throw new InvalidOperationException("Demo database migration failed:" + Environment.NewLine + output);
    }
}
