namespace Contoso.Products.Test.Api;

[NonParallelizable]
public class ProductHybridSearchTests : WithApiTester<Contoso.Products.Api.Program>
{
    private readonly TestEmbeddingAdapter _embeddings = new();

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        Test.UseAdditionalConfiguration("Products:Search:Mode", "Hybrid");
        Test.ReplaceScoped<IProductEmbeddingAdapter>(_ => _embeddings);
        await Test.MigratePostgresDataAsync<TestData>(["read-data.seed.yaml"], DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
        await Test.ClearFusionCacheAsync().ConfigureAwait(false);
        RebuildSearch();
    }

    [Test]
    public void ReciprocalRankFusion_UsesOneBasedRanksAndRewardsSharedCandidates()
    {
        var fused = ProductSearchFusion.Fuse(
            [new("shared", false), new("lexical-only", false)],
            ["shared", "semantic-only"]);

        fused[0].Id.Should().Be("shared");
        fused[0].LexicalRank.Should().Be(1);
        fused[0].SemanticRank.Should().Be(1);
        fused[0].Score.Should().Be(2d / 61);
        fused.Single(candidate => candidate.Id == "lexical-only").Score.Should().Be(1d / 62);
        fused.Single(candidate => candidate.Id == "semantic-only").Score.Should().Be(1d / 62);
        fused.Select(candidate => candidate.Id).Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void ReciprocalRankFusion_PrioritizesExactSkuAndKeepsBoundedBranchCandidates()
    {
        var lexical = Enumerable.Range(0, ProductSearchFusion.CandidateLimit)
            .Select(i => new ProductSearchFusion.LexicalCandidate($"L{i:D3}", i == 0))
            .ToArray();
        var semantic = Enumerable.Range(0, ProductSearchFusion.CandidateLimit)
            .Select(i => $"S{i:D3}")
            .ToArray();
        var fused = ProductSearchFusion.Fuse(lexical, semantic);

        fused.Should().HaveCount(ProductSearchFusion.MaximumCandidateCount);
        fused[0].Id.Should().Be("L000");
        fused[0].ExactSku.Should().BeTrue();
        fused.Select(candidate => candidate.Id).Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void ReciprocalRankFusion_HandlesEmptyAndDuplicateBranchCandidates()
    {
        ProductSearchFusion.Fuse([], []).Should().BeEmpty();
        var fused = ProductSearchFusion.Fuse(
            [new("same", false), new("same", false)],
            ["same", "same", "other"]);

        fused.Should().HaveCount(2);
        fused[0].Id.Should().Be("same");
        fused[0].LexicalRank.Should().Be(1);
        fused[0].SemanticRank.Should().Be(1);
        ProductSearchFusion.Fuse(
            [new("same", false), new("same", false)],
            ["same", "same", "other"]).Select(candidate => candidate.Id)
            .Should().Equal(fused.Select(candidate => candidate.Id));
    }

    [Test]
    public void Product_HybridSearch_ExactSkuPriorityFiltersAndPagingAreConsistent()
    {
        var exact = Search("SHIMANO-XT-M8100-RD", "&$take=5&$count=true");
        exact.Should().NotBeEmpty();
        exact[0].Sku.Should().Be("SHIMANO-XT-M8100-RD");
        exact.Select(product => product.Id).Should().OnlyHaveUniqueItems();

        Search("SHIMANO-XT-M8100-RD", "&$filter=price lt 0&$count=true").Should().BeEmpty();
        Search("derailleur", "&$filter=brand eq 'SRAM' and price lt 160&$count=true")
            .Should().ContainSingle().Which.Sku.Should().Be("SRAM-GX-EAGLE-RD");

        Test.Scoped(test => test.Run(async _ =>
        {
            var repository = test.Services.GetRequiredService<IProductQueryRepository>();
            var query = new QueryArgs { Search = "a component for changing gears" };
            var all = await repository.QueryAsync(query, PagingArgs.CreateWithCount(take: ProductSearchFusion.MaximumCandidateCount)).ConfigureAwait(false);
            var firstPage = await repository.QueryAsync(query, PagingArgs.CreateWithCount(take: 3)).ConfigureAwait(false);
            var secondPage = await repository.QueryAsync(query, PagingArgs.Create(skip: 3, take: 3)).ConfigureAwait(false);

            all.Paging!.TotalCount.Should().Be(firstPage.Paging!.TotalCount);
            all.Paging.TotalCount.Should().BeLessThanOrEqualTo(ProductSearchFusion.MaximumCandidateCount);
            var allItems = all.Items!.ToArray();
            firstPage.Items!.Select(product => product.Id).Should().Equal(allItems.Take(3).Select(product => product.Id));
            secondPage.Items!.Select(product => product.Id).Should().Equal(allItems.Skip(3).Take(3).Select(product => product.Id));
        }).AssertSuccess());
    }

    [Test]
    public void Product_HybridSearch_EligibilityAndStructuredFiltersApplyBeforeCandidateLimit()
        => Test.Scoped(test => test.Run(async _ =>
        {
            var ef = test.Services.GetRequiredService<ProductsEfDb>();
            var unfiltered = await ProductSearchFusion.TakeCandidatesAsync(
                ef.Products.Model.Query().OrderBy(product => product.Id).Select(product => product.Id),
                2,
                CancellationToken.None).ConfigureAwait(false);
            unfiltered.Should().HaveCount(2);

            var filtered = await ProductSearchFusion.TakeCandidatesAsync(
                ef.Products.Model.Query()
                    .Where(product => !product.IsInactive && product.BrandCode == "SRAM" && product.Price < 160)
                    .OrderBy(product => product.Id)
                    .Select(product => product.Sku),
                2,
                CancellationToken.None).ConfigureAwait(false);
            filtered.Should().ContainSingle().Which.Should().Be("SRAM-GX-EAGLE-RD");
        }).AssertSuccess());

    [Test]
    public void Product_HybridSearch_InactiveAndDeletedEligibilityIsLive()
    {
        Search("SPEC-EPIC-8-PRO").Should().NotContain(product => product.Sku == "SPEC-EPIC-8-PRO");
        Search("SPEC-EPIC-8-PRO", "&$inactive").Should().Contain(product => product.Sku == "SPEC-EPIC-8-PRO");
        try
        {
            ExecuteSearchSql("UPDATE products.product SET is_deleted = true WHERE sku = 'MUCOFF-NANO-CLEANER'");
            Search("MUCOFF-NANO-CLEANER", "&$filter=sku eq 'MUCOFF-NANO-CLEANER'").Should().BeEmpty();
        }
        finally
        {
            ExecuteSearchSql("UPDATE products.product SET is_deleted = false WHERE sku = 'MUCOFF-NANO-CLEANER'");
        }
    }

    [Test]
    public void Product_HybridSearch_RequiresBothReadyBranchesWithoutFallback()
    {
        try
        {
            ExecuteSearchSql("DELETE FROM products.product_lexical_search_readiness");
            Test.Http().Run(HttpMethod.Get, "/api/products?$search=cleaner").Assert(HttpStatusCode.ServiceUnavailable);

            RebuildSearch();
            ExecuteSearchSql("DELETE FROM products.product_search_readiness WHERE profile = 'tests:controlled-vectors:768'");
            Test.Http().Run(HttpMethod.Get, "/api/products?$search=cleaner").Assert(HttpStatusCode.ServiceUnavailable);
        }
        finally
        {
            RebuildSearch();
        }
    }

    [Test]
    public void Product_HybridSearch_EmbeddingFailureDoesNotReturnLexicalResults()
    {
        _embeddings.FailQuery = true;
        try
        {
            Test.Http().Run(HttpMethod.Get, "/api/products?$search=cleaner").Assert(HttpStatusCode.ServiceUnavailable);
            _embeddings.QueryCallCount.Should().BeGreaterThan(0);
        }
        finally
        {
            _embeddings.FailQuery = false;
        }
    }

    [Test]
    public void Product_HybridSearch_CancellationDuringEmbeddingPropagates()
        => Test.Scoped(test => test.Run(async _ =>
        {
            var queryStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _embeddings.BeforeQuery = async cancellationToken =>
            {
                queryStarted.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            };

            try
            {
                using var cancellation = new CancellationTokenSource();
                var repository = test.Services.GetRequiredService<IProductQueryRepository>();
                var pending = repository.QueryAsync(new QueryArgs { Search = "cleaner" }, null, cancellation.Token);
                await queryStarted.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                cancellation.Cancel();
                Func<Task> wait = async () => await pending.ConfigureAwait(false);
                await wait.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
            }
            finally
            {
                _embeddings.BeforeQuery = null;
            }
        }).AssertSuccess());

    [Test]
    public void Product_HybridSearch_EnlistsCompatibleParentAndRejectsWeakParent()
    {
        Test.Scoped(test => test.Run(async _ =>
        {
            var unitOfWork = test.Services.GetRequiredService<PostgresUnitOfWork>();
            var repository = test.Services.GetRequiredService<IProductQueryRepository>();
            await unitOfWork.TransactionAsync(new PostgresDatabaseArgs { IsolationLevel = IsolationLevel.RepeatableRead }, async token =>
            {
                var parent = unitOfWork.Database.CurrentTransaction;
                var result = await repository.QueryAsync(new QueryArgs { Search = "changing gears" }, PagingArgs.CreateWithCount(take: 1), token).ConfigureAwait(false);
                result.Items.Should().ContainSingle();
                result.Paging!.TotalCount.Should().BeGreaterThan(0);
                unitOfWork.Database.CurrentTransaction.Should().BeSameAs(parent);
            }).ConfigureAwait(false);

            await unitOfWork.TransactionAsync(async token =>
            {
                var calls = _embeddings.QueryCallCount;
                Func<Task> search = () => repository.QueryAsync(new QueryArgs { Search = "cleaner" }, null, token);
                await search.Should().ThrowAsync<InvalidOperationException>()
                    .WithMessage("Hybrid product search requires a RepeatableRead or Serializable parent transaction.")
                    .ConfigureAwait(false);
                _embeddings.QueryCallCount.Should().Be(calls);
            }).ConfigureAwait(false);
        }).AssertSuccess());
    }

    [Test]
    public void Product_HybridSearch_SchemaGraphQLAndOrdinaryQueryDescribeSelectedBehavior()
    {
        var schema = Test.Http<JsonElement>().Run(HttpMethod.Get, "/api/products/$query").AssertOK().Value;
        schema.GetProperty("search").GetProperty("mode").GetString().Should().Be("hybrid");
        schema.GetProperty("search").GetProperty("description").GetString().Should().Contain("100");
        schema.GetProperty("search").GetProperty("description").GetString().Should().Contain("deduplicated candidate union");

        var graphql = Test.Http<JsonElement>()
            .Run(HttpMethod.Post, "/api/query", new { query = "{ products(search: \"changing gears\", first: 1) { edges { node { sku } } } }" })
            .AssertOK().Value;
        graphql.TryGetProperty("errors", out _).Should().BeFalse();
        graphql.GetProperty("data").GetProperty("products").GetProperty("edges").GetArrayLength().Should().Be(1);

        Test.Http<ProductLite[]>()
            .Run(HttpMethod.Get, "/api/products?$search=%20%20&$take=50")
            .AssertOK().Value.Should().HaveCount(29);
    }

    private ProductLite[] Search(string query, string suffix = "")
        => Test.Http<ProductLite[]>()
            .Run(HttpMethod.Get, $"/api/products?$search={Uri.EscapeDataString(query)}&$take=50{suffix}")
            .AssertOK().Value!;

    private void RebuildSearch() => Test.Scoped(test => test.Run(async _ =>
    {
        var count = await test.Services.GetRequiredService<ProductSearchIndex>().RebuildAsync().ConfigureAwait(false);
        count.Should().Be(31);
    }).AssertSuccess());

    private void ExecuteSearchSql(string sql) => Test.Scoped(test => test.Run(async _ =>
        await test.Services.GetRequiredService<ProductsDbContext>().Database.ExecuteSqlRawAsync(sql).ConfigureAwait(false)).AssertSuccess());
}
