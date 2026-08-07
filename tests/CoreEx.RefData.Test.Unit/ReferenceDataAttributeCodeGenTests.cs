using CoreEx.Entities;
using CoreEx.Json;

namespace CoreEx.RefData.Test.Unit;

public partial class ReferenceDataAttributeCodeGenTests
{
    [Contract]
    internal partial class DummyEntityTextDisabled
    {
        [ReferenceData<ReferenceDataOrchestratorTests.DummyRefData>(Text = false)]
        public partial string? RefDataSid { get; set; }
    }

    [Contract]
    internal partial class DummyEntityTextCustomJsonName
    {
        [ReferenceData<ReferenceDataOrchestratorTests.DummyRefData>(TextJsonName = "customRefText")]
        public partial string? RefDataSid { get; set; }
    }

    [Test]
    public void ReferenceDataAttribute_TextFalse_OmitsTextProperty()
    {
        // Today (before the fix) this property always exists regardless of Text = false.
        typeof(DummyEntityTextDisabled).GetProperty("RefDataText").Should().BeNull();
        typeof(DummyEntityTextDisabled).GetProperty("RefDataSid").Should().NotBeNull();
    }

    [Test]
    public void ReferenceDataAttribute_TextJsonName_OverridesJsonPropertyName()
    {
        var entity = new DummyEntityTextCustomJsonName { RefDataSid = "A", RefDataText = "Some Text" };

        var json = System.Text.Json.JsonSerializer.Serialize(entity, JsonDefaults.SerializerOptions);

        json.Should().Contain("customRefText");
        json.Should().NotContain("refDataText");
    }
}
