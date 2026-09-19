namespace Contoso.Customers.Test.Api;

public partial class HostTests : WithApiTester<Contoso.Customers.Api.Program>
{
    [OneTimeSetUp]
    public Task OneTimeSetUpAsync() => Test.DatabaseSetUpAsync();

    [Test]
    public void Swagger_UI()
    {
        // Hit swagger and assert redirect.
        Test.Http()
            .Run(HttpMethod.Get, "/swagger")
            .Assert(HttpStatusCode.Found)
            .AssertLocationHeader(new Uri("/swagger/index.html", UriKind.Relative));

        // Go to redirected URL and assert basic content.
        Test.Http()
            .Run(HttpMethod.Get, "/swagger/index.html")
            .Assert(HttpStatusCode.OK)
            .GetContent().Should().Contain("<title>Swagger UI</title>");
    }

    [Test]
    public void Swagger_Json()
    {
        Test.Http()
            .Run(HttpMethod.Get, "/swagger/v1/swagger.json")
            .Assert(HttpStatusCode.OK)
            .AssertContentTypeJson()
            .GetContent().Should().BeJson()
                .ContainAll(["$.openapi", "$.info", "$.paths"])
                .HavePath("$.info.title").GetValue<string>().Should().Be("Contoso.Customers.Api");
    }

    [TestCase("/health/live")]
    [TestCase("/health/startup")]
    [TestCase("/health/ready")]
    public void Health(string path)
    {
        Test.Http()
            .Run(HttpMethod.Get, path)
            .Response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
    }

    [TestCase("/health/live/detailed", true)]
    [TestCase("/health/startup/detailed", false)]
    [TestCase("/health/ready/detailed", false)]
    public void Health_Detailed(string path, bool minimal)
    {
        // Both registered against HealthCheckTags.StartUpAndReadyOnly (not Live), matching AddHostedService's own convention - liveness reflects the process, not downstream dependencies.
        string[] paths = ["$.entries.reference-data-orchestrator", "$.entries.cosmos-database"];

        var r = Test.Http()
            .Run(HttpMethod.Get, path)
            .AssertContentTypeJson();

        r.Response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);

        var json = r.GetContent().Should().BeJson();
        if (minimal)
            json.NotContainAny(paths);
        else
            json.ContainAll(paths);
    }
}
