using CoreEx.Mapping;

namespace CoreEx.Test.Unit.Mapping;

public class MapperTests
{
    private class Source { public int Value { get; set; } }
    private class Destination { public int Value { get; set; } }

    private class TestMapper : Mapper<Source, Destination>
    {
        protected override Destination OnMap(Source source) => new()
        {
            Value = source.Value
        };
    }

    [Test]
    public void SourceType_And_DestinationType_AreCorrect()
    {
        var mapper = new TestMapper();
        ((IMapperBase)mapper).SourceType.Should().Be<Source>();
        ((IMapperBase)mapper).DestinationType.Should().Be<Destination>();
    }

    [Test]
    public void Map_NullSource_EqualsNull()
    {
        var mapper = new TestMapper();
        mapper.Map(null).Should().BeNull();
    }

    [Test]
    public void Map_MapsValue()
    {
        var mapper = new TestMapper();
        var src = new Source { Value = 42 };
        var dest = mapper.Map(src);
        dest.Value.Should().Be(42);
    }
}