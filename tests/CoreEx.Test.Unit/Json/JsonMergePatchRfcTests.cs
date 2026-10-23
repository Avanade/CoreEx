namespace CoreEx.Test.Unit.Json;

[TestFixture]
public class JsonMergePatchRfcTests
{
    [TestCase("""{"Name":"old"}""", """{"name":"new"}""", """{"Name":"old","name":"new"}""", true)]
    [TestCase("""{"Name":"old"}""", """{"name":null}""", """{"Name":"old"}""", false)]
    [TestCase("""{"values":{"P1":1,"p1":2}}""", """{"values":{"P1":3}}""", """{"values":{"P1":3,"p1":2}}""", true)]
    [TestCase("""{"values":{"P1":1,"p1":2}}""", """{"values":{"P1":null}}""", """{"values":{"p1":2}}""", true)]
    [TestCase("""{"values":{"P1":1,"p1":2}}""", """{"values":{"P1":1}}""", """{"values":{"P1":1,"p1":2}}""", false)]
    [TestCase("""{}""", """{"missing":null}""", """{}""", false)]
    [TestCase("""{"values":{"P1":1}}""", """{"values":{"missing":null}}""", """{"values":{"P1":1}}""", false)]
    [TestCase("""[1,2]""", """{"keep":3,"remove":null}""", """{"keep":3}""", true)]
    [TestCase("""null""", """{"remove":null}""", """{}""", true)]
    [TestCase("""42""", """{}""", """{}""", true)]
    [TestCase("""{}""", """{"values":{"nested":{"remove":null}}}""", """{"values":{"nested":{}}}""", true)]
    [TestCase("""{"values":42}""", """{"values":{"remove":null,"keep":3}}""", """{"values":{"keep":3}}""", true)]
    [TestCase("""{"values":null}""", """{"values":{"remove":null}}""", """{"values":{}}""", true)]
    [TestCase("""{"values":[1,2]}""", """{"values":{"remove":null}}""", """{"values":{}}""", true)]
    [TestCase("""{"values":[1,2]}""", """{"values":[3]}""", """{"values":[3]}""", true)]
    [TestCase("""{"values":[1,2]}""", """{"values":[1,2]}""", """{"values":[1,2]}""", false)]
    [TestCase("""{"values":1}""", """null""", """null""", true)]
    [TestCase("""{"values":1}""", "\"replacement\"", "\"replacement\"", true)]
    [TestCase("""{"untouched":null}""", """{"added":1}""", """{"untouched":null,"added":1}""", true)]
    [TestCase("""{"values":[{"Name":1}]}""", """{"values":[{"name":1}]}""", """{"values":[{"name":1}]}""", true)]
    public void Merge_UsesRfcMemberIdentityAndRecursiveObjectSemantics(string target, string patch, string expected, bool hasChanges)
    {
        var merger = new CoreEx.Json.JsonMergePatch();
        var merged = JsonSerializer.Deserialize<JsonElement>(merger.Merge(patch, target));
        AssertJson(merged, expected);

        merger.TryMerge(patch, target, out string? changed).Should().Be(hasChanges);
        if (hasChanges)
            AssertJson(JsonSerializer.Deserialize<JsonElement>(changed!), expected);
        else
            changed.Should().BeNull();

        var targetElement = JsonSerializer.Deserialize<JsonElement>(target);
        var patchElement = JsonSerializer.Deserialize<JsonElement>(patch);
        merger.TryMerge(patchElement, targetElement, out JsonElement result).Should().Be(hasChanges);
        AssertJson(result, expected);
    }

    [Test]
    public void CaseInsensitiveMatching_RequiresExplicitCompatibilityOption()
    {
        var options = new CoreEx.Json.JsonMergePatchOptions { PropertyNameComparer = StringComparer.OrdinalIgnoreCase };
        var merger = new CoreEx.Json.JsonMergePatch(options);

        merger.Merge("""{"name":"new"}""", """{"Name":"old"}""").Should().Be("""{"name":"new"}""");
    }

    [TestCase("""{"values":{"P1":"updated"}}""", "updated")]
    [TestCase("""{"values":{"P1":null}}""", null)]
    public void TypedMerge_DistinguishesDictionaryKeysByCase(string patch, string? upperValue)
    {
        var target = new Dictionary<string, Dictionary<string, string>> { ["values"] = new() { ["P1"] = "upper", ["p1"] = "lower" } };
        var result = new CoreEx.Json.JsonMergePatch().Merge(patch, target).ThrowOnError().Value;

        result.HasChanges.Should().BeTrue();
        result.Merged.Should().NotBeNull();
        var values = result.Merged!["values"];
        values["p1"].Should().Be("lower");
        if (upperValue is null)
            values.Should().NotContainKey("P1");
        else
            values["P1"].Should().Be(upperValue);

        target["values"]["P1"].Should().Be("upper");
    }

    private static void AssertJson(JsonElement actual, string expected)
        => System.Text.Json.Nodes.JsonNode.DeepEquals(System.Text.Json.Nodes.JsonNode.Parse(actual.GetRawText()), System.Text.Json.Nodes.JsonNode.Parse(expected)).Should().BeTrue();
}
