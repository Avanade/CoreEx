namespace Contoso.Products.Test.Api;

[NonParallelizable]
public class ProductLexicalSearchTests : WithApiTester<Contoso.Products.Api.Program>
{
    private readonly TestEmbeddingAdapter _embeddings = new() { FailQuery = true, FailDocumentOnCall = 1 };

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        Test.UseAdditionalConfiguration("Products:Search:Mode", "FullText");
        Test.ReplaceScoped<IProductEmbeddingAdapter>(_ => _embeddings);
        await Test.MigratePostgresDataAsync<TestData>(["read-data.seed.yaml"], DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
        await Test.ClearFusionCacheAsync().ConfigureAwait(false);
        Rebuild();
    }

    [TestCase("  shimano-xt-m8100-rd  ", "SHIMANO-XT-M8100-RD")]
    [TestCase("MUCOFF-NANO-CLEANER", "MUCOFF-NANO-CLEANER")]
    public void ExactSku_IsFirstAndUnique(string query, string sku)
    {
        var result = Search(query);
        result[0].Sku.Should().Be(sku);
        result.Select(p => p.Id).Should().OnlyHaveUniqueItems();
        _embeddings.QueryCallCount.Should().Be(0);
        _embeddings.DocumentCallCount.Should().Be(0);
    }

    [TestCase("derailleurs", "SHIMANO-XT-M8100-RD")]
    [TestCase("derailleur!!!", "SHIMANO-XT-M8100-RD")]
    [TestCase("\"rear derailleur\"", "SHIMANO-XT-M8100-RD")]
    [TestCase("derailleur OR cleaner", "MUCOFF-NANO-CLEANER")]
    public void EnglishNormalizationAndWebsearch_Match(string query, string sku)
        => Search(query).Should().Contain(p => p.Sku == sku);

    [TestCase("the and a")]
    [TestCase("!!!")]
    [TestCase("zzzxnomatch")]
    [TestCase("'; DROP TABLE products.product; --")]
    public void EmptyLexemesAndNoMatch_AreEmptyNotFallback(string query) => Search(query).Should().BeEmpty();

    [Test]
    public void FiltersAndMatchingSetPaging_AreConsistent()
    {
        var all = Search("derailleur");
        all.Should().HaveCount(4);
        var page = Test.Http<ProductLite[]>()
            .Run(HttpMethod.Get, "/api/products?$search=derailleur&$skip=1&$take=2&$count=true").AssertOK();
        page.Value!.Select(p => p.Id).Should().Equal(all.Skip(1).Take(2).Select(p => p.Id));
        page.Response.Headers.Should().ContainKey("X-Paging-Total-Count").WhoseValue.Should().ContainSingle().Which.Should().Be("4");
        Search("derailleur", "&$filter=brand eq 'SRAM' and price lt 160").Should().ContainSingle().Which.Sku.Should().Be("SRAM-GX-EAGLE-RD");
        Search("SHIMANO-XT-M8100-RD", "&$filter=price lt 0").Should().BeEmpty();
        Search("derailleur", "&$filter=category eq 'B'").Should().BeEmpty();
        Search("derailleur", "&$filter=subcategory eq 'DR'").Should().HaveCount(3);
        Search("derailleur", "&$filter=startswith(sku,'SHIMANO')").Should().ContainSingle();
    }

    [Test]
    public void Eligibility_IsLiveEvenWithRetainedProjection()
    {
        Search("SPEC-EPIC-8-PRO").Should().BeEmpty();
        Search("SPEC-EPIC-8-PRO", "&$inactive").First().Sku.Should().Be("SPEC-EPIC-8-PRO");
        Search("GIANT-TALON-2").Should().BeEmpty();
        try
        {
            Execute("UPDATE products.product SET is_deleted = true WHERE sku = 'MUCOFF-NANO-CLEANER'");
            Search("MUCOFF-NANO-CLEANER").Should().BeEmpty();
        }
        finally
        {
            Execute("UPDATE products.product SET is_deleted = false WHERE sku = 'MUCOFF-NANO-CLEANER'");
        }
    }

    [Test]
    public void Ties_UseStableProductIdentifier()
    {
        try
        {
            Execute("UPDATE products.product SET text = 'tieprobe', description = 'tieprobe' WHERE sku IN ('YETI-ASR-C2-2025', 'YETI-SB120-C2')");
            Rebuild();
            var results = Search("tieprobe");
            results.Select(p => p.Id).Should().Equal(1.ToGuid().ToString(), 2.ToGuid().ToString());
            Search("tieprobe").Select(p => p.Id).Should().Equal(results.Select(p => p.Id));
        }
        finally
        {
            // Restore the authoritative seed rather than preserving altered descriptions.
            Test.MigratePostgresDataAsync<TestData>(["read-data.seed.yaml"], DbMigration.ConfigureMigrationArgs).GetAwaiter().GetResult();
            Rebuild();
        }
    }

