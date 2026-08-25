using CoreEx.Json;
using System.Text.Json.Nodes;

namespace CoreEx.Test.Unit.Json;

[TestFixture]
public class JsonFilterTests
{
    [TestCase(null!, "$")]
    [TestCase("", "$")]
    [TestCase("foo", "$.foo")]
    [TestCase("[0]", "$[0]")]
    [TestCase("$.foo", "$.foo")]
    public void PrependRootPath_Works(string input, string expected)
    {
        JsonFilter.PrependRootPath(input).Should().Be(expected);
    }

    [TestCase(null!, false, null!)]
    [TestCase("", false, "")]
    [TestCase("$.foo[0].bar[1]", true, "$.foo.bar")]
    [TestCase("$.foo.bar", false, "$.foo.bar")]
    [TestCase("$.entries['stackExchange.Redis'].enabled", false, "$.entries['stackExchange.Redis'].enabled")]
    [TestCase("$.entries['stackExchange.Redis'][0].name", true, "$.entries['stackExchange.Redis'].name")]
    [TestCase("$.entries[\"stackExchange.Redis\"]", false, "$.entries[\"stackExchange.Redis\"]")]
    public void TryRemovePathIndexes_Works(string input, bool expectedResult, string expectedPath)
    {
        var result = JsonFilter.TryRemovePathIndexes(input, out var path);
        result.Should().Be(expectedResult);
        path.Should().Be(expectedPath);
    }

    [Test]
    public void CreateDictionary_Include_AddsIntermediaries()
    {
        int maxDepth = 0;
        var dict = JsonFilter.CreateDictionary(["$.a.b.c"], JsonFilterOption.Include, StringComparison.Ordinal, ref maxDepth);
        dict.Should().ContainKey("$.a.b.c");
        dict.Should().ContainKey("$.a.b");
        dict.Should().ContainKey("$.a");
        dict.Should().ContainKey("$");
        dict["$.a.b.c"].Should().BeTrue();
        dict["$.a.b"].Should().BeFalse();
        dict["$.a"].Should().BeFalse();
        dict["$"].Should().BeFalse();
        maxDepth.Should().Be(4);
    }

    [Test]
    public void CreateDictionary_Include_BracketNotation_AddsIntermediaries()
    {
        int maxDepth = 0;
        var dict = JsonFilter.CreateDictionary(["$.entries['stackExchange.Redis'].enabled"], JsonFilterOption.Include, StringComparison.Ordinal, ref maxDepth);
        dict.Should().ContainKey("$");
        dict.Should().ContainKey("$.entries");
        dict.Should().ContainKey("$.entries['stackExchange.Redis']");
        dict.Should().ContainKey("$.entries['stackExchange.Redis'].enabled");
        dict["$"].Should().BeFalse();
        dict["$.entries"].Should().BeFalse();
        dict["$.entries['stackExchange.Redis']"].Should().BeFalse();
        dict["$.entries['stackExchange.Redis'].enabled"].Should().BeTrue();
        maxDepth.Should().Be(4);
    }

    [Test]
    public void CreateDictionary_Exclude_NoIntermediaries()
    {
        int maxDepth = 0;
        var dict = JsonFilter.CreateDictionary(["$.a.b"], JsonFilterOption.Exclude, StringComparison.Ordinal, ref maxDepth);
        dict.Should().ContainKey("$.a.b");
        dict["$.a.b"].Should().BeTrue();
        dict.Count.Should().Be(1);
        maxDepth.Should().Be(3);
    }

    [Test]
    public void CreateDictionary_Include_SiblingPropertyIsRawStringPrefixOfAnother_BothRemainSpecified()
    {
        // Regression: '$.category' is a raw string prefix of '$.categoryText' (they are sibling scalar properties, not a nested path), so neither should be
        // demoted to an 'intermediary' (false) entry as a result of the other being present.
        int maxDepth = 0;
        var dict = JsonFilter.CreateDictionary(["$.category", "$.categoryText"], JsonFilterOption.Include, StringComparison.OrdinalIgnoreCase, ref maxDepth);
        dict["$.category"].Should().BeTrue();
        dict["$.categoryText"].Should().BeTrue();
    }

