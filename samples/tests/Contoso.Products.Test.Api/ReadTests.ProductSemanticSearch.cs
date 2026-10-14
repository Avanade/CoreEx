namespace Contoso.Products.Test.Api;

public partial class ReadTests
{
    [TestCase(IsolationLevel.RepeatableRead)]
    [TestCase(IsolationLevel.Serializable)]
    public void Product_Search_EnlistsInCompatibleParent(IsolationLevel isolation) => Test.Scoped(test => test.Run(async _ =>
    {
        var ef = test.Services.GetRequiredService<ProductsEfDb>();
        var uow = test.Services.GetRequiredService<PostgresUnitOfWork>();
        var repository = test.Services.GetRequiredService<IProductRepository>();
        await uow.TransactionAsync(new PostgresDatabaseArgs { IsolationLevel = isolation }, async token =>
        {
            var parent = uow.Database.CurrentTransaction;
            var result = await repository.QueryAsync(new QueryArgs { Search = "changing gears" }, PagingArgs.CreateWithCount(take: 1), token).ConfigureAwait(false);
            result.Items.Should().ContainSingle().Which.Sku.Should().Be("SHIMANO-XT-M8100-RD");
            result.Paging!.TotalCount.Should().Be(29);
            uow.Database.CurrentTransaction.Should().BeSameAs(parent);
            ef.DbContext.Database.CurrentTransaction.Should().NotBeNull();
            uow.Events.IsEmpty.Should().BeTrue();
        }).ConfigureAwait(false);
        uow.Database.CurrentTransaction.Should().BeNull();
        ef.DbContext.Database.CurrentTransaction.Should().BeNull();
    }).AssertSuccess());

    [Test]
    public void Product_Search_RejectsWeakParentWithoutInference() => Test.Scoped(test => test.Run(async _ =>
    {
        var ef = test.Services.GetRequiredService<ProductsEfDb>();
        var uow = test.Services.GetRequiredService<PostgresUnitOfWork>();
        var repository = test.Services.GetRequiredService<IProductRepository>();
        await uow.TransactionAsync(async token =>
        {
            var parent = uow.Database.CurrentTransaction;
            var queries = _embeddings.QueryCallCount;
            Func<Task> search = () => repository.QueryAsync(new QueryArgs { Search = "cleaner" }, null, token);
            await search.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Semantic product search requires a RepeatableRead or Serializable parent transaction.").ConfigureAwait(false);
            _embeddings.QueryCallCount.Should().Be(queries);
            uow.Database.CurrentTransaction.Should().BeSameAs(parent);
            (await repository.QueryAsync(null, PagingArgs.Create(take: 1), token).ConfigureAwait(false)).Items.Should().ContainSingle();
        }).ConfigureAwait(false);
        uow.Database.CurrentTransaction.Should().BeNull();
        ef.DbContext.Database.CurrentTransaction.Should().BeNull();
    }).AssertSuccess());

    [Test]
    public void Product_Search_ExactDistancePagingAndCount()
    {
        var result = Test.Http<ProductLite[]>()
            .Run(HttpMethod.Get, "/api/products?$search=a component for changing gears&$skip=1&$take=2&$count=true")
            .AssertOK();

        result.Value!.Select(p => p.Id).Should().Equal(22.ToGuid().ToString(), 23.ToGuid().ToString());
        result.Response.Headers.Should().ContainKey("X-Paging-Total-Count").WhoseValue.Should().ContainSingle().Which.Should().Be("29");
    }

    [Test]
    public void Product_Search_FiltersBeforeTake()
    {
        var result = Test.Http<ProductLite[]>()
            .Run(HttpMethod.Get, "/api/products?$search=a component for changing gears&$filter=brand eq 'SRAM' and price lt 160&$take=1&$count=true")
            .AssertOK();

        result.Value.Should().ContainSingle().Which.Sku.Should().Be("SRAM-GX-EAGLE-RD");
        result.Response.Headers.Should().ContainKey("X-Paging-Total-Count").WhoseValue.Should().ContainSingle().Which.Should().Be("1");
    }

