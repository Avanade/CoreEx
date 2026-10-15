namespace Contoso.Test.Aspire;

/// <summary>Exercises the composition-only snapshot bridge over isolated real PostgreSQL and SQL Server databases.</summary>
[TestFixture, NonParallelizable, Explicit("Resets only contoso_search_eval and Contoso_demo_eval and requires the configured real embedding model.")]
public class DemoCatalogBootstrapTests
{
    private IConfiguration _configuration = new ConfigurationBuilder().Build();
    private string _productsConnection = string.Empty;
    private string _shoppingConnection = string.Empty;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("products-settings.json")
            .AddJsonFile("e2e-settings.json")
            .AddEnvironmentVariables()
            .Build();

        _productsConnection = _configuration["E2E:Products:ConnectionString"].ThrowIfNullOrEmpty();
        _shoppingConnection = _configuration["E2E:Shopping:ConnectionString"].ThrowIfNullOrEmpty();

        if (new Npgsql.NpgsqlConnectionStringBuilder(_productsConnection).Database != "contoso_search_eval"
            || new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_shoppingConnection).InitialCatalog != "Contoso_demo_eval")
            throw new InvalidOperationException("Demo bootstrap tests require E2E__Products__ConnectionString targeting contoso_search_eval and E2E__Shopping__ConnectionString targeting Contoso_demo_eval. No shared database may be reset.");

        if (StackExchange.Redis.ConfigurationOptions.Parse(_configuration["Aspire:StackExchange:Redis:ConnectionString"].ThrowIfNullOrEmpty()).DefaultDatabase != 14)
            throw new InvalidOperationException("Demo bootstrap tests require an isolated Redis connection with defaultDatabase=14; shared serving reference caches must not be modified.");
    }

    [Test]
    public async Task DemoCatalog_ResetReconcileAndRepeat_ActualSourceAndSearchAreReady()
    {
        using (var cacheHost = DemoCatalogBootstrap.CreateServingCacheHost(_configuration, "Products"))
        {
            await using var scope = cacheHost.Services.CreateAsyncScope();
            var cache = scope.ServiceProvider.GetRequiredService<CoreEx.RefData.IReferenceDataCache>();
            await cache.RemoveAsync(typeof(BrandCollection)).ConfigureAwait(false);
            await cache.GetOrCreateAsync(typeof(BrandCollection),
                (_, _) => Task.FromResult<CoreEx.RefData.Abstractions.IReferenceDataCollection>(new BrandCollection { new Brand { Id = "OLD-SNAPSHOT", Code = "OLD-SNAPSHOT", Text = "Stale brand" } })).ConfigureAwait(false);
        }

        var bootstrap = await DemoCatalogBootstrap.RunAsync(_configuration).ConfigureAwait(false);
        bootstrap.Should().Be(173);

        using (var cacheHost = DemoCatalogBootstrap.CreateServingCacheHost(_configuration, "Products"))
        {
            await using var scope = cacheHost.Services.CreateAsyncScope();
            var cache = scope.ServiceProvider.GetRequiredService<CoreEx.RefData.IReferenceDataCache>();
            var brands = await cache.GetOrCreateAsync(typeof(BrandCollection),
                (_, _) => Task.FromResult<CoreEx.RefData.Abstractions.IReferenceDataCollection>(new BrandCollection { new Brand { Id = "NEW-SNAPSHOT", Code = "NEW-SNAPSHOT", Text = "Fresh brand" } })).ConfigureAwait(false);
            ((BrandCollection)brands).Single().Code.Should().Be("NEW-SNAPSHOT");
            await cache.RemoveAsync(typeof(BrandCollection)).ConfigureAwait(false);
        }

        using var products = DemoCatalogBootstrap.CreateProductsHost(_configuration, _productsConnection);
        using var shopping = DemoCatalogBootstrap.CreateShoppingHost(_configuration, _shoppingConnection);
        await DemoCatalogBootstrap.VerifyAsync(products, shopping).ConfigureAwait(false);

        await using (var scope = products.Services.CreateAsyncScope())
        {
            var ef = scope.ServiceProvider.GetRequiredService<ProductsEfDb>();
            var rows = await ef.Products.Model.Query().AsNoTracking().ToListAsync().ConfigureAwait(false);
            rows.Should().HaveCount(173);
            rows.Should().NotContain(p => p.Sku.StartsWith("DEMO-", StringComparison.Ordinal));
            rows.Select(p => p.Sku).Should().OnlyHaveUniqueItems();
            rows.Single(p => p.Id == 41.ToGuid().ToString()).Sku.Should().Be("SPECIALIZED-DEMO");
            rows.Count(p => !p.IsInactive && !p.IsNonStocked).Should().Be(170);
            rows.Count(p => p.IsInactive).Should().Be(2);
            var labor = rows.Single(p => p.Sku == "LABOR");
            labor.IsNonStocked.Should().BeTrue();
            labor.Description.Should().Contain("charged by the hour").And.Contain("mountain-bike maintenance and repair");
            labor.Tags.Should().Contain(["bike-service", "bicycle-repair", "workshop-labour", "workshop-labor",
                "bike-mechanic", "maintenance", "wrench-monkey", "grease-monkey", "bike-whisperer", "spanner-man"]);
            rows.Should().NotContain(p => p.Sku == "GIANT-TALON-2");

            (await ef.Brands.Query().CountAsync().ConfigureAwait(false)).Should().Be(12);
            (await ef.SubCategories.Query().CountAsync().ConfigureAwait(false)).Should().Be(31);

            var db = scope.ServiceProvider.GetRequiredService<ProductsDbContext>();
            (await db.Database.SqlQuery<int>($"SELECT COUNT(*)::integer AS \"Value\" FROM products.product").SingleAsync().ConfigureAwait(false)).Should().Be(174);
            (await db.Database.SqlQuery<int>($"SELECT COUNT(*)::integer AS \"Value\" FROM products.product WHERE is_deleted = true").SingleAsync().ConfigureAwait(false)).Should().Be(1);
        }

        await using (var scope = shopping.Services.CreateAsyncScope())
        {
            var ef = scope.ServiceProvider.GetRequiredService<Contoso.Shopping.Infrastructure.Repositories.ShoppingEfDb>();
            (await ef.Baskets.Query().CountAsync().ConfigureAwait(false)).Should().Be(8);
            (await ef.BasketItems.Query().CountAsync().ConfigureAwait(false)).Should().Be(13);
            var labor = await ef.Products.Query().SingleAsync(p => p.Sku == "LABOR").ConfigureAwait(false);
            labor.UnitOfMeasureCode.Should().Be("HR");
            labor.Price.Should().Be(80);
            labor.Id.Should().Be(32.ToGuid().ToString());
        }

        // Change actual source values, including flags; do not assert against a second hard-coded replica catalog.
        string deletedId;
        await using (var scope = products.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProductsDbContext>();
            var ef = scope.ServiceProvider.GetRequiredService<ProductsEfDb>();
            var row = await ef.Products.Model.QueryTracked().SingleAsync(p => p.Sku == "FOXRACING-UNION-FLAT-SHOE").ConfigureAwait(false);
            row.Sku = "SNAPSHOT-SHOE";
            row.Text = "Snapshot changed shoe";
            row.Price = 123.45m;
            row.UnitOfMeasureCode = "SET";
            row.IsInactive = true;
            row.IsNonStocked = true;

            var deleted = await ef.Products.Model.QueryTracked().SingleAsync(p => p.Sku == "FOXRACING-UNION-BOA-FLAT").ConfigureAwait(false);
            deletedId = deleted.Id;
            deleted.IsDeleted = true;

            await db.SaveChangesAsync().ConfigureAwait(false);
        }

        var mismatch = () => DemoCatalogBootstrap.VerifyAsync(products, shopping);
        await mismatch.Should().ThrowAsync<InvalidOperationException>().WithMessage("*replica mismatch*").ConfigureAwait(false);
        (await DemoCatalogBootstrap.SynchronizeAsync(products, shopping).ConfigureAwait(false)).Should().Be(172);
        await DemoCatalogBootstrap.VerifyAsync(products, shopping).ConfigureAwait(false);
        (await DemoCatalogBootstrap.SynchronizeAsync(products, shopping).ConfigureAwait(false)).Should().Be(172);

        await using (var scope = shopping.Services.CreateAsyncScope())
        {
            var ef = scope.ServiceProvider.GetRequiredService<Contoso.Shopping.Infrastructure.Repositories.ShoppingEfDb>();

            (await ef.Products.Query().AnyAsync(p => p.Id == deletedId).ConfigureAwait(false)).Should().BeFalse();
            var changed = await ef.Products.Query().SingleAsync(p => p.Sku == "SNAPSHOT-SHOE").ConfigureAwait(false);
            changed.Id.Should().Be(138.ToGuid().ToString());
            changed.Text.Should().Be("Snapshot changed shoe");
            changed.Price.Should().Be(123.45m);
            changed.UnitOfMeasureCode.Should().Be("SET");
            changed.IsInactive.Should().BeTrue();
            changed.IsNonStocked.Should().BeTrue();

            await ef.Products.CreateAsync(new Contoso.Shopping.Infrastructure.Persistence.Product
            {
                Id = "snapshot-stale-product", Sku = "SNAPSHOT-STALE", Text = "Stale snapshot", UnitOfMeasureCode = "EA", Price = 1
            }).ConfigureAwait(false);
        }

        (await DemoCatalogBootstrap.SynchronizeAsync(products, shopping).ConfigureAwait(false)).Should().Be(172);
        await DemoCatalogBootstrap.VerifyAsync(products, shopping).ConfigureAwait(false);

        await using (var scope = shopping.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Contoso.Shopping.Infrastructure.Repositories.ShoppingDbContext>();
            var ef = scope.ServiceProvider.GetRequiredService<Contoso.Shopping.Infrastructure.Repositories.ShoppingEfDb>();
            var item = await ef.BasketItems.QueryTracked().FirstAsync().ConfigureAwait(false);
            var originalId = item.ProductId;
            try
            {
                item.ProductId = "missing-snapshot-product";
                await db.SaveChangesAsync().ConfigureAwait(false);
                var integrity = () => DemoCatalogBootstrap.VerifyAsync(products, shopping);
                await integrity.Should().ThrowAsync<InvalidOperationException>().WithMessage("*missing product or basket*").ConfigureAwait(false);
            }
            finally
            {
                item.ProductId = originalId;
                await db.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        await using (var scope = products.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProductsDbContext>();
            var ef = scope.ServiceProvider.GetRequiredService<ProductsEfDb>();
            var brand = await ef.Brands.Query().SingleAsync(b => b.Code == "FOX").ConfigureAwait(false);
            try
            {
                db.Remove(brand);
                await db.SaveChangesAsync().ConfigureAwait(false);
                using var fresh = DemoCatalogBootstrap.CreateProductsHost(_configuration, _productsConnection);
                var reference = () => DemoCatalogBootstrap.VerifyAsync(fresh, shopping);
                await reference.Should().ThrowAsync<InvalidOperationException>().WithMessage("*reference data is missing*").ConfigureAwait(false);
            }
            finally
            {
                db.Add(brand);
                await db.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var cancel = () => DemoCatalogBootstrap.RunAsync(_configuration, cancelled.Token);
        await cancel.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
        await DemoCatalogBootstrap.VerifyAsync(products, shopping).ConfigureAwait(false);

        var brokenConfiguration = new ConfigurationBuilder().AddConfiguration(_configuration)
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Ollama:BaseAddress"] = "http://127.0.0.1:1/" }).Build();
        using (var broken = DemoCatalogBootstrap.CreateProductsHost(brokenConfiguration, _productsConnection))
        {
            var rebuild = () => DemoCatalogBootstrap.PrepareSearchAsync(broken);
            var failure = await rebuild.Should().ThrowAsync<InvalidOperationException>().WithMessage("*search preparation failed*").ConfigureAwait(false);
            failure.Which.InnerException.Should().NotBeNull();
            failure.Which.ToString().Should().Contain("127.0.0.1:1");
        }

        // A full rerun must clear stale projections and restore deterministic product/basket identities.
        (await DemoCatalogBootstrap.RunAsync(_configuration).ConfigureAwait(false)).Should().Be(173);
        await DemoCatalogBootstrap.VerifyAsync(products, shopping).ConfigureAwait(false);
        await using (var scope = products.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<ProductSearchIndex>().EnsureReadyAsync().ConfigureAwait(false);
            await scope.ServiceProvider.GetRequiredService<ProductLexicalSearchIndex>().EnsureReadyAsync().ConfigureAwait(false);
            var db = scope.ServiceProvider.GetRequiredService<ProductsDbContext>();
            var profile = scope.ServiceProvider.GetRequiredService<IProductEmbeddingAdapter>().Profile;
            (await db.Database.SqlQuery<int>($"SELECT COUNT(*)::integer AS \"Value\" FROM products.product_search WHERE profile = {profile}").SingleAsync().ConfigureAwait(false)).Should().Be(173);
            (await db.Database.SqlQuery<int>($"SELECT COUNT(*)::integer AS \"Value\" FROM products.product_lexical_search").SingleAsync().ConfigureAwait(false)).Should().Be(173);
        }
    }
}