    [Test]
    public void TryJsonFilter_Include_RemovesOtherProperties()
    {
        var json = "{\"a\":1,\"b\":2,\"c\":3}";
        var paths = new[] { "$.a", "$.c" };
        var result = JsonFilter.TryJsonFilter(json, paths, out var filtered, JsonFilterOption.Include);
        result.Should().BeTrue();
        filtered.Should().Be("{\"a\":1,\"c\":3}");
    }

    [Test]
    public void TryJsonFilter_Include_SiblingPropertyIsRawStringPrefixOfAnother_BothIncluded()
    {
        // Regression test: requesting both 'category' and 'categoryText' (a common CoreEx ref-data Code/Text property pair) must return both - previously
        // 'category' was incorrectly dropped because '$.categoryText' textually starts with '$.category', even though they are unrelated sibling properties.
        var json = "{\"category\":\"CAT1\",\"categoryText\":\"Category One\",\"sku\":\"ABC\"}";
        var paths = new[] { "category", "categoryText" };
        var result = JsonFilter.TryJsonFilter(json, paths, out var filtered, JsonFilterOption.Include);
        result.Should().BeTrue();
        filtered.Should().Be("{\"category\":\"CAT1\",\"categoryText\":\"Category One\"}");
    }

    [Test]
    public void TryJsonFilter_Exclude_RemovesSpecifiedProperties()
    {
        var json = "{\"a\":1,\"b\":2,\"c\":3}";
        var paths = new[] { "$.b" };
        var result = JsonFilter.TryJsonFilter(json, paths, out var filtered, JsonFilterOption.Exclude);
        result.Should().BeTrue();
        filtered.Should().Be("{\"a\":1,\"c\":3}");
    }

    [Test]
    public void TryJsonFilter_NoPaths_NoChange()
    {
        var json = "{\"a\":1,\"b\":2}";
        var result = JsonFilter.TryJsonFilter(json, null, out var filtered, JsonFilterOption.Include);
        result.Should().BeFalse();
        filtered.Should().Be("{\"a\":1,\"b\":2}");
    }

    public class TestObj { public int X { get; set; } public int Y { get; set; } }

    [Test]
    public void TryFilter_T_ReturnsFilteredJson()
    {
        var obj = new TestObj { X = 1, Y = 2 };
        var result = JsonFilter.TryFilter(obj, ["$.X"], out string json, JsonFilterOption.Include);
        result.Should().BeTrue();
        json.Should().Be("{\"x\":1}");
    }

    [Test]
    public void TryFilter_T_ReturnsFilteredJsonNode()
    {
        var obj = new TestObj { X = 1, Y = 2 };
        var result = JsonFilter.TryFilter(obj, ["$.Y"], out JsonNode node, JsonFilterOption.Include);
        result.Should().BeTrue();
        var json = node.ToJsonString();
        json.Should().Be("{\"y\":2}");
    }

    private const string _json = """
    {
        "Name": "John Doe",
        "Age": 30,
        "IsEmployed": true,
        "Skills": ["C#", "JavaScript", "Python"],
        "Address": {
            "Street": "123 Main St",
            "City": "Anytown",
            "State": "CA"
        },
        "Projects": [
            {
                "Name": "Project A",
                "Year": 2020,
                "Technologies": ["C#", "ASP.NET"]
            },
            {
                "Name": "Project B",
                "Year": 2021,
                "Technologies": ["JavaScript", "React"]
            }
        ]
    }
    """;

    [Test]
    public void TryJsonFilter_Include_Simple()
    {
        string exp = """
            {
                "Name": "John Doe",
                "Skills": ["C#", "JavaScript", "Python"]
            }
            """;

        var r = JsonFilter.TryJsonFilter(_json, ["name", "skills"], out string json);
        r.Should().BeTrue();
        ObjectComparer.AssertJson(exp, json);
    }

    [Test]
    public void TryJsonFilter_Include_NoMatches()
    {
        string exp = """
            {
            }
            """;

        var r = JsonFilter.TryJsonFilter(_json, ["parent", "address.country", "skills[4]", "projects[3].years"], out string json);
        r.Should().BeTrue();
        ObjectComparer.AssertJson(exp, json);
    }

    [Test]
    public void TryJsonFilter_Include_Indexed()
    {
        string exp = """
            {
                "Skills": ["JavaScript"],
                "Projects": [
                    {
                        "Name": "Project A",
                        "Year": 2020,
                        "Technologies": ["C#", "ASP.NET"]
                    }
                ]
            }
            """;

        var r = JsonFilter.TryJsonFilter(_json, ["skills[1]", "projects[0]"], out string json);
        r.Should().BeTrue();
        ObjectComparer.AssertJson(exp, json);
    }

