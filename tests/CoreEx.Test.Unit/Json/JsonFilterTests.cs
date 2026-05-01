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
    public void TryJsonFilter_Include_RemovesOtherProperties()
    {
        var json = "{\"a\":1,\"b\":2,\"c\":3}";
        var paths = new[] { "$.a", "$.c" };
        var result = JsonFilter.TryJsonFilter(json, paths, out var filtered, JsonFilterOption.Include);
        result.Should().BeTrue();
        filtered.Should().Be("{\"a\":1,\"c\":3}");
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
}