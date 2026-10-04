namespace Contoso.Products.Test.Api;

public class RefDataMutateTests : WithApiTester<Contoso.Products.Api.Program>
{
    private const string _brandsUrl = "/api/refdata/brands";

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        await Test.MigratePostgresDataAsync<TestData>(["mutate-data.seed.yaml"], DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
        await Test.ClearFusionCacheAsync().ConfigureAwait(false);

        Test.UseExpectedPostgresOutboxPublisher();
    }

    /// <summary>
    /// Creates a new (inactive) brand with the specified <paramref name="code"/>.
    /// </summary>
    private Brand CreateBrand(string code)
        => Test.Http<Brand>()
            .ExpectPostgresOutboxEvents()
            .Run(HttpMethod.Post, _brandsUrl, new Brand { Code = code, Text = $"Brand {code}" })
            .AssertCreated()
            .Value!;

    /// <summary>
    /// Creates a new brand with the specified <paramref name="code"/> and activates it.
    /// </summary>
    private Brand CreateActiveBrand(string code)
    {
        var b = CreateBrand(code);
        return Test.Http<Brand>()
            .ExpectPostgresOutboxEvents()
            .Run(HttpMethod.Post, $"{_brandsUrl}/{b.Id}/activate")
            .AssertOK()
            .Value!;
    }

    private Brand[] GetBrands(bool includeInactive = false)
        => Test.Http<Brand[]>()
            .Run(HttpMethod.Get, includeInactive ? $"{_brandsUrl}?$inactive=true" : _brandsUrl)
            .AssertOK()
            .Value!;

    #region Get

    [Test]
    public void Get_NotFound()
    {
        Test.Http()
            .Run(HttpMethod.Get, $"{_brandsUrl}/404")
            .AssertNotFound();
    }

    [Test]
    public void Get_Success()
    {
        // Arrange - the seeded identifier is not assumed; get via the list.
        var b = GetBrands().Single(x => x.Code == "YETI");

        // Act/Assert.
        Test.Http<Brand>()
            .Run(HttpMethod.Get, $"{_brandsUrl}/{b.Id}")
            .AssertOK()
            .AssertValue(b);
    }

    #endregion

    #region Create

    [Test]
    public void Create_Empty()
    {
        Test.Http()
            .Run(HttpMethod.Post, _brandsUrl, new Brand())
            .AssertBadRequest()
            .AssertErrors(
                "Code is required.",
                "Text is required."
            );
    }

    [Test]
    public void Create_Bad_Data()
    {
        var b = new Brand
        {
            Code = new string('X', CoreEx.Validation.ReferenceDataValidator<Brand>.CodeMaximumLength + 1),
            Text = new string('X', CoreEx.Validation.ReferenceDataValidator<Brand>.TextMaximumLength + 1),
            Description = new string('X', CoreEx.Validation.ReferenceDataValidator<Brand>.DescriptionMaximumLength + 1),
        };

        Test.Http()
            .Run(HttpMethod.Post, _brandsUrl, b)
            .AssertBadRequest()
            .AssertErrors(
                $"Code must not exceed {CoreEx.Validation.ReferenceDataValidator<Brand>.CodeMaximumLength} character(s) in length.",
                $"Text must not exceed {CoreEx.Validation.ReferenceDataValidator<Brand>.TextMaximumLength} character(s) in length.",
                $"Description must not exceed {CoreEx.Validation.ReferenceDataValidator<Brand>.DescriptionMaximumLength} character(s) in length."
            );
    }

    [Test]
    public void Create_Duplicate()
    {
        Test.Http()
            .Run(HttpMethod.Post, _brandsUrl, new Brand { Code = "YETI", Text = "Yeti Again" })
            .AssertConflict();
    }