    [Test]
    public void TryJsonFilter_Include_Indexed_Indexed()
    {
        string exp = """
            {
                "Projects": [
                    {
                        "Technologies": ["React"]
                    }
                ]
            }
            """;

        var r = JsonFilter.TryJsonFilter(_json, ["projects[1].technologies[1]"], out string json);
        r.Should().BeTrue();
        ObjectComparer.AssertJson(exp, json);
    }

    [Test]
    public void TryJsonFilter_Include_Indexed_Property_Indexed()
    {
        string exp = """
            {
                "Projects": [
                    {
                        "Year": 2020
                    },
                    {
                        "Year": 2021,
                        "Technologies": ["React"]
                    }
                ]
            }
            """;

        var r = JsonFilter.TryJsonFilter(_json, ["projects.year", "projects[1].technologies[1]"], out string json);
        r.Should().BeTrue();
        ObjectComparer.AssertJson(exp, json);
    }

    [Test]
    public void TryJsonFilter_Include_Array()
    {
        string val = """
            [
                {
                    "Name": "John Doe",
                    "Age": 30
                },
                {
                    "Name": "Jane Smith",
                    "Age": 25
                }
            ]
            """;

        string exp = """
            [
                {
                    "Name": "John Doe"
                },
                {
                    "Name": "Jane Smith"
                }
            ]
            """;

        var r = JsonFilter.TryJsonFilter(val, ["name"], out string json);
        r.Should().BeTrue();
        ObjectComparer.AssertJson(exp, json);
    }

    [Test]
    public void TryJsonFilter_Include_Null_Value()
    {
        var r = JsonFilter.TryJsonFilter("null", ["age"], out string json);
        r.Should().BeFalse();
        ObjectComparer.AssertJson("null", json);
    }

    [Test]
    public void TryFilter_Include_Null_Value()
    {
        var r = JsonFilter.TryFilter<string?>(null, ["age"], out string json);
        r.Should().BeFalse();
        ObjectComparer.AssertJson("null", json);
    }

    [Test]
    public void TryFilter_Include_Int_Value()
    {
        // filtering a json value is non-sensical and will return as-is.
        var r = JsonFilter.TryFilter(1, ["age"], out string json);
        r.Should().BeFalse();
        ObjectComparer.AssertJson("1", json);
    }

    [Test]
    public void TryJsonFilter_Exclude_Nothing()
    {
        string val = """
            {
                "Name": "John Doe",
                "Age": 30,
                "IsEmployed": true
            }
            """;

        var r = JsonFilter.TryJsonFilter(val, ["height"], out string json, JsonFilterOption.Exclude);
        r.Should().BeFalse();
        ObjectComparer.AssertJson(val, json);
    }

    [Test]
    public void TryJsonFilter_Exclude_Simple()
    {
        string val = """
            {
                "Name": "John Doe",
                "Age": 30,
                "IsEmployed": true
            }
            """;

        string exp = """
            {
                "Name": "John Doe",
                "IsEmployed": true
            }
            """;

        var r = JsonFilter.TryJsonFilter(val, ["age"], out string json, JsonFilterOption.Exclude);
        r.Should().BeTrue();
        ObjectComparer.AssertJson(exp, json);
    }

    [Test]
    public void TryJsonFilter_Exclude_Simple_Array()
    {
        string val = """
            {
                "Name": "John Doe",
                "Skills": ["C#", "JavaScript", "Python"]
            }
            """;

        string exp = """
            {
                "Name": "John Doe",
                "Skills": ["C#", "Python"]
            }
            """;

        var r = JsonFilter.TryJsonFilter(val, ["skills[1]"], out string json, JsonFilterOption.Exclude);
        r.Should().BeTrue();
        ObjectComparer.AssertJson(exp, json);
    }

    [Test]
    public void TryJsonFilter_Exclude_Complex()
    {
        string val = """
            {
                "Name": "John Doe",
                "Address": {
                    "Street": "123 Main St",
                    "City": "Anytown",
                    "State": "CA"
                }
            }
            """;

        string exp = """
            {
                "Name": "John Doe",
                "Address": {
                    "Street": "123 Main St"
                }
            }
            """;

        var r = JsonFilter.TryJsonFilter(val, ["address.city", "address.state"], out string json, JsonFilterOption.Exclude);
        r.Should().BeTrue();
        ObjectComparer.AssertJson(exp, json);
    }