    [Test]
    public void SchemaAndGraphQL_AdvertiseAndExecuteSelectedMode()
    {
        var schema = Test.Http<JsonElement>().Run(HttpMethod.Get, "/api/products/$query").AssertOK().Value;
        schema.GetProperty("search").GetProperty("mode").GetString().Should().Be("fulltext");
        schema.GetProperty("search").GetProperty("description").GetString().Should().Contain("English");
        Test.Http().Run(HttpMethod.Get, "/api/products?$search=derailleur&$orderby=sku").AssertBadRequest();
        var graphql = Test.Http<JsonElement>().Run(HttpMethod.Post, "/api/query",
            new { query = "{ products(search: \"MUCOFF-NANO-CLEANER\", first: 1) { edges { node { sku } } } }" }).AssertOK().Value;
        graphql.TryGetProperty("errors", out _).Should().BeFalse();
        graphql.GetProperty("data").GetProperty("products").GetProperty("edges")[0].GetProperty("node").GetProperty("sku").GetString().Should().Be("MUCOFF-NANO-CLEANER");
    }

    [Test]
    public void InvalidConfiguredMode_HasNoSilentFallback()
    {
        var configuration = Test.Configuration;
        try
        {
            configuration["Products:Search:Mode"] = "Hybrid";
            Action resolve = () => ProductQueryArgsConfig.FromConfiguration(configuration);
            resolve.Should().Throw<InvalidOperationException>().WithMessage("Unsupported Products:Search:Mode 'Hybrid'. Expected Semantic or FullText.");
        }
        finally
        {
            configuration["Products:Search:Mode"] = "FullText";
        }
    }

    [TestCase("")]
    [TestCase("   ")]
    public void EmptySearch_PreservesOrdinaryQueryBehavior(string text)
        => Test.Http<ProductLite[]>().Run(HttpMethod.Get, $"/api/products?$search={Uri.EscapeDataString(text)}&$take=50").AssertOK().Value.Should().HaveCount(29);

    [Test]
    public void SynchronizationAndRebuild_AreIndependentOfModelAndReadinessIsExplicit()
    {
        try
        {
            Execute("DELETE FROM products.product_lexical_search_readiness");
            Execute("UPDATE products.product SET description = 'lexicaloutageprobe' WHERE sku = 'MUCOFF-NANO-CLEANER'");
            Synchronize();
            SearchUnavailable();
            Rebuild();
            Search("lexicaloutageprobe").Should().ContainSingle().Which.Sku.Should().Be("MUCOFF-NANO-CLEANER");
            Execute("UPDATE products.product SET description = 'replacementprobe' WHERE sku = 'MUCOFF-NANO-CLEANER'");
            Synchronize();
            Search("lexicaloutageprobe").Should().BeEmpty();
            Search("replacementprobe").Should().ContainSingle();
            Execute("UPDATE products.product SET is_deleted = true WHERE sku = 'MUCOFF-NANO-CLEANER'");
            Synchronize();
            Test.Scoped(test => test.Run(async _ =>
            {
                var matches = test.Services.GetRequiredService<ProductLexicalSearchIndex>().Matches("replacementprobe");
                (await matches.CountAsync().ConfigureAwait(false)).Should().Be(0);
            }).AssertSuccess());
            _embeddings.DocumentCallCount.Should().Be(0);
            _embeddings.QueryCallCount.Should().Be(0);
        }
        finally
        {
            Test.MigratePostgresDataAsync<TestData>(["read-data.seed.yaml"], DbMigration.ConfigureMigrationArgs).GetAwaiter().GetResult();
            Rebuild();
        }
    }

    [Test]
    public void StopwordSku_StillMatchesExactly()
    {
        try
        {
            Execute("UPDATE products.product SET sku = 'THE' WHERE sku = 'MUCOFF-NANO-CLEANER'");
            Synchronize();
            Search("the").Should().ContainSingle().Which.Sku.Should().Be("THE");
        }
        finally
        {
            Execute("UPDATE products.product SET sku = 'MUCOFF-NANO-CLEANER' WHERE sku = 'THE'");
            Synchronize();
        }
    }