    [Test]
    public void Create_Success()
    {
        // Act/Assert - a newly created reference data value is always inactive until explicitly activated.
        var b = Test.Http<Brand>()
            .ExpectIdentifier()
            .ExpectETag()
            .ExpectPostgresOutboxEvents(e => e.AssertWithValue("contoso", "contoso.products.brand.created.v1"))
            .Run(HttpMethod.Post, _brandsUrl, new Brand { Code = "CREATE-OK", Text = "Create OK", SortOrder = 5 })
            .AssertCreated()
            .AssertLocationHeader(r => new Uri($"{_brandsUrl}/{r!.Id}", UriKind.Relative))
            .Value!;

        b.Code.Should().Be("CREATE-OK");
        b.Text.Should().Be("Create OK");
        b.SortOrder.Should().Be(5);
        b.IsInactive.Should().BeTrue();

        // Assert - get by identifier.
        Test.Http<Brand>()
            .Run(HttpMethod.Get, $"{_brandsUrl}/{b.Id}")
            .AssertOK()
            .AssertValue(b);

        // Assert - not within the (default) active list, but is within the inactive-inclusive list.
        GetBrands().Should().NotContain(x => x.Code == "CREATE-OK");
        GetBrands(includeInactive: true).Should().Contain(x => x.Code == "CREATE-OK");
    }

    [Test]
    public void Create_IdempotencyKey()
    {
        var b = new Brand { Code = "CREATE-IDEM", Text = "Create Idempotent" };
        var ik = Guid.NewGuid().ToString();

        // Act/Assert - first request creates and emits an event.
        var v1 = Test.Http<Brand>()
            .ExpectPostgresOutboxEvents()
            .Run(HttpMethod.Post, _brandsUrl, b, requestModifier: r => r.WithIdempotencyKey(ik))
            .AssertCreated()
            .Value!;

        // Assert - repeat with the same idempotency key; same result and no further event emitted.
        var v2 = Test.Http<Brand>()
            .ExpectNoPostgresOutboxEvents()
            .Run(HttpMethod.Post, _brandsUrl, b, requestModifier: r => r.WithIdempotencyKey(ik))
            .AssertCreated()
            .Value!;

        ObjectComparer.Assert(v1, v2);
    }

    #endregion

    #region Patch

    [Test]
    public void Patch_NotFound()
    {
        Test.Http()
            .Run(HttpMethod.Patch, $"{_brandsUrl}/404", new { text = "abc" }, requestModifier: r => r.WithMergePatchJsonContentType())
            .AssertNotFound();
    }

    [Test]
    public void Patch_Concurrency()
    {
        var b = CreateBrand("PATCH-CONC");

        Test.Http()
            .Run(HttpMethod.Patch, $"{_brandsUrl}/{b.Id}", new { text = "Changed" }, requestModifier: r => r.WithIfMatch("AAAAAAAA").WithMergePatchJsonContentType())
            .AssertPreconditionFailed();
    }

    [Test]
    public void Patch_Validation()
    {
        var b = CreateBrand("PATCH-VAL");

        Test.Http()
            .Run(HttpMethod.Patch, $"{_brandsUrl}/{b.Id}", new { text = string.Empty }, requestModifier: r => r.WithIfMatch(b.ETag).WithMergePatchJsonContentType())
            .AssertBadRequest()
            .AssertErrors("Text is required.");
    }

    [Test]
    public void Patch_NotSupported()
    {
        var b = CreateBrand("PATCH-NS");

        Test.Http()
            .Run(HttpMethod.Patch, $"{_brandsUrl}/{b.Id}", new { startsOn = "2024-01-01" }, requestModifier: r => r.WithIfMatch(b.ETag).WithMergePatchJsonContentType())
            .AssertBadRequest()
            .AssertProblemDetails(pd => pd.Title.Should().Be("Starts on is not currently supported and as such cannot be set."));
    }

    [Test]
    public void Patch_Success()
    {
        var b = CreateBrand("PATCH-OK");

        // Act/Assert.
        var u = Test.Http<Brand>()
            .ExpectPostgresOutboxEvents(e => e.AssertWithValue("contoso", "contoso.products.brand.updated.v1"))
            .Run(HttpMethod.Patch, $"{_brandsUrl}/{b.Id}", new { text = "Patched Text", sortOrder = 9 }, requestModifier: r => r.WithIfMatch(b.ETag).WithMergePatchJsonContentType())
            .AssertOK()
            .Value!;

        u.Id.Should().Be(b.Id);
        u.Code.Should().Be(b.Code);
        u.Text.Should().Be("Patched Text");
        u.SortOrder.Should().Be(9);
        u.IsInactive.Should().BeTrue();
        u.ETag.Should().NotBe(b.ETag);

        // Assert.
        Test.Http<Brand>()
            .Run(HttpMethod.Get, $"{_brandsUrl}/{b.Id}")
            .AssertOK()
            .AssertValue(u);
    }

