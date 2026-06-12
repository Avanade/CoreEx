namespace Contoso.Products.Test.Api;

public partial class HostTests
{
    [Test]
    public void RefData_Categories()
    {
        Test.Http<Category[]>()
            .Run(HttpMethod.Get, "/api/refdata/categories")
            .AssertOK()
            .Value.Should().HaveCountGreaterThan(0);
    }

    [Test]
    public void RefData_SubCategories()
    {
        var r = Test.Http<SubCategory[]>()
            .Run(HttpMethod.Get, "/api/refdata/sub-categories")
            .AssertOK()
            .Value;

        r.Should().HaveCountGreaterThan(0);
        r.Should().Contain(sc => sc.Code == "CA" && sc.Text == "Cassette" && sc.CategoryCode == "P");
    }

    [Test]
    public void RefData_UnitsOfMeasure()
    {
        var r = Test.Http<UnitOfMeasure[]>()
            .Run(HttpMethod.Get, "/api/refdata/units-of-measure")
            .AssertOK()
            .Value;

        r.Should().HaveCountGreaterThan(0);
        r.Should().Contain(uom => uom.Code == "EA" && uom.Text == "Each" && uom.Scale == 0);
        r.Should().Contain(uom => uom.Code == "HR" && uom.Text == "Hour" && uom.Scale == 2);
    }

    [Test]
    public void RefData_Brands()
    {
        Test.Http<Brand[]>()
            .Run(HttpMethod.Get, "/api/refdata/brands")
            .AssertOK()
            .Value.Should().HaveCountGreaterThan(0);
    }

    [Test]
    public void RefData_Named()
    {
        var r = Test.Http<JsonElement>()
            .Run(HttpMethod.Get, "/api/refdata?name=sub-cateGORies&name=brands&name=subcategory&name=other")
            .AssertOK()
            .Value;

        r.ValueKind.Should().Be(JsonValueKind.Object);
        r.EnumerateObject().Count().Should().Be(2);

        r.TryGetProperty("sub-categories", out var subCategory).Should().BeTrue();
        subCategory.GetArrayLength().Should().BeGreaterThan(0);

        r.TryGetProperty("brands", out var brands).Should().BeTrue();
        brands.GetArrayLength().Should().BeGreaterThan(0);
    }
}