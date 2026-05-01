namespace Contoso.Products.Test.Outbox.Relay;

public partial class OtherTests
{
    [Test]
    public void HostedService_Pause_And_Resume()
    {
        var s = Test.Http<string>()
            .Run(HttpMethod.Get, "/hosted-services/sqlserver-outbox-relay-03/status")
            .Value;

        s.Should().BeOneOf("Running", "Sleeping");

        Test.Http()
            .Run(HttpMethod.Post, "/hosted-services/sqlserver-outbox-relay-03/pause")
            .Response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        s = Test.Delay(TimeSpan.FromSeconds(1))
            .Http<string>()
            .Run(HttpMethod.Get, "/hosted-services/sqlserver-outbox-relay-03/status")
            .Value;

        s.Should().Be("Paused");

        Test.Http()
            .Run(HttpMethod.Post, "/hosted-services/sqlserver-outbox-relay-03/resume")
            .Response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        s = Test.Delay(TimeSpan.FromSeconds(1))
            .Http<string>()
            .Run(HttpMethod.Get, "/hosted-services/sqlserver-outbox-relay-03/status")
            .Value;

        s.Should().BeOneOf("Running", "Sleeping");
    }
}