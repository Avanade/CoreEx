using CoreEx.UnitTesting.Data;

namespace CoreEx.UnitTesting.Test.Unit;

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
}
