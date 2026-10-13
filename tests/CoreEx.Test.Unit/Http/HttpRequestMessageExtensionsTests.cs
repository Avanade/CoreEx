namespace CoreEx.Test.Unit.Http;

[TestFixture]
public class HttpRequestMessageExtensionsTests
{
    [Test]
    public void WithQuery_EncodesAmpersandInValue_DoesNotInjectExtraParameter()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/api");
        request.WithQuery(filter: "a=1&b=2");

        var query = request.RequestUri!.Query.TrimStart('?');
        var pairs = query.Split('&');

        // Without encoding, the raw '&' in the value would be misinterpreted as a second query parameter.
        pairs.Should().HaveCount(1);

        var parts = pairs[0].Split('=', 2);
        parts[0].Should().Be("$filter");
        Uri.UnescapeDataString(parts[1]).Should().Be("a=1&b=2");
    }

    [Test]
    public void WithQuery_EncodesSpacesAndSpecialCharacters_RoundTrips()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/api");
        request.WithQuery(filter: "name eq 'John & Jane'", orderBy: "name desc");

        var query = request.RequestUri!.Query.TrimStart('?');
        var pairs = query.Split('&').Select(p => p.Split('=', 2)).ToDictionary(p => p[0], p => Uri.UnescapeDataString(p[1]));

        pairs.Should().HaveCount(2);
        pairs["$filter"].Should().Be("name eq 'John & Jane'");
        pairs["$orderby"].Should().Be("name desc");
    }

    [Test]
    public void WithQuery_QueryArgs_IncludesSearchAndFlags()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/api");
        request.WithQuery(new CoreEx.Data.QueryArgs { Filter = "a eq 1", OrderBy = "a", Search = "red  & bike", IsIncludeText = true, IsIncludeInactive = true });

        var pairs = request.RequestUri!.Query.TrimStart('?').Split('&').Select(p => p.Split('=', 2)).ToDictionary(p => p[0], p => Uri.UnescapeDataString(p[1]));

        pairs.Should().HaveCount(5);
        pairs["$filter"].Should().Be("a eq 1");
        pairs["$orderby"].Should().Be("a");
        pairs["$search"].Should().Be("red  & bike");
        pairs["$text"].Should().Be("true");
        pairs["$inactive"].Should().Be("true");
    }

    [Test]
    public void WithSearch_BlankIsIgnored()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/api");
        request.WithSearch("  ");
        request.RequestUri!.Query.Should().BeEmpty();

        request.WithSearch("bike");
        request.RequestUri!.Query.Should().Be("?$search=bike");
    }

    [Test]
    public void WithIdempotencyKey_AddsHeader()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://example.com/api");
        request.WithIdempotencyKey("abc-123");

        request.Headers.GetValues(CoreEx.Http.HttpNames.IdempotencyKeyHeaderName).Should().ContainSingle().Which.Should().Be("abc-123");
    }
}