    [Test]
    public void TryJsonFilter_Exclude_Complex_Array()
    {
        string val = """
            {
                "Name": "John Doe",
                "Projects": [
                    {
                        "Name": "Project A",
                        "Year": 2020,
                        "Technologies": ["C#", "ASP.NET"]
                    },
                    {
                        "Name": "Project B",
                        "Year": 2021,
                        "Technologies": ["JavaScript", "React"]
                    }
                ]
            }
            """;

        string exp = """
            {
                "Name": "John Doe",
                "Projects": [
                    {
                        "Name": "Project A",
                        "Technologies": ["C#", "ASP.NET"]
                    },
                    {
                        "Name": "Project B",
                        "Year": 2021,
                        "Technologies": ["JavaScript"]
                    }
                ]
            }
            """;

        var r = JsonFilter.TryJsonFilter(val, ["projects[0].year", "projects[1].technologies[1]"], out string json, JsonFilterOption.Exclude);
        r.Should().BeTrue();
        ObjectComparer.AssertJson(exp, json);
    }

    [Test]
    public void TryJsonFilter_Exclude_Array()
    {
        string val = """
            [
                {
                    "Name": "John Doe",
                    "Age": 30
                },
                {
                    "Name": "Jane Smith",
                    "Age": 25
                }
            ]
            """;

        string exp = """
            [
                {
                    "Name": "John Doe"
                },
                {
                    "Name": "Jane Smith"
                }
            ]
            """;

        var r = JsonFilter.TryJsonFilter(val, ["age"], out string json, JsonFilterOption.Exclude);
        r.Should().BeTrue();
        ObjectComparer.AssertJson(exp, json);
    }

    [Test]
    public void TryJsonFilter_Exclude_Null_Value()
    {
        var r = JsonFilter.TryJsonFilter("null", ["age"], out string json, JsonFilterOption.Exclude);
        r.Should().BeFalse();
        ObjectComparer.AssertJson("null", json);
    }

    [Test]
    public void TryFilter_Exclude_Null_Value()
    {
        var r = JsonFilter.TryFilter<string?>(null, ["age"], out string json, JsonFilterOption.Exclude);
        r.Should().BeFalse();
        ObjectComparer.AssertJson("null", json);
    }

    [Test]
    public void TryFilter_Exclude_Int_Value()
    {
        var r = JsonFilter.TryFilter(1, ["age"], out string json, JsonFilterOption.Exclude);
        r.Should().BeFalse();
        ObjectComparer.AssertJson("1", json);
    }

    [Test]
    public void TryFilter_Object_Array_Object()
    {
        string val = """
            {
              "Products": [
                {
                  "Category": [
                    { "A": "Accessories" },
                    { "B": "Bikes" }
                  ],
                  "Other": [
                    { "G": "Gear" }
                  ]
                }
              ]
            }
            """;

        string exp = """
            {
              "Products": [
                {
                  "Category": [
                    { "A": "Accessories" },
                    { "B": "Bikes" }
                  ]
                }
              ]
            }
            """;

        var r = JsonFilter.TryJsonFilter(val, ["products.category"], out string json);
        r.Should().BeTrue();
        ObjectComparer.AssertJson(exp, json);
    }

    private const string _bracketJson = """
        {
            "entries": {
                "stackExchange.Redis": {
                    "enabled": true,
                    "resiliency": "high"
                },
                "inMemory": {
                    "enabled": false
                }
            }
        }
        """;

    [Test]
    public void TryJsonFilter_Include_BracketNotation_SingleQuote()
    {
        string exp = """
            {
                "entries": {
                    "stackExchange.Redis": {
                        "enabled": true
                    }
                }
            }
            """;

        var r = JsonFilter.TryJsonFilter(_bracketJson, ["$.entries['stackExchange.Redis'].enabled"], out string json);
        r.Should().BeTrue();
        ObjectComparer.AssertJson(exp, json);
    }

    [Test]
    public void TryJsonFilter_Include_BracketNotation_DoubleQuote()
    {
        string exp = """
            {
                "entries": {
                    "stackExchange.Redis": {
                        "enabled": true
                    }
                }
            }
            """;

        var r = JsonFilter.TryJsonFilter(_bracketJson, ["$.entries[\"stackExchange.Redis\"].enabled"], out string json);
        r.Should().BeTrue();
        ObjectComparer.AssertJson(exp, json);
    }

