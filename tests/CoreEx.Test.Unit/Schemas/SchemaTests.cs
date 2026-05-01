using CoreEx.Schemas;

namespace CoreEx.Test.Unit.Schemas;

[TestFixture]
public class SchemaTests
{
    [Test]
    public void DefaultVersion_Is_1_0()
    {
        Schema.DefaultVersion.Should().Be(new Version(1, 0));
    }

    [Test]
    public void DefaultVersionString_Is_1_0()
    {
        Schema.DefaultVersionString.Should().Be("1.0");
    }

    [Test]
    public void TryGetMetadata_Generic_FoundAndNotFound()
    {
        // With attribute
        var found = Schema.TryGetMetadata<WithSchema>(out var meta);
        found.Should().BeTrue();
        meta.Should().NotBeNull();
        meta.VersionString.Should().Be("2.1");
        meta.Name.Should().Be("WithSchema");

        // Without attribute
        var notFound = Schema.TryGetMetadata<NoSchema>(out var meta2);
        notFound.Should().BeFalse();
        meta2.Should().NotBeNull();
        meta2.Version.Should().Be(Schema.DefaultVersion);
        meta2.VersionString.Should().Be(Schema.DefaultVersionString);
        meta2.Name.Should().Be("NoSchema");
    }

    [Test]
    public void TryGetMetadata_ByType_FoundAndNotFound()
    {
        var withSchemaType = typeof(WithSchema);
        var noSchemaType = typeof(NoSchema);

        var found = Schema.TryGetMetadata(withSchemaType, out var meta);
        found.Should().BeTrue();
        meta.VersionString.Should().Be("2.1");
        meta.Name.Should().Be("WithSchema");

        var notFound = Schema.TryGetMetadata(noSchemaType, out var meta2);
        notFound.Should().BeFalse();
        meta2.Version.Should().Be(Schema.DefaultVersion);
        meta2.VersionString.Should().Be(Schema.DefaultVersionString);
        meta2.Name.Should().Be("NoSchema");
    }

    [Schema("2.1")]
    private class WithSchema { }

    private class NoSchema { }
}