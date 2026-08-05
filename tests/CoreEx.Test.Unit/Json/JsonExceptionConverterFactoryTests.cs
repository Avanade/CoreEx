using CoreEx.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoreEx.Test.Unit.Json;

[TestFixture]
public class JsonExceptionConverterFactoryTests
{
    private static JsonSerializerOptions CreateOptions() => new() { Converters = { new JsonExceptionConverterFactory() } };

    [Test]
    public void Write_IncludesNonIgnoredProperties()
    {
        var ex = new TestException("boom") { Extra = "info" };
        var json = JsonSerializer.Serialize(ex, CreateOptions());

        json.Should().Contain("\"Message\":\"boom\"");
        json.Should().Contain("\"Extra\":\"info\"");
    }

    [Test]
    public void Write_ExcludesJsonIgnoreDecoratedProperties()
    {
        var ex = new TestException("boom") { Hidden = "secret" };
        var json = JsonSerializer.Serialize(ex, CreateOptions());

        json.Should().NotContain("Hidden");
        json.Should().NotContain("secret");
    }

    [Test]
    public void Write_ExcludesTargetSite()
    {
        // TargetSite is only populated once thrown; explicitly excluded regardless. Note: the exception message and
        // resulting stack trace deliberately avoid the substring "TargetSite" so it can't leak in via unrelated
        // property values (e.g. StackTrace) and produce a false positive.
        Exception ex;
        try
        {
            throw new TestException("boom");
        }
        catch (TestException caught)
        {
            ex = caught;
        }

        var json = JsonSerializer.Serialize(ex, CreateOptions());
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty(nameof(Exception.TargetSite), out _).Should().BeFalse();
    }

    private class TestException(string message) : Exception(message)
    {
        public string? Extra { get; set; }

        [JsonIgnore]
        public string? Hidden { get; set; }
    }
}
