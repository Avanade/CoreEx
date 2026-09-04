using CoreEx.Data.Json;

namespace CoreEx.Data.Test.Unit.Json;

public class JsonDataReaderTests
{
    public class Widget
    {
        public string? Code { get; set; }
        public string? Text { get; set; }
        public Guid Id { get; set; }
        public Guid Ref { get; set; }
        public DateTimeOffset Now { get; set; }
        public DateTimeOffset Tomorrow { get; set; }
        public DateTimeOffset Yesterday { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public string? Sku { get; set; }
        public decimal Price { get; set; }
        public string? TenantId { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? CreatedBy { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
    }

    [Test]
    public void ParseJson_Deserialize_SimpleObject()
    {
        var jdr = JsonDataReader.ParseJson("""{ "widget": { "code": "ABC", "text": "A widget" } }""");
        var w = jdr.Deserialize<Widget>("widget");

        w.Should().NotBeNull();
        w!.Code.Should().Be("ABC");
        w.Text.Should().Be("A widget");
    }

    [Test]
    public void ParseJson_Deserialize_MissingPath_ReturnsDefault()
    {
        var jdr = JsonDataReader.ParseJson("""{ "widget": { "code": "ABC" } }""");
        var w = jdr.Deserialize<Widget>("does-not-exist");

        w.Should().BeNull();
    }

    [Test]
    public void ParseJson_DynamicParameters_GuidAndDates()
    {
        var jdr = JsonDataReader.ParseJson("""{ "widget": { "id": "^guid", "ref": "^guid", "now": "^now", "tomorrow": "^tomorrow", "yesterday": "^yesterday" } }""");
        var w = jdr.Deserialize<Widget>("widget");

        w.Should().NotBeNull();
        w!.Id.Should().NotBe(Guid.Empty);
        w.Ref.Should().NotBe(Guid.Empty);
        w.Id.Should().NotBe(w.Ref, "each '^guid' substitution must generate an independent value");
        w.Tomorrow.Should().BeAfter(w.Now);
        w.Yesterday.Should().BeBefore(w.Now);
    }

    [Test]
    public void ParseJson_ArrayIndex_UsesElementPosition()
    {
        var jdr = JsonDataReader.ParseJson("""{ "widgets": [ { "sortOrder": "^index" }, { "sortOrder": "^index" } ] }""");
        var widgets = jdr.Deserialize<List<Widget>>("widgets");

        widgets.Should().NotBeNull().And.HaveCount(2);
        widgets![0].SortOrder.Should().Be(0);
        widgets[1].SortOrder.Should().Be(1);
    }

    [Test]
    public void CreateForReferenceData_SingleKey_MapsToCodeAndText()
    {
        var jdr = JsonDataReader.ParseJson("""{ "widget": { "ABC": "A widget" } }""", JsonDataReaderOptions.CreateForReferenceData(JsonPropertyNamingConvention.CamelCase));
        var w = jdr.Deserialize<Widget>("widget");

        w.Should().NotBeNull();
        w!.Code.Should().Be("ABC");
        w.Text.Should().Be("A widget");
        w.IsActive.Should().BeTrue();
        w.Id.Should().NotBe(Guid.Empty);
    }

    [Test]
    public void CreateForReferenceData_MultiKey_LeavesExplicitPropertiesAlone()
    {
        var jdr = JsonDataReader.ParseJson("""{ "widget": { "code": "XYZ", "text": "Explicit widget", "sortOrder": 5 } }""", JsonDataReaderOptions.CreateForReferenceData(JsonPropertyNamingConvention.CamelCase));
        var w = jdr.Deserialize<Widget>("widget");

        w.Should().NotBeNull();
        w!.Code.Should().Be("XYZ");
        w.Text.Should().Be("Explicit widget");
        w.SortOrder.Should().Be(5, "an explicitly-supplied property must not be overwritten by the standard/reference-data defaults");
    }

    [Test]
    public void ParseYaml_Deserialize_SimpleObject()
    {
        // YAML is the primary real-world path (every sample *.yaml/*.seed.yaml fixture) - previously entirely untested, only ParseJson was ever exercised.
        var jdr = JsonDataReader.ParseYaml("""
            widget:
              code: ABC
              text: A widget
            """);
        var w = jdr.Deserialize<Widget>("widget");

        w.Should().NotBeNull();
        w!.Code.Should().Be("ABC");
        w.Text.Should().Be("A widget");
    }

    [Test]
    public void ParseYaml_LeadingZeroNumber_StaysString()
    {
        // The custom YamlNodeTypeResolver must keep a leading-zero value as a string - "007" is not a valid JSON number, and real fixtures rely on this (e.g. SKUs, codes with leading zeros).
        var jdr = JsonDataReader.ParseYaml("""
            widget:
              sku: 007
            """);
        var w = jdr.Deserialize<Widget>("widget");

        w.Should().NotBeNull();
        w!.Sku.Should().Be("007");
    }

    [Test]
    public void ParseYaml_BoolAndDecimalLiterals_CoerceCorrectly()
    {
        var jdr = JsonDataReader.ParseYaml("""
            widget:
              isActive: true
              price: 16.99
            """);
        var w = jdr.Deserialize<Widget>("widget");

        w.Should().NotBeNull();
        w!.IsActive.Should().BeTrue();
        w.Price.Should().Be(16.99m);
    }

    [Test]
    public void ParseJson_NumericDynamicParameter_GeneratesDeterministicGuidFromInt()
    {
        // The convention every real fixture file uses (e.g. "product_id: ^1") - a bare integer key deterministically maps to the same Guid every time, and different integers map to different Guids.
        var jdr = JsonDataReader.ParseJson("""{ "widgets": [ { "id": "^1" }, { "id": "^1" }, { "id": "^2" } ] }""");
        var widgets = jdr.Deserialize<List<Widget>>("widgets");

        widgets.Should().NotBeNull().And.HaveCount(3);
        widgets![0].Id.Should().Be(widgets[1].Id, "the same numeric token must always resolve to the same deterministic Guid");
        widgets[0].Id.Should().NotBe(widgets[2].Id, "different numeric tokens must resolve to different Guids");
        widgets[0].Id.Should().NotBe(Guid.Empty);
    }

    [Test]
    public void ParseJson_EmbeddedDynamicParameter_ReplacesOnlyThePlaceholderPortion()
    {
        // Distinct from a whole-value '^xxx' replacement - '(^xxx)' substitutes just the placeholder within a larger string, leaving the rest of the string intact.
        var jdr = JsonDataReader.ParseJson("""{ "widget": { "code": "order-(^guid)-suffix" } }""");
        var w = jdr.Deserialize<Widget>("widget");

        w.Should().NotBeNull();
        w!.Code.Should().StartWith("order-").And.EndWith("-suffix");
        w.Code.Should().MatchRegex(@"^order-[0-9a-fA-F-]{36}-suffix$");
    }

    [Test]
    public void ParseJson_EmbeddedDynamicParameter_ResolvesRecursively()
    {
        // A parameter function whose own returned value contains another '(^xxx)' placeholder must have that inner placeholder resolved too, not left as a literal string.
        var options = new JsonDataReaderOptions();
        options.Parameters.Add("nested", _ => "prefix-(^guid)");

        var jdr = JsonDataReader.ParseJson("""{ "widget": { "code": "(^nested)" } }""", options);
        var w = jdr.Deserialize<Widget>("widget");

        w.Should().NotBeNull();
        w!.Code.Should().MatchRegex(@"^prefix-[0-9a-fA-F-]{36}$", "the inner '(^guid)' placeholder produced by the 'nested' parameter must itself be resolved, not left literal");
    }

    [Test]
    public void ParseJson_TenantIdUserIdUserNameTokens_ProduceNonEmptyValues()
    {
        // No ambient ExecutionContext is expected in this test - these tokens must still resolve via their documented fallback (Options.TenantId / AuthenticationUser.EnvironmentUser) rather than
        // throwing or producing an empty value.
        var jdr = JsonDataReader.ParseJson("""{ "widget": { "tenantId": "^tenant_id", "userId": "^user_id", "userName": "^user_name" } }""");
        var w = jdr.Deserialize<Widget>("widget");

        w.Should().NotBeNull();
        w!.UserId.Should().NotBeNullOrEmpty();
        w.UserName.Should().NotBeNullOrEmpty();
    }

    [Test]
    public void AddStandardProperties_FillsMissing_ButNeverOverwritesExplicitValues()
    {
        var options = new JsonDataReaderOptions(JsonPropertyNamingConvention.CamelCase).AddStandardProperties();

        var jdr = JsonDataReader.ParseJson("""{ "widget": { "code": "ABC", "createdBy": "explicit-user" } }""", options);
        var w = jdr.Deserialize<Widget>("widget");

        w.Should().NotBeNull();
        w!.CreatedBy.Should().Be("explicit-user", "an explicitly-supplied standard property must not be overwritten");
        w.CreatedOn.Should().NotBe(default(DateTimeOffset), "a standard property missing from the source must be filled in via '^now'");
    }

    [Test]
    public void ParseJson_RootNotAnObject_Throws()
    {
        // The constructor enforces that the root node must be a JsonObject - a top-level JSON array is not a valid data reader source.
        var act = () => JsonDataReader.ParseJson("""[ { "code": "ABC" } ]""");

        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void ParseJson_SnakeCaseNamingConvention_AppliesToStandardProperties()
    {
        // Only CamelCase was ever exercised previously (via the reference-data tests) - PascalCase is the default and SnakeCase/KebabCase were entirely untested.
        var options = new JsonDataReaderOptions(JsonPropertyNamingConvention.SnakeCase).AddStandardProperties();

        var jdr = JsonDataReader.ParseJson("""{ "widget": { "code": "ABC" } }""", options);
        var w = jdr.Deserialize<Widget>("widget", new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower });

        w.Should().NotBeNull();
        w!.CreatedOn.Should().NotBe(default(DateTimeOffset), "the 'created_on' standard property (snake_case) must have been applied and successfully bound");
    }

    [Test]
    public void RootNodePreProcessor_CustomHook_CanMutateRootMostObject()
    {
        var options = new JsonDataReaderOptions(JsonPropertyNamingConvention.CamelCase)
        {
            RootNodePreProcessor = args =>
            {
                if (args.CurrentNode is System.Text.Json.Nodes.JsonObject jo)
                    jo["code"] = "INJECTED";
            }
        };

        var jdr = JsonDataReader.ParseJson("""{ "widget": { "text": "A widget" } }""", options);
        var w = jdr.Deserialize<Widget>("widget");

        w.Should().NotBeNull();
        w!.Code.Should().Be("INJECTED", "a user-supplied RootNodePreProcessor must be able to mutate the root-most object before substitution/property application");
    }
}
