using CoreEx.Entities;
using CoreEx.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoreEx.Test.Unit.Json;

[TestFixture]
public class JsonSubstituteNamingPolicyTests
{
    [Test]
    public void Default_Test()
    {
        var p = new Person { Id = "abc", Age = 55, ETag = "xyz" };
        var json = JsonSerializer.Serialize(p, JsonDefaults.SerializerOptions);
        json.Should().Be("""{"id":"abc","years":55,"etag":"xyz"}""");
    }

    [Test]
    public void ConvertName_NoSubstitution_UsesFallbackPolicy_NotHardcodedCamelCase()
    {
        var policy = new JsonSubstituteNamingPolicy { FallbackPolicy = JsonNamingPolicy.SnakeCaseLower };
        policy.ConvertName("FirstName").Should().Be("first_name");
    }

    [Test]
    public void ConvertName_Substitution_TakesPrecedenceOverFallbackPolicy()
    {
        var policy = new JsonSubstituteNamingPolicy { FallbackPolicy = JsonNamingPolicy.SnakeCaseLower };
        policy.ConvertName("ETag").Should().Be("etag");
    }

    private class Person : IIdentifier<string?>, IReadOnlyETag
    {
        public string? Id { get; set; }

        [JsonPropertyName("years")]
        public int Age { get; set; }

        public string? ETag { get; set; }
    }
}