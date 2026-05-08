namespace Contoso.Products.Test.Outbox.Relay;

public partial class OtherTests
{
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
        string[] _requiredServices =
        [
            "postgres",
            "postgres-outbox-relay-00",
            "postgres-outbox-relay-01",
            "postgres-outbox-relay-02",
            "postgres-outbox-relay-03"
        ];

        var r = Test.Http()
            .Run(HttpMethod.Get, path)
            .AssertContentTypeJson();

        r.Response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);

        var json = r.GetContent();
        if (minimal)
            json.Should().NotContainAny(_requiredServices);
        else
            json.Should().ContainAll(_requiredServices);
    }
}