    [Test]
    public void TagsAndFieldWeights_AreSearchable()
    {
        try
        {
            Execute("UPDATE products.product SET text = 'weightprobe', description = NULL, tags_json = '[\"tagprobe\"]'::jsonb WHERE sku = 'MUCOFF-NANO-CLEANER'");
            Execute("UPDATE products.product SET description = 'weightprobe' WHERE sku = 'YETI-ASR-C2-2025'");
            Rebuild();
            Search("weightprobe").First().Sku.Should().Be("MUCOFF-NANO-CLEANER");
            Search("tagprobe").Should().ContainSingle().Which.Sku.Should().Be("MUCOFF-NANO-CLEANER");
            Execute("UPDATE products.product SET sku = 'WEIGHTPROBE', text = 'opaque', description = NULL, tags_json = NULL WHERE sku = 'MUCOFF-NANO-CLEANER'");
            Execute("UPDATE products.product SET text = 'weightprobe weightprobe weightprobe weightprobe', description = 'weightprobe' WHERE sku = 'YETI-ASR-C2-2025'");
            Rebuild();
            Test.Scoped(test => test.Run(async _ =>
            {
                var matches = await test.Services.GetRequiredService<ProductLexicalSearchIndex>().Matches("weightprobe").ToListAsync().ConfigureAwait(false);
                matches.Single(m => m.ExactSku).Rank.Should().BeLessThan(matches.Single(m => !m.ExactSku).Rank);
            }).AssertSuccess());
            Search("weightprobe").First().Sku.Should().Be("WEIGHTPROBE");
        }
        finally
        {
            Test.MigratePostgresDataAsync<TestData>(["read-data.seed.yaml"], DbMigration.ConfigureMigrationArgs).GetAwaiter().GetResult();
            Rebuild();
        }
    }

    [Test]
    public void EmptyBackfill_IsReadyAndCountIsZero()
    {
        try
        {
            Execute("UPDATE products.product SET is_deleted = true");
            Rebuild();
            var result = Test.Http<ProductLite[]>().Run(HttpMethod.Get, "/api/products?$search=bike&$count=true").AssertOK();
            result.Value.Should().BeEmpty();
            result.Response.Headers.Should().ContainKey("X-Paging-Total-Count").WhoseValue.Should().ContainSingle().Which.Should().Be("0");
        }
        finally
        {
            Test.MigratePostgresDataAsync<TestData>(["read-data.seed.yaml"], DbMigration.ConfigureMigrationArgs).GetAwaiter().GetResult();
            Rebuild();
        }
    }

    [TestCase(IsolationLevel.RepeatableRead)]
    [TestCase(IsolationLevel.Serializable)]
    public void CompatibleParent_PreservesSnapshot(IsolationLevel isolation) => Test.Scoped(test => test.Run(async _ =>
    {
        var uow = test.Services.GetRequiredService<PostgresUnitOfWork>();
        await uow.TransactionAsync(new PostgresDatabaseArgs { IsolationLevel = isolation }, async token =>
        {
            var parent = uow.Database.CurrentTransaction;
            var page = await test.Services.GetRequiredService<IProductQueryRepository>().QueryAsync(
                new QueryArgs { Search = "derailleur" }, PagingArgs.CreateWithCount(take: 1), token).ConfigureAwait(false);
            page.Items.Should().ContainSingle();
            page.Paging!.TotalCount.Should().Be(4);
            uow.Database.CurrentTransaction.Should().BeSameAs(parent);
        }).ConfigureAwait(false);
    }).AssertSuccess());

    [Test]
    public void CancellationAndParentTransaction_AreExplicit() => Test.Scoped(test => test.Run(async _ =>
    {
        var index = test.Services.GetRequiredService<ProductLexicalSearchIndex>();
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        Func<Task> rebuild = () => index.RebuildAsync(canceled.Token);
        await rebuild.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
        Func<Task> query = () => test.Services.GetRequiredService<IProductQueryRepository>().QueryAsync(new QueryArgs { Search = "cleaner" }, null, canceled.Token);
        await query.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
        await test.Services.GetRequiredService<PostgresUnitOfWork>().TransactionAsync(async token =>
        {
            Func<Task> sync = () => index.SynchronizeAsync(29.ToGuid().ToString(), token);
            await sync.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(false);
            Func<Task> search = () => test.Services.GetRequiredService<IProductQueryRepository>().QueryAsync(new QueryArgs { Search = "cleaner" }, null, token);
            await search.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(false);
        }).ConfigureAwait(false);
    }).AssertSuccess());

    private ProductLite[] Search(string query, string suffix = "")
        => Test.Http<ProductLite[]>().Run(HttpMethod.Get, $"/api/products?$search={Uri.EscapeDataString(query)}&$take=50{suffix}").AssertOK().Value!;

    private void SearchUnavailable() => Test.Http().Run(HttpMethod.Get, "/api/products?$search=cleaner").Assert(HttpStatusCode.ServiceUnavailable);

    private void Rebuild() => Test.Scoped(test => test.Run(async _ =>
        await test.Services.GetRequiredService<ProductLexicalSearchIndex>().RebuildAsync().ConfigureAwait(false)).AssertSuccess());

    private void Synchronize() => Test.Scoped(test => test.Run(async _ =>
        (await test.Services.GetRequiredService<IProductSearchSyncAdapter>().SynchronizeAsync(29.ToGuid().ToString()).ConfigureAwait(false)).ThrowOnError()).AssertSuccess());

    private void Execute(string sql) => Test.Scoped(test => test.Run(async _ =>
        await test.Services.GetRequiredService<ProductsDbContext>().Database.ExecuteSqlRawAsync(sql).ConfigureAwait(false)).AssertSuccess());
}