    [Test]
    public void Patch_Code_Immutable()
    {
        var b = CreateBrand("PATCH-CODE");

        // Act/Assert - the code is silently retained; only the text is updated.
        var u = Test.Http<Brand>()
            .ExpectPostgresOutboxEvents()
            .Run(HttpMethod.Patch, $"{_brandsUrl}/{b.Id}", new { code = "PATCH-CODE-CHANGED", text = "Code Retained" }, requestModifier: r => r.WithIfMatch(b.ETag).WithMergePatchJsonContentType())
            .AssertOK()
            .Value!;

        u.Code.Should().Be("PATCH-CODE");
        u.Text.Should().Be("Code Retained");
    }

    [Test]
    public void Patch_IsInactive_Ignored()
    {
        // The active state can only be changed via activate/deactivate.
        var b = CreateBrand("PATCH-ACTIVE");

        var u = Test.Http<Brand>()
            .ExpectPostgresOutboxEvents()
            .Run(HttpMethod.Patch, $"{_brandsUrl}/{b.Id}", new { isInactive = false, text = "Still Inactive" }, requestModifier: r => r.WithIfMatch(b.ETag).WithMergePatchJsonContentType())
            .AssertOK()
            .Value!;

        u.IsInactive.Should().BeTrue();
        u.Text.Should().Be("Still Inactive");
    }

    [Test]
    public void Patch_NoChanges()
    {
        var b = CreateBrand("PATCH-NONE");

        // Act/Assert - nothing changed so no event and the etag remains.
        var u = Test.Http<Brand>()
            .ExpectNoPostgresOutboxEvents()
            .Run(HttpMethod.Patch, $"{_brandsUrl}/{b.Id}", new { }, requestModifier: r => r.WithIfMatch(b.ETag).WithMergePatchJsonContentType())
            .AssertOK()
            .Value!;

        u.ETag.Should().Be(b.ETag);
        u.Text.Should().Be(b.Text);
    }

    #endregion

    #region Activate

    [Test]
    public void Activate_NotFound()
    {
        Test.Http()
            .Run(HttpMethod.Post, $"{_brandsUrl}/404/activate")
            .AssertNotFound();
    }

    [Test]
    public void Activate_Success()
    {
        var b = CreateBrand("ACTIVATE-OK");

        // Act/Assert.
        var a = Test.Http<Brand>()
            .ExpectPostgresOutboxEvents(e => e.AssertWithValue("contoso", "contoso.products.brand.activated.v1"))
            .Run(HttpMethod.Post, $"{_brandsUrl}/{b.Id}/activate")
            .AssertOK()
            .Value!;

        a.IsInactive.Should().BeFalse();
        a.ETag.Should().NotBe(b.ETag);

        // Assert - now within the (default) active list.
        GetBrands().Should().Contain(x => x.Code == "ACTIVATE-OK");
    }

    [Test]
    public void Activate_AlreadyActive()
    {
        var b = CreateActiveBrand("ACTIVATE-AGAIN");

        // Act/Assert - already active so no change and no event.
        var a = Test.Http<Brand>()
            .ExpectNoPostgresOutboxEvents()
            .Run(HttpMethod.Post, $"{_brandsUrl}/{b.Id}/activate")
            .AssertOK()
            .Value!;

        a.IsInactive.Should().BeFalse();
        a.ETag.Should().Be(b.ETag);
    }

    #endregion

    #region Deactivate

    [Test]
    public void Deactivate_NotFound()
    {
        Test.Http()
            .Run(HttpMethod.Post, $"{_brandsUrl}/404/deactivate")
            .AssertNotFound();
    }