    [Test]
    public void Product_Search_InactiveAndDeletedEligibility()
    {
        var result = Test.Http<ProductLite[]>()
            .Run(HttpMethod.Get, "/api/products?$search=mountain bike&$inactive&$take=50")
            .AssertOK()
            .Value!;

        result.Should().HaveCount(31).And.Contain(p => p.Sku == "SPEC-EPIC-8-PRO");
        result.Should().NotContain(p => p.Id == 18.ToGuid().ToString());
    }

    [Test]
    public void Product_Search_NoEligibleProducts()
    {
        Test.Http<ProductLite[]>()
            .Run(HttpMethod.Get, "/api/products?$search=clean mud off my bike&$filter=price lt 0")
            .AssertOK()
            .Value.Should().BeEmpty();
    }

    [Test]
    public void Product_Search_GraphQLMatchesRest()
    {
        var graphql = Test.Http<JsonElement>()
            .Run(HttpMethod.Post, "/api/query", new { query = "{ products(search: \"a component for changing gears\", where: {price: {lt: 130}}, first: 2) { edges { node { id sku } } } }" })
            .AssertOK()
            .Value;

        graphql.TryGetProperty("errors", out _).Should().BeFalse();
        var rest = Test.Http<ProductLite[]>()
            .Run(HttpMethod.Get, "/api/products?$search=a component for changing gears&$filter=price lt 130&$take=2")
            .AssertOK()
            .Value!;
        graphql.GetProperty("data").GetProperty("products").GetProperty("edges").EnumerateArray()
            .Select(edge => edge.GetProperty("node").GetProperty("id").GetString()).Should().Equal(rest.Select(p => p.Id));
    }

    [TestCase("DELETE FROM products.product_search WHERE profile = 'tests:controlled-vectors:768'")]
    [TestCase("UPDATE products.product_search SET profile = 'incompatible' WHERE profile = 'tests:controlled-vectors:768'")]
    [TestCase("UPDATE products.product_search SET recipe = 'old-recipe' WHERE profile = 'tests:controlled-vectors:768'")]
    [TestCase("UPDATE products.product_search SET source_hash = repeat('0', 64) WHERE profile = 'tests:controlled-vectors:768'")]
    public void Product_Search_InvalidProjectionIsUnavailable(string sql)
    {
        try
        {
            ExecuteSearchSql(sql);
            var queries = _embeddings.QueryCallCount;
            Test.Http()
                .Run(HttpMethod.Get, "/api/products?$search=cleaner")
                .Assert(HttpStatusCode.ServiceUnavailable);
            _embeddings.QueryCallCount.Should().Be(queries);
            Test.Http<ProductLite[]>()
                .Run(HttpMethod.Get, "/api/products?$take=1")
                .AssertOK()
                .Value.Should().ContainSingle();
        }
        finally
        {
            ExecuteSearchSql("DELETE FROM products.product_search WHERE profile = 'incompatible' OR recipe = 'old-recipe'");
            RebuildSearch();
        }
    }

    [TestCase("zero")]
    [TestCase("nonFinite")]
    [TestCase("wrongDimensions")]
    public void Product_Search_InvalidVectorIsNotAValidResult(string shape)
    {
        _embeddings.QueryVector = new float[shape == "wrongDimensions" ? 767 : 768];
        if (shape != "zero")
            _embeddings.QueryVector[0] = shape == "nonFinite" ? float.NaN : 1;
        try
        {
            Test.Http().Run(HttpMethod.Get, "/api/products?$search=cleaner").Assert(HttpStatusCode.InternalServerError);
        }
        finally
        {
            _embeddings.QueryVector = null;
        }
    }

    [Test]
    public void Product_Search_QueryEmbeddingFailureHasNoFallback()
    {
        _embeddings.FailQuery = true;
        try
        {
            Test.Http().Run(HttpMethod.Get, "/api/products?$search=cleaner").Assert(HttpStatusCode.InternalServerError);
        }
        finally
        {
            _embeddings.FailQuery = false;
        }
    }

