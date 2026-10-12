namespace Contoso.Shopping.Test.Unit.Clients.SendGrid;

public class SendGridHttpClientTests : WithGenericTester<EntryPoint>
{
    private MockHttpClientRequest _mockHttpSendMailRequest = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var mcf = UnitTestEx.MockHttpClientFactory.Create();
        _mockHttpSendMailRequest = mcf.CreateClient("SendGrid").Request(HttpMethod.Post, "v3/mail/send");
        Test.ReplaceHttpClientFactory(mcf);
    }

    [Test]
    public void SendMailAsync_Success_ReturnsSuccess() => Test.Scoped(test =>
    {
        _mockHttpSendMailRequest.WithAnyBody()
            .Respond.With(HttpStatusCode.Accepted);

        test.Run(async _ =>
        {
            var client = ExecutionContext.GetRequiredService<SendGridHttpClient>();
            var result = await client.SendMailAsync(CreateRequest()).ConfigureAwait(false);
            result.IsSuccess.Should().BeTrue();
        }).AssertSuccess();

        _mockHttpSendMailRequest.Verify();
    });

    [Test]
    public void SendMailAsync_ServerError_ReturnsFailure() => Test.Scoped(test =>
    {
        _mockHttpSendMailRequest.WithAnyBody()
            .Respond.With(HttpStatusCode.InternalServerError);

        test.Run(async _ =>
        {
            var client = ExecutionContext.GetRequiredService<SendGridHttpClient>();
            var result = await client.SendMailAsync(CreateRequest()).ConfigureAwait(false);
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<HttpRequestException>();
        }).AssertSuccess();

        _mockHttpSendMailRequest.Verify();
    });

    [Test]
    public void SendMailAsync_UnprocessableEntityProblemDetails_ReturnsMappedFailure() => Test.Scoped(test =>
    {
        _mockHttpSendMailRequest.WithAnyBody()
            .Respond.With(new StringContent("{\"type\":\"https://tools.ietf.org/html/rfc4918#section-11.2\",\"title\":\"Unprocessable Entity\",\"status\":422,\"detail\":\"The from address does not match a verified Sender Identity.\"}", System.Text.Encoding.UTF8, "application/problem+json"), HttpStatusCode.UnprocessableEntity);

        test.Run(async _ =>
        {
            var client = ExecutionContext.GetRequiredService<SendGridHttpClient>();
            var result = await client.SendMailAsync(CreateRequest()).ConfigureAwait(false);
            result.IsFailure.Should().BeTrue();
            var pde = result.Error.Should().BeOfType<ProblemDetailsException>().Subject;
            pde.ProblemDetails.Status.Should().Be(422);
            pde.ProblemDetails.Detail.Should().Be("The from address does not match a verified Sender Identity.");
        }).AssertSuccess();

        _mockHttpSendMailRequest.Verify();
    });

    private static SendGridMailRequest CreateRequest() => new()
    {
        From = new() { Email = "orders@contoso.com", Name = "Contoso Shopping" },
        Personalizations = [new() { To = [new() { Email = "customer-1@customer.contoso.local" }], Subject = "Your Contoso order is confirmed" }],
        Content = [new() { Type = "text/plain", Value = "Thanks for shopping with Contoso!" }]
    };
}
