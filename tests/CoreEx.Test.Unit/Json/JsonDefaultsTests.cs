namespace CoreEx.Test.Unit.Json;

[TestFixture]
public class JsonDefaultsTests
{
    [Test]
    public void Defaults_PreserveDictionaryKeysAndPropertyNaming()
    {
        var options = new CoreEx.Json.JsonDefaults.JsonDefaultConfiguration().SerializerOptions;
        var target = new DictionaryDocument
        {
            DisplayName = "Original",
            ETag = "version",
            Values = new() { ["P1"] = "upper", ["p1"] = "lower", ["HomeAddress"] = "home", ["ETag"] = "key" }
        };

        options.DictionaryKeyPolicy.Should().BeNull();
        var json = JsonSerializer.Serialize(target, options);
        json.Should().Be("""{"displayName":"Original","etag":"version","values":{"P1":"upper","p1":"lower","HomeAddress":"home","ETag":"key"}}""");
        var restored = JsonSerializer.Deserialize<DictionaryDocument>(json, options);
        restored.Should().NotBeNull();
        restored!.Values.Should().BeEquivalentTo(target.Values);
    }

    [Test]
    public void ExplicitDictionaryKeyPolicy_RemainsSupported()
    {
        var options = new CoreEx.Json.JsonDefaults.JsonDefaultConfiguration().SerializerOptions;
        options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;

        JsonSerializer.Serialize(new Dictionary<string, string> { ["HomeAddress"] = "home" }, options)
            .Should().Be("""{"homeAddress":"home"}""");
    }

    [TestCase("""{}""", false, "Original")]
    [TestCase("""{"displayName":"Updated"}""", true, "Updated")]
    public void MergePatch_PreservesUnpatchedDictionaryKeys(string patch, bool hasChanges, string displayName)
    {
        var target = new DictionaryDocument
        {
            DisplayName = "Original",
            Values = new() { ["P1"] = "upper", ["p1"] = "lower", ["ETag"] = "key" }
        };

        var result = new CoreEx.Json.JsonMergePatch().Merge(patch, target).ThrowOnError().Value;

        result.HasChanges.Should().Be(hasChanges);
        result.Merged.Should().NotBeNull();
        result.Merged!.DisplayName.Should().Be(displayName);
        result.Merged.Values.Should().BeEquivalentTo(target.Values);
        target.Values.Keys.Should().BeEquivalentTo("P1", "p1", "ETag");
    }

    public class DictionaryDocument
    {
        public string? DisplayName { get; set; }
        public string? ETag { get; set; }
        public Dictionary<string, string> Values { get; set; } = [];
    }
}