    [Test]
    public void Product_Search_DescriptionEditRequiresRebuild()
    {
        var product = Test.Http<Product>().Run(HttpMethod.Get, $"/api/products/{29.ToGuid()}").AssertOK().Value!;
        try
        {
            ExecuteSearchSql("UPDATE products.product SET description = 'Changed searchable meaning.' WHERE sku = 'MUCOFF-NANO-CLEANER'");
            Test.Http().Run(HttpMethod.Get, "/api/products?$search=cleaner").Assert(HttpStatusCode.ServiceUnavailable);
        }
        finally
        {
            Test.Scoped(test => test.Run(async _ =>
                await test.Services.GetRequiredService<ProductsDbContext>().Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE products.product SET description = {product.Description} WHERE sku = 'MUCOFF-NANO-CLEANER'").ConfigureAwait(false)).AssertSuccess());
            RebuildSearch();
        }
    }

    [Test]
    public void Product_Search_PriceChangeIsLiveWithoutReembedding()
    {
        try
        {
            ExecuteSearchSql("UPDATE products.product SET price = 99 WHERE sku = 'MUCOFF-NANO-CLEANER'");
            Test.Http<ProductLite[]>()
                .Run(HttpMethod.Get, "/api/products?$search=cleaner&$filter=sku eq 'MUCOFF-NANO-CLEANER'")
                .AssertOK().Value.Should().ContainSingle().Which.Price.Should().Be(99);
            Test.Http<ProductLite[]>()
                .Run(HttpMethod.Get, "/api/products?$search=cleaner&$filter=sku eq 'MUCOFF-NANO-CLEANER' and price lt 20")
                .AssertOK().Value.Should().BeEmpty();
        }
        finally
        {
            ExecuteSearchSql("UPDATE products.product SET price = 16.99 WHERE sku = 'MUCOFF-NANO-CLEANER'");
        }
    }

    [Test]
    public void Product_Search_FailedRebuildPreservesPublishedIndex()
    {
        _embeddings.DocumentCallCount = 0;
        _embeddings.FailDocumentOnCall = 2;
        try
        {
            Test.Scoped(test => test.Run(async _ =>
                await test.Services.GetRequiredService<ProductSearchIndex>().RebuildAsync().ConfigureAwait(false)).AssertException<HttpRequestException>());
            Test.Http<ProductLite[]>().Run(HttpMethod.Get, "/api/products?$search=changing gears&$take=3").AssertOK()
                .Value.Should().HaveCount(3).And.OnlyContain(p => p.Text!.Contains("Rear Derailleur"));
        }
        finally
        {
            _embeddings.FailDocumentOnCall = null;
        }
    }

    [Test]
    public void Product_Search_ConcurrentCatalogEditRejectsRebuild()
    {
        var product = Test.Http<Product>().Run(HttpMethod.Get, $"/api/products/{29.ToGuid()}").AssertOK().Value!;
        try
        {
            Test.Scoped(test => test.Run(async _ =>
            {
                _embeddings.BeforeDocument = async ct =>
                {
                    _embeddings.BeforeDocument = null;
                    await test.Services.GetRequiredService<ProductsDbContext>().Database.ExecuteSqlRawAsync(
                        "UPDATE products.product SET description = 'Concurrent edit during embedding.' WHERE sku = 'MUCOFF-NANO-CLEANER'", ct).ConfigureAwait(false);
                };
                await test.Services.GetRequiredService<ProductSearchIndex>().RebuildAsync().ConfigureAwait(false);
            }).AssertException<ConcurrencyException>());
        }
        finally
        {
            _embeddings.BeforeDocument = null;
            Test.Scoped(test => test.Run(async _ =>
                await test.Services.GetRequiredService<ProductsDbContext>().Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE products.product SET description = {product.Description} WHERE sku = 'MUCOFF-NANO-CLEANER'").ConfigureAwait(false)).AssertSuccess());
        }
        Test.Http<ProductLite[]>().Run(HttpMethod.Get, "/api/products?$search=cleaner&$take=1").AssertOK().Value.Should().ContainSingle();
    }

    private void ExecuteSearchSql(string sql) => Test.Scoped(test => test.Run(async _ =>
        await test.Services.GetRequiredService<ProductsDbContext>().Database.ExecuteSqlRawAsync(sql).ConfigureAwait(false)).AssertSuccess());
}