    [Test]
    public void Deactivate_Success()
    {
        var b = CreateActiveBrand("DEACTIVATE-OK");
        GetBrands().Should().Contain(x => x.Code == "DEACTIVATE-OK");

        // Act/Assert.
        var d = Test.Http<Brand>()
            .ExpectPostgresOutboxEvents(e => e.AssertWithValue("contoso", "contoso.products.brand.deactivated.v1"))
            .Run(HttpMethod.Post, $"{_brandsUrl}/{b.Id}/deactivate")
            .AssertOK()
            .Value!;

        d.IsInactive.Should().BeTrue();
        d.ETag.Should().NotBe(b.ETag);

        // Assert - no longer within the (default) active list, but still exists.
        GetBrands().Should().NotContain(x => x.Code == "DEACTIVATE-OK");
        GetBrands(includeInactive: true).Should().Contain(x => x.Code == "DEACTIVATE-OK");
    }

    [Test]
    public void Deactivate_AlreadyInactive()
    {
        var b = CreateBrand("DEACTIVATE-AGAIN");

        // Act/Assert - already inactive so no change and no event.
        var d = Test.Http<Brand>()
            .ExpectNoPostgresOutboxEvents()
            .Run(HttpMethod.Post, $"{_brandsUrl}/{b.Id}/deactivate")
            .AssertOK()
            .Value!;

        d.IsInactive.Should().BeTrue();
        d.ETag.Should().Be(b.ETag);
    }

    [Test]
    public void Deactivate_PreCheck()
    {
        var b = GetBrands().Where(x => x.Code == "YETI").Single();

        // Act/Assert - pre-check should prevent deactivation.
        var d = Test.Http<Brand>()
            .ExpectNoPostgresOutboxEvents()
            .Run(HttpMethod.Post, $"{_brandsUrl}/{b.Id}/deactivate")
            .AssertBadRequest()
            .AssertProblemDetails(p => p.Title.Should().Be("YETI brand cannot be deactivated as it is awesome."));
    }

    #endregion

    #region Delete

    [Test]
    public void Delete_NotFound()
    {
        // Delete is idempotent.
        Test.Http()
            .ExpectNoPostgresOutboxEvents()
            .Run(HttpMethod.Delete, $"{_brandsUrl}/404")
            .AssertNoContent();
    }

    [Test]
    public void Delete_IsActive()
    {
        var b = CreateActiveBrand("DELETE-ACTIVE");

        Test.Http()
            .Run(HttpMethod.Delete, $"{_brandsUrl}/{b.Id}")
            .AssertBadRequest()
            .AssertProblemDetails(p => p.Title.Should().Be("An active reference data value cannot be deleted."));

        // Assert - still exists.
        Test.Http()
            .Run(HttpMethod.Get, $"{_brandsUrl}/{b.Id}")
            .AssertOK();
    }

    [Test]
    public void Delete_Success()
    {
        var b = CreateActiveBrand("DELETE-OK");
        GetBrands().Should().Contain(x => x.Code == "DELETE-OK");

        // Arrange - must be deactivated before it can be deleted.
        Test.Http()
            .ExpectPostgresOutboxEvents()
            .Run(HttpMethod.Post, $"{_brandsUrl}/{b.Id}/deactivate")
            .AssertOK();

        // Act.
        Test.Http()
            .ExpectPostgresOutboxEvents(e => e.AssertMetadata("contoso", "contoso.products.brand.deleted", b.Id!))
            .Run(HttpMethod.Delete, $"{_brandsUrl}/{b.Id}")
            .AssertNoContent();

        // Assert - idempotent.
        Test.Http()
            .ExpectNoPostgresOutboxEvents()
            .Run(HttpMethod.Delete, $"{_brandsUrl}/{b.Id}")
            .AssertNoContent();

        // Assert - gone, including from the cached reference data.
        Test.Http()
            .Run(HttpMethod.Get, $"{_brandsUrl}/{b.Id}")
            .AssertNotFound();

        GetBrands(includeInactive: true).Should().NotContain(x => x.Code == "DELETE-OK");
    }

    #endregion
}