    [Test]
    public void TryJsonFilter_Include_BracketNotation_ObjectNode()
    {
        string exp = """
            {
                "entries": {
                    "stackExchange.Redis": {
                        "enabled": true,
                        "resiliency": "high"
                    }
                }
            }
            """;

        var r = JsonFilter.TryJsonFilter(_bracketJson, ["$.entries['stackExchange.Redis']"], out string json);
        r.Should().BeTrue();
        ObjectComparer.AssertJson(exp, json);
    }

    [Test]
    public void TryJsonFilter_Exclude_BracketNotation()
    {
        string exp = """
            {
                "entries": {
                    "inMemory": {
                        "enabled": false
                    }
                }
            }
            """;

        var r = JsonFilter.TryJsonFilter(_bracketJson, ["$.entries['stackExchange.Redis']"], out string json, JsonFilterOption.Exclude);
        r.Should().BeTrue();
        ObjectComparer.AssertJson(exp, json);
    }

    [Test]
    public void TryJsonFilter_Include_BracketNotation_WithIndex()
    {
        string val = """
            {
                "entries": {
                    "stackExchange.Redis": {
                        "hosts": ["host1", "host2"]
                    },
                    "inMemory": {
                        "hosts": ["host3"]
                    }
                }
            }
            """;

        string exp = """
            {
                "entries": {
                    "stackExchange.Redis": {
                        "hosts": ["host2"]
                    }
                }
            }
            """;

        var r = JsonFilter.TryJsonFilter(val, ["$.entries['stackExchange.Redis'].hosts[1]"], out string json);
        r.Should().BeTrue();
        ObjectComparer.AssertJson(exp, json);
    }

    [Test]
    public void GetMatched_BracketNotation()
    {
        var node = JsonNode.Parse(_bracketJson)!;
        var matched = JsonFilter.GetMatched(node, "$.entries['stackExchange.Redis'].enabled");
        matched.Should().NotBeNull();
        matched!.GetValue<bool>().Should().BeTrue();
    }

    [Test]
    public void TryJsonFilter_Exclude_RecursiveDescent_AnyDepth()
    {
        string val = """
            {
                "Name": "John",
                "Password": "secret1",
                "Account": {
                    "Password": "secret2",
                    "Username": "john"
                },
                "Users": [
                    { "Name": "A", "Password": "secret3" },
                    { "Name": "B", "Password": "secret4" }
                ]
            }
            """;

        string exp = """
            {
                "Name": "John",
                "Account": {
                    "Username": "john"
                },
                "Users": [
                    { "Name": "A" },
                    { "Name": "B" }
                ]
            }
            """;

        var r = JsonFilter.TryJsonFilter(val, ["..Password"], out string json, JsonFilterOption.Exclude);
        r.Should().BeTrue();
        ObjectComparer.AssertJson(exp, json);
    }

    [Test]
    public void TryJsonFilter_Exclude_RecursiveDescent_MultiSegmentTail()
    {
        string val = """
            {
                "Foo": { "Bar": 1, "Baz": 2 },
                "Nested": { "Foo": { "Bar": 3, "Baz": 4 } },
                "Other": { "Bar": 5 }
            }
            """;

        string exp = """
            {
                "Foo": { "Baz": 2 },
                "Nested": { "Foo": { "Baz": 4 } },
                "Other": { "Bar": 5 }
            }
            """;

        var r = JsonFilter.TryJsonFilter(val, ["..Foo.Bar"], out string json, JsonFilterOption.Exclude);
        r.Should().BeTrue();
        ObjectComparer.AssertJson(exp, json);
    }

    [Test]
    public void TryJsonFilter_Exclude_RecursiveDescent_DoesNotMatchRawStringSuffix()
    {
        // Regression: '..Text' must not match 'LongText' - the match must occur at a proper path-segment boundary, not merely as a raw string suffix.
        string val = """
            {
                "LongText": "keep me",
                "Nested": { "Text": "remove me", "LongText": "keep me too" }
            }
            """;

        string exp = """
            {
                "LongText": "keep me",
                "Nested": { "LongText": "keep me too" }
            }
            """;

        var r = JsonFilter.TryJsonFilter(val, ["..Text"], out string json, JsonFilterOption.Exclude);
        r.Should().BeTrue();
        ObjectComparer.AssertJson(exp, json);
    }

