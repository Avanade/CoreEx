using System;
namespace Contoso.Products.Test.Api;

public partial class ReadTests
{
    [Test]
    public void Movement_Query_All()
    {
        var r = Test.Http<ProductLite[]>()
            .Run(HttpMethod.Get, "/api/inventory/movements")
            .AssertOK()
            .Value;

        r.Should().NotBeNull().And.HaveCount(5);
    }

    [Test]
    public void Movement_Query_Filter()
    {
        var r = Test.Http<ProductLite[]>()
            .Run(HttpMethod.Get, $"/api/inventory/movements?$filter=referenceid eq '{1000.ToGuid()}' and productid ne '{6.ToGuid()}' and kind eq 'I' and status eq 'p'")
            .AssertOK()
            .AssertJsonFromResource("Movement_Query_Filter.res.json", ["changelog", "etag"]);
    }
}