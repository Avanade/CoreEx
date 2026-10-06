namespace Contoso.Customers.Test.Api;

public class RefDataMutateTests : WithApiTester<Contoso.Customers.Api.Program>
{
    private const string _url = "/api/refdata/customer-types";

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        await Test.DatabaseSetUpAsync("mutate-data.seed.yaml").ConfigureAwait(false);
        await Test.ClearFusionCacheAsync().ConfigureAwait(false);

        Test.UseExpectedCosmosDbOutboxPublisher();
    }

    /// <summary>
    /// Creates a new (inactive) customer type with the specified <paramref name="code"/>.
    /// </summary>
    private CustomerType CreateCustomerType(string code)
        => Test.Http<CustomerType>()
            .ExpectCosmosDbOutboxEvents()
            .Run(HttpMethod.Post, _url, new CustomerType { Code = code, Text = $"Customer type {code}" })
            .AssertCreated()
            .Value!;

    /// <summary>
    /// Creates a new customer type with the specified <paramref name="code"/> and activates it.
    /// </summary>
    private CustomerType CreateActiveCustomerType(string code)
    {
        var b = CreateCustomerType(code);
        return Test.Http<CustomerType>()
            .ExpectCosmosDbOutboxEvents()
            .Run(HttpMethod.Post, $"{_url}/{b.Id}/activate")
            .AssertOK()
            .Value!;
    }

    private CustomerType[] GetCustomerTypes(bool includeInactive = false)
        => Test.Http<CustomerType[]>()
            .Run(HttpMethod.Get, includeInactive ? $"{_url}?$inactive=true" : _url)
            .AssertOK()
            .Value!;

    #region Get

    [Test]
    public void Get_NotFound()
    {
        Test.Http()
            .Run(HttpMethod.Get, $"{_url}/404")
            .AssertNotFound();
    }

    [Test]
    public void Get_Success()
    {
        // Arrange - the seeded identifier is not assumed; get via the list.
        var b = GetCustomerTypes().Single(x => x.Code == "IND");

        // Act/Assert.
        Test.Http<CustomerType>()
            .Run(HttpMethod.Get, $"{_url}/{b.Id}")
            .AssertOK()
            .AssertValue(b);
    }

    #endregion

    #region Create

    [Test]
    public void Create_Empty()
    {
        Test.Http()
            .Run(HttpMethod.Post, _url, new CustomerType())
            .AssertBadRequest()
            .AssertErrors(
                "Code is required.",
                "Text is required."
            );
    }

    [Test]
    public void Create_Bad_Data()
    {
        var b = new CustomerType
        {
            Code = new string('X', CoreEx.Validation.ReferenceDataValidator<CustomerType>.CodeMaximumLength + 1),
            Text = new string('X', CoreEx.Validation.ReferenceDataValidator<CustomerType>.TextMaximumLength + 1),
            Description = new string('X', CoreEx.Validation.ReferenceDataValidator<CustomerType>.DescriptionMaximumLength + 1),
        };

        Test.Http()
            .Run(HttpMethod.Post, _url, b)
            .AssertBadRequest()
            .AssertErrors(
                $"Code must not exceed {CoreEx.Validation.ReferenceDataValidator<CustomerType>.CodeMaximumLength} character(s) in length.",
                $"Text must not exceed {CoreEx.Validation.ReferenceDataValidator<CustomerType>.TextMaximumLength} character(s) in length.",
                $"Description must not exceed {CoreEx.Validation.ReferenceDataValidator<CustomerType>.DescriptionMaximumLength} character(s) in length."
            );
    }

    [Test]
    public void Create_Duplicate()
    {
        Test.Http()
            .Run(HttpMethod.Post, _url, new CustomerType { Code = "IND", Text = "Yeti Again" })
            .AssertConflict();
    }

    [Test]
    public void Create_Success()
    {
        // Act/Assert - a newly created reference data value is always inactive until explicitly activated.
        var b = Test.Http<CustomerType>()
            .ExpectIdentifier()
            .ExpectETag()
            .ExpectCosmosDbOutboxEvents(e => e.AssertWithValue("contoso", "contoso.customers.customertype.created.v1"))
            .Run(HttpMethod.Post, _url, new CustomerType { Code = "CREATE-OK", Text = "Create OK", SortOrder = 5 })
            .AssertCreated()
            .AssertLocationHeader(r => new Uri($"{_url}/{r!.Id}", UriKind.Relative))
            .Value!;

        b.Code.Should().Be("CREATE-OK");
        b.Text.Should().Be("Create OK");
        b.SortOrder.Should().Be(5);
        b.IsInactive.Should().BeTrue();

        // Assert - get by identifier.
        Test.Http<CustomerType>()
            .Run(HttpMethod.Get, $"{_url}/{b.Id}")
            .AssertOK()
            .AssertValue(b);

        // Assert - not within the (default) active list, but is within the inactive-inclusive list.
        GetCustomerTypes().Should().NotContain(x => x.Code == "CREATE-OK");
        GetCustomerTypes(includeInactive: true).Should().Contain(x => x.Code == "CREATE-OK");
    }

    [Test]
    public void Create_IdempotencyKey()
    {
        var b = new CustomerType { Code = "CREATE-IDEM", Text = "Create Idempotent" };
        var ik = Guid.NewGuid().ToString();

        // Act/Assert - first request creates and emits an event.
        var v1 = Test.Http<CustomerType>()
            .ExpectCosmosDbOutboxEvents()
            .Run(HttpMethod.Post, _url, b, requestModifier: r => r.WithIdempotencyKey(ik))
            .AssertCreated()
            .Value!;

        // Assert - repeat with the same idempotency key; same result and no further event emitted.
        var v2 = Test.Http<CustomerType>()
            .ExpectNoCosmosDbOutboxEvents()
            .Run(HttpMethod.Post, _url, b, requestModifier: r => r.WithIdempotencyKey(ik))
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
            .Run(HttpMethod.Patch, $"{_url}/404", new { text = "abc" }, requestModifier: r => r.WithMergePatchJsonContentType())
            .AssertNotFound();
    }

    [Test]
    public void Patch_Concurrency()
    {
        var b = CreateCustomerType("PATCH-CONC");

        Test.Http()
            .Run(HttpMethod.Patch, $"{_url}/{b.Id}", new { text = "Changed" }, requestModifier: r => r.WithIfMatch("AAAAAAAA").WithMergePatchJsonContentType())
            .AssertPreconditionFailed();
    }

    [Test]
    public void Patch_Validation()
    {
        var b = CreateCustomerType("PATCH-VAL");

        Test.Http()
            .Run(HttpMethod.Patch, $"{_url}/{b.Id}", new { text = string.Empty }, requestModifier: r => r.WithIfMatch(b.ETag).WithMergePatchJsonContentType())
            .AssertBadRequest()
            .AssertErrors("Text is required.");
    }

    [Test]
    public void Patch_Success()
    {
        var b = CreateCustomerType("PATCH-OK");

        // Act/Assert.
        var u = Test.Http<CustomerType>()
            .ExpectCosmosDbOutboxEvents(e => e.AssertWithValue("contoso", "contoso.customers.customertype.updated.v1"))
            .Run(HttpMethod.Patch, $"{_url}/{b.Id}", new { text = "Patched Text", sortOrder = 9 }, requestModifier: r => r.WithIfMatch(b.ETag).WithMergePatchJsonContentType())
            .AssertOK()
            .Value!;

        u.Id.Should().Be(b.Id);
        u.Code.Should().Be(b.Code);
        u.Text.Should().Be("Patched Text");
        u.SortOrder.Should().Be(9);
        u.IsInactive.Should().BeTrue();
        u.ETag.Should().NotBe(b.ETag);

        // Assert.
        Test.Http<CustomerType>()
            .Run(HttpMethod.Get, $"{_url}/{b.Id}")
            .AssertOK()
            .AssertValue(u);
    }

    [Test]
    public void Patch_Code_Immutable()
    {
        var b = CreateCustomerType("PATCH-CODE");

        // Act/Assert - the code is silently retained; only the text is updated.
        var u = Test.Http<CustomerType>()
            .ExpectCosmosDbOutboxEvents()
            .Run(HttpMethod.Patch, $"{_url}/{b.Id}", new { code = "PATCH-CODE-CHANGED", text = "Code Retained" }, requestModifier: r => r.WithIfMatch(b.ETag).WithMergePatchJsonContentType())
            .AssertOK()
            .Value!;

        u.Code.Should().Be("PATCH-CODE");
        u.Text.Should().Be("Code Retained");
    }

    [Test]
    public void Patch_IsInactive_Ignored()
    {
        // The active state can only be changed via activate/deactivate.
        var b = CreateCustomerType("PATCH-ACTIVE");

        var u = Test.Http<CustomerType>()
            .ExpectCosmosDbOutboxEvents()
            .Run(HttpMethod.Patch, $"{_url}/{b.Id}", new { isInactive = false, text = "Still Inactive" }, requestModifier: r => r.WithIfMatch(b.ETag).WithMergePatchJsonContentType())
            .AssertOK()
            .Value!;

        u.IsInactive.Should().BeTrue();
        u.Text.Should().Be("Still Inactive");
    }

    [Test]
    public void Patch_NoChanges()
    {
        var b = CreateCustomerType("PATCH-NONE");

        // Act/Assert - nothing changed so no event and the etag remains.
        var u = Test.Http<CustomerType>()
            .ExpectNoCosmosDbOutboxEvents()
            .Run(HttpMethod.Patch, $"{_url}/{b.Id}", new { }, requestModifier: r => r.WithIfMatch(b.ETag).WithMergePatchJsonContentType())
            .AssertOK()
            .Value!;

        u.ETag.Should().Be(b.ETag);
        u.Text.Should().Be(b.Text);
    }

    #endregion

    #region Put

    [Test]
    public void Put_NotFound()
    {
        Test.Http()
            .Run(HttpMethod.Put, $"{_url}/404", new { code = "PUT-404", text = "abc" }, requestModifier: r => r.WithIfMatch("abc"))
            .AssertNotFound();
    }

    [Test]
    public void Put_MissingETag()
    {
        Test.Http()
            .Run(HttpMethod.Put, $"{_url}/404", new { code = "PUT-404", text = "abc" })
            .Assert(System.Net.HttpStatusCode.PreconditionRequired);
    }

    [Test]
    public void Put_Concurrency()
    {
        var b = CreateCustomerType("PUT-CONC");

        Test.Http()
            .Run(HttpMethod.Put, $"{_url}/{b.Id}", new { code = b.Code, text = "Changed" }, requestModifier: r => r.WithIfMatch("AAAAAAAA"))
            .AssertPreconditionFailed();
    }

    [Test]
    public void Put_Validation()
    {
        var b = CreateCustomerType("PUT-VAL");

        Test.Http()
            .Run(HttpMethod.Put, $"{_url}/{b.Id}", new { code = b.Code, text = string.Empty }, requestModifier: r => r.WithIfMatch(b.ETag))
            .AssertBadRequest()
            .AssertErrors("Text is required.");
    }

    [Test]
    public void Put_Success()
    {
        var b = CreateCustomerType("PUT-OK");

        // Act/Assert.
        var u = Test.Http<CustomerType>()
            .ExpectCosmosDbOutboxEvents(e => e.AssertWithValue("contoso", "contoso.customers.customertype.updated.v1"))
            .Run(HttpMethod.Put, $"{_url}/{b.Id}", new { code = b.Code, text = "Put Text", sortOrder = 9 }, requestModifier: r => r.WithIfMatch(b.ETag))
            .AssertOK()
            .Value!;

        u.Id.Should().Be(b.Id);
        u.Code.Should().Be(b.Code);
        u.Text.Should().Be("Put Text");
        u.SortOrder.Should().Be(9);
        u.IsInactive.Should().BeTrue();
        u.ETag.Should().NotBe(b.ETag);

        // Assert.
        Test.Http<CustomerType>()
            .Run(HttpMethod.Get, $"{_url}/{b.Id}")
            .AssertOK()
            .AssertValue(u);
    }

    [Test]
    public void Put_Code_And_IsInactive_Ignored()
    {
        // The code is immutable and the active state can only be changed via activate/deactivate.
        var b = CreateCustomerType("PUT-IGNORED");

        var u = Test.Http<CustomerType>()
            .ExpectCosmosDbOutboxEvents()
            .Run(HttpMethod.Put, $"{_url}/{b.Id}", new { code = "PUT-IGNORED-CHANGED", text = "Retained", isInactive = false }, requestModifier: r => r.WithIfMatch(b.ETag))
            .AssertOK()
            .Value!;

        u.Code.Should().Be("PUT-IGNORED");
        u.IsInactive.Should().BeTrue();
        u.Text.Should().Be("Retained");
    }

    [Test]
    public void Put_NoChanges()
    {
        var b = CreateCustomerType("PUT-NONE");

        // Act/Assert - nothing changed so no event and the etag remains.
        var u = Test.Http<CustomerType>()
            .ExpectNoCosmosDbOutboxEvents()
            .Run(HttpMethod.Put, $"{_url}/{b.Id}", new { code = b.Code, text = b.Text, sortOrder = b.SortOrder }, requestModifier: r => r.WithIfMatch(b.ETag))
            .AssertOK()
            .Value!;

        u.ETag.Should().Be(b.ETag);
    }

    #endregion

    #region Activate

    [Test]
    public void Activate_NotFound()
    {
        Test.Http()
            .Run(HttpMethod.Post, $"{_url}/404/activate")
            .AssertNotFound();
    }

    [Test]
    public void Activate_Success()
    {
        var b = CreateCustomerType("ACTIVATE-OK");

        // Act/Assert.
        var a = Test.Http<CustomerType>()
            .ExpectCosmosDbOutboxEvents(e => e.AssertWithValue("contoso", "contoso.customers.customertype.activated.v1"))
            .Run(HttpMethod.Post, $"{_url}/{b.Id}/activate")
            .AssertOK()
            .Value!;

        a.IsInactive.Should().BeFalse();
        a.ETag.Should().NotBe(b.ETag);

        // Assert - now within the (default) active list.
        GetCustomerTypes().Should().Contain(x => x.Code == "ACTIVATE-OK");
    }

    [Test]
    public void Activate_AlreadyActive()
    {
        var b = CreateActiveCustomerType("ACTIVATE-AGAIN");

        // Act/Assert - already active so no change and no event.
        var a = Test.Http<CustomerType>()
            .ExpectNoCosmosDbOutboxEvents()
            .Run(HttpMethod.Post, $"{_url}/{b.Id}/activate")
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
            .Run(HttpMethod.Post, $"{_url}/404/deactivate")
            .AssertNotFound();
    }

    [Test]
    public void Deactivate_Success()
    {
        var b = CreateActiveCustomerType("DEACTIVATE-OK");
        GetCustomerTypes().Should().Contain(x => x.Code == "DEACTIVATE-OK");

        // Act/Assert.
        var d = Test.Http<CustomerType>()
            .ExpectCosmosDbOutboxEvents(e => e.AssertWithValue("contoso", "contoso.customers.customertype.deactivated.v1"))
            .Run(HttpMethod.Post, $"{_url}/{b.Id}/deactivate")
            .AssertOK()
            .Value!;

        d.IsInactive.Should().BeTrue();
        d.ETag.Should().NotBe(b.ETag);

        // Assert - no longer within the (default) active list, but still exists.
        GetCustomerTypes().Should().NotContain(x => x.Code == "DEACTIVATE-OK");
        GetCustomerTypes(includeInactive: true).Should().Contain(x => x.Code == "DEACTIVATE-OK");
    }

    [Test]
    public void Deactivate_AlreadyInactive()
    {
        var b = CreateCustomerType("DEACTIVATE-AGAIN");

        // Act/Assert - already inactive so no change and no event.
        var d = Test.Http<CustomerType>()
            .ExpectNoCosmosDbOutboxEvents()
            .Run(HttpMethod.Post, $"{_url}/{b.Id}/deactivate")
            .AssertOK()
            .Value!;

        d.IsInactive.Should().BeTrue();
        d.ETag.Should().Be(b.ETag);
    }

    #endregion

    #region Delete

    [Test]
    public void Delete_NotFound()
    {
        // Delete is idempotent.
        Test.Http()
            .ExpectNoCosmosDbOutboxEvents()
            .Run(HttpMethod.Delete, $"{_url}/404")
            .AssertNoContent();
    }

    [Test]
    public void Delete_IsActive()
    {
        var b = CreateActiveCustomerType("DELETE-ACTIVE");

        Test.Http()
            .Run(HttpMethod.Delete, $"{_url}/{b.Id}")
            .AssertBadRequest()
            .AssertProblemDetails(p => p.Title.Should().Be("An active reference data value cannot be deleted."));

        // Assert - still exists.
        Test.Http()
            .Run(HttpMethod.Get, $"{_url}/{b.Id}")
            .AssertOK();
    }

    [Test]
    public void Delete_Success()
    {
        var b = CreateActiveCustomerType("DELETE-OK");
        GetCustomerTypes().Should().Contain(x => x.Code == "DELETE-OK");

        // Arrange - must be deactivated before it can be deleted.
        Test.Http()
            .ExpectCosmosDbOutboxEvents()
            .Run(HttpMethod.Post, $"{_url}/{b.Id}/deactivate")
            .AssertOK();

        // Act.
        Test.Http()
            .ExpectCosmosDbOutboxEvents(e => e.AssertMetadata("contoso", "contoso.customers.customertype.deleted", b.Id!))
            .Run(HttpMethod.Delete, $"{_url}/{b.Id}")
            .AssertNoContent();

        // Assert - idempotent.
        Test.Http()
            .ExpectNoCosmosDbOutboxEvents()
            .Run(HttpMethod.Delete, $"{_url}/{b.Id}")
            .AssertNoContent();

        // Assert - gone, including from the cached reference data.
        Test.Http()
            .Run(HttpMethod.Get, $"{_url}/{b.Id}")
            .AssertNotFound();

        GetCustomerTypes(includeInactive: true).Should().NotContain(x => x.Code == "DELETE-OK");
    }

    #endregion
}