    [Test]
    public void TryJsonFilter_Exclude_RecursiveDescent_RootPrefixOptional()
    {
        string val = """{ "A": { "Foo": 1 }, "B": { "Foo": 2 } }""";
        string exp = """{ "A": { }, "B": { } }""";

        var r1 = JsonFilter.TryJsonFilter(val, ["..Foo"], out string json1, JsonFilterOption.Exclude);
        var r2 = JsonFilter.TryJsonFilter(val, ["$..Foo"], out string json2, JsonFilterOption.Exclude);

        r1.Should().BeTrue();
        r2.Should().BeTrue();
        ObjectComparer.AssertJson(exp, json1);
        ObjectComparer.AssertJson(exp, json2);
    }

    [Test]
    public void TryJsonFilter_Include_RecursiveDescent_AnyDepth()
    {
        string val = """
            {
                "Id": 1,
                "Name": "root",
                "Child": { "Id": 2, "Name": "child", "GrandChild": { "Id": 3, "Name": "grand" } },
                "Items": [
                    { "Id": 4, "Name": "item1" },
                    { "Id": 5, "Name": "item2" }
                ]
            }
            """;

        string exp = """
            {
                "Id": 1,
                "Child": { "Id": 2, "GrandChild": { "Id": 3 } },
                "Items": [
                    { "Id": 4 },
                    { "Id": 5 }
                ]
            }
            """;

        var r = JsonFilter.TryJsonFilter(val, ["..Id"], out string json, JsonFilterOption.Include);
        r.Should().BeTrue();
        ObjectComparer.AssertJson(exp, json);
    }

    [Test]
    public void TryJsonFilter_Include_RecursiveDescent_CombinedWithPlainPath()
    {
        string val = """
            {
                "Id": 1,
                "Name": "root",
                "Child": { "Id": 2, "Name": "child" }
            }
            """;

        string exp = """
            {
                "Id": 1,
                "Name": "root",
                "Child": { "Id": 2 }
            }
            """;

        var r = JsonFilter.TryJsonFilter(val, ["name", "..Id"], out string json, JsonFilterOption.Include);
        r.Should().BeTrue();
        ObjectComparer.AssertJson(exp, json);
    }

    [Test]
    public void TryJsonFilter_RecursiveDescent_EmptyTail_Throws()
    {
        Assert.Throws<ArgumentException>(() => JsonFilter.TryJsonFilter("{}", [".."], out _, JsonFilterOption.Exclude));
    }

    // The following exercise TryExcludeUtf8Json (the streaming, JsonNode-free Exclude engine) both directly and via the parity check against the
    // JsonNode-based Filter(JsonNode,...) engine, since both must produce equivalent JSON output for every path shape (compared structurally via ObjectComparer.AssertJson, not byte-for-byte).

    [Test]
    public void TryExcludeUtf8Json_NoPatterns_ReturnsInputUnchanged()
    {
        var utf8 = System.Text.Encoding.UTF8.GetBytes("""{"a":1,"b":2}""");
        var r = JsonFilter.TryExcludeUtf8Json(utf8, null, out var filtered);
        r.Should().BeFalse();
        filtered.Should().BeEquivalentTo(utf8);
    }

    [Test]
    public void TryExcludeUtf8Json_RecursiveDescent_StripsAtAnyDepth()
    {
        var utf8 = System.Text.Encoding.UTF8.GetBytes("""{"id":1,"etag":"a","child":{"id":2,"etag":"b"},"items":[{"id":3,"etag":"c"}]}""");
        var r = JsonFilter.TryExcludeUtf8Json(utf8, ["$..etag"], out var filtered);
        r.Should().BeTrue();
        ObjectComparer.AssertJson("""{"id":1,"child":{"id":2},"items":[{"id":3}]}""", System.Text.Encoding.UTF8.GetString(filtered));
    }

    [Test]
    public void TryExcludeUtf8Json_DefaultWriterOptions_IsCompact()
    {
        // Regression: TryExcludeUtf8Json must default to compact output - matching a plain `new Utf8JsonWriter(stream)` - not silently pick up indentation from an ambient JsonSerializerOptions.
        var utf8 = System.Text.Encoding.UTF8.GetBytes("""{"a":1,"etag":"x","b":{"c":2}}""");
        JsonFilter.TryExcludeUtf8Json(utf8, ["etag"], out var filtered);
        var json = System.Text.Encoding.UTF8.GetString(filtered);
        json.Should().NotContain("\n");
        json.Should().Be("""{"a":1,"b":{"c":2}}""");
    }

