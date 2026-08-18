using CoreEx.Data;
using CoreEx.Schemas;

namespace CoreEx.Test.Unit.Data;

[TestFixture]
public class ModelTests
{
    [Test]
    public void PrepareTypeDiscriminator_ExplicitOverride_UsesGivenValue()
    {
        var model = new WithSchemaModel();
        Model.PrepareTypeDiscriminator(model, "Explicit");
        model.TypeDiscriminator.Should().Be("Explicit");
    }

    [Test]
    public void PrepareTypeDiscriminator_WithSchemaAttribute_UsesSchemaName()
    {
        var model = new WithSchemaModel();
        Model.PrepareTypeDiscriminator(model);
        model.TypeDiscriminator.Should().Be("CustomSchemaName");
    }

    [Test]
    public void PrepareTypeDiscriminator_NoSchemaAttribute_FallsBackToTypeName()
    {
        // Regression test: Schema.TryGetMetadata returns false when no [Schema] attribute is present, but its out-param is still
        // populated with a defaulted SchemaAttribute whose Name is the type name. PrepareTypeDiscriminator must use that default
        // rather than leaving TypeDiscriminator null, per the documented fallback on IReadOnlyTypeDiscriminator.TypeDiscriminator.
        var model = new NoSchemaModel();
        Model.PrepareTypeDiscriminator(model);
        model.TypeDiscriminator.Should().Be(nameof(NoSchemaModel));
    }

    [Test]
    public void PrepareTypeDiscriminator_ModelNotITypeDiscriminator_NoOp()
    {
        var model = new object();
        var result = Model.PrepareTypeDiscriminator(model);
        result.Should().BeSameAs(model);
    }

    [Test]
    public void PrepareTypeDiscriminator_NullModel_ReturnsNull()
    {
        Model.PrepareTypeDiscriminator<NoSchemaModel>(null).Should().BeNull();
    }

    [Schema(Name = "CustomSchemaName")]
    private class WithSchemaModel : ITypeDiscriminator
    {
        public string? TypeDiscriminator { get; set; }
    }

    private class NoSchemaModel : ITypeDiscriminator
    {
        public string? TypeDiscriminator { get; set; }
    }
}
