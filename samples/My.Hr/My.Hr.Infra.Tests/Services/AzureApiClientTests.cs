
using System.Net;
using My.Hr.Infra.Services;
using UnitTestEx.NUnit;

namespace My.Hr.Infra.Tests.Services;

public class AzureApiClientTests
{
    [Test]
    public async Task GetMyIP_Should_ReturnIP()
    {
        // Arrange
        const string mockedIp = "192.168.1.1";
        var mcf = MockHttpClientFactory.Create();
        var mc = mcf.CreateClient("ip");

        mc.Request(HttpMethod.Get, "https://api.ipify.org")
            .Respond.With(new StringContent(mockedIp));

        var target = new AzureApiClient(mcf.GetHttpClient("ip")!);

        // Act
        var result = await target.GetMyIP();

        // Assert
        result.Should().Be(mockedIp);
    }
}