    [Test]
    public void TryExcludeUtf8Json_ExplicitIndentedWriterOptions_IsIndented()
    {
        var utf8 = System.Text.Encoding.UTF8.GetBytes("""{"a":1,"etag":"x"}""");
        JsonFilter.TryExcludeUtf8Json(utf8, ["etag"], out var filtered, new System.Text.Json.JsonWriterOptions { Indented = true });
        var json = System.Text.Encoding.UTF8.GetString(filtered);
        json.Should().Contain("\n");
    }

    [Test]
    public void TryExcludeUtf8Json_NullPropertyValue_DoesNotThrow()
    {
        // Regression: a literal JSON null property/array-element value must not be treated as an error - it simply has nothing to recurse into.
        var utf8 = System.Text.Encoding.UTF8.GetBytes("""{"a":1,"b":null,"c":{"etag":null},"items":[null,{"etag":"x"}]}""");
        var r = JsonFilter.TryExcludeUtf8Json(utf8, ["$..etag"], out var filtered);
        r.Should().BeTrue();
        ObjectComparer.AssertJson("""{"a":1,"b":null,"c":{},"items":[null,{}]}""", System.Text.Encoding.UTF8.GetString(filtered));
    }

    [Test]
    public void Filter_Exclude_NullPropertyValue_DoesNotThrow()
    {
        // Same regression as TryExcludeUtf8Json_NullPropertyValue_DoesNotThrow, but via the JsonNode-based Filter(JsonNode,...) engine.
        var node = JsonNode.Parse("""{"a":1,"b":null,"c":{"etag":null},"items":[null,{"etag":"x"}]}""")!;
        var r = JsonFilter.Filter(node, ["$..etag"], JsonFilterOption.Exclude);
        r.Should().BeTrue();
        ObjectComparer.AssertJson("""{"a":1,"b":null,"c":{},"items":[null,{}]}""", node.ToJsonString());
    }

    private static readonly string[] _parityJson =
    [
        """{"name":"John Doe","etag":"e1","password":"p1","address":{"city":"Anytown","etag":"e2"},"skills":["C#","JavaScript","Python"],"projects":[{"name":"A","year":2020,"etag":"e3","technologies":["C#","ASP.NET"]},{"name":"B","year":2021,"etag":"e4","technologies":["JavaScript","React"]}]}""",
        """{"entries":{"stackExchange.Redis":{"enabled":true,"etag":"e5"},"inMemory":{"enabled":false}}}""",
        """[{"name":"John Doe","age":30,"etag":"e6"},{"name":"Jane Smith","age":25,"etag":"e7"}]""",
        """{"category":"CAT1","categoryText":"Category One","etagText":"not-an-etag","etag":"e8"}""",
        """null""",
        """{"a":{"b":1,"etag":"x"},"nested":{"a":{"b":2,"etag":"y"}},"other":{"b":3}}"""
    ];

    private static readonly string[][] _parityPaths =
    [
        ["etag"],
        ["$..etag"],
        ["..etag"],
        ["address.etag"],
        ["projects[0].etag", "projects[1].technologies[1]"],
        ["projects.etag"],
        ["$.entries['stackExchange.Redis'].etag"],
        ["..etag", "categoryText"],
        ["..a.b"]
    ];

    [Test]
    public void Filter_Exclude_DomAndStreamingEnginesProduceIdenticalOutput()
    {
        foreach (var json in _parityJson)
        {
            foreach (var paths in _parityPaths)
            {
                var domNode = JsonNode.Parse(json);
                var domResult = domNode is not null && JsonFilter.Filter(domNode, paths, JsonFilterOption.Exclude);
                var domJson = domNode?.ToJsonString() ?? "null";

                var streamResult = JsonFilter.TryJsonFilter(json, paths, out var streamJson, JsonFilterOption.Exclude);

                streamResult.Should().Be(domResult, because: $"json='{json}', paths='{string.Join(",", paths)}'");
                ObjectComparer.AssertJson(domJson, streamJson);
            }
        }
    }
}