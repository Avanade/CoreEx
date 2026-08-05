using CoreEx.Entities;
using CoreEx.Mapping;

namespace CoreEx.Test.Unit.Mapping;

[TestFixture]
public class BiDirectionMapperTests
{
    private class Person : IIdentifier<Guid>, IETag
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? ETag { get; set; }
    }

    private class PersonDto : IIdentifier<Guid>, IETag
    {
        public Guid Id { get; set; }
        public string? FullName { get; set; }
        public string? ETag { get; set; }
    }

    private class TestBiDirectionMapper : BiDirectionMapper<Person, PersonDto>
    {
        protected override PersonDto OnMap(Person source) => new() { FullName = source.Name };
        protected override Person OnMap(PersonDto source) => new() { Name = source.FullName };
    }

    [Test]
    public void To_MapsCustomAndStandardProperties()
    {
        var mapper = new TestBiDirectionMapper();
        var person = new Person { Id = Guid.NewGuid(), Name = "Bob", ETag = "etag1" };

        var dto = mapper.To.Map(person);

        dto!.FullName.Should().Be("Bob");
        dto.Id.Should().Be(person.Id);
        dto.ETag.Should().Be("etag1");
    }

    [Test]
    public void From_MapsCustomAndStandardProperties()
    {
        var mapper = new TestBiDirectionMapper();
        var dto = new PersonDto { Id = Guid.NewGuid(), FullName = "Alice", ETag = "etag2" };

        var person = mapper.From.Map(dto);

        person!.Name.Should().Be("Alice");
        person.Id.Should().Be(dto.Id);
        person.ETag.Should().Be("etag2");
    }

    [Test]
    public void To_NullSource_ReturnsNull()
    {
        var mapper = new TestBiDirectionMapper();
        mapper.To.Map(null).Should().BeNull();
    }

    [Test]
    public void From_NullSource_ReturnsNull()
    {
        var mapper = new TestBiDirectionMapper();
        mapper.From.Map(null).Should().BeNull();
    }

    [Test]
    public void To_NonGenericMapperBridge_MapsValue()
    {
        var mapper = new TestBiDirectionMapper();
        var person = new Person { Id = Guid.NewGuid(), Name = "Charlie" };

        var result = ((IMapper)mapper.To).Map(person);

        result.Should().BeOfType<PersonDto>();
        ((PersonDto)result!).FullName.Should().Be("Charlie");
    }

    [Test]
    public void SourceAndDestinationTypes_AreCorrect()
    {
        var mapper = new TestBiDirectionMapper();
        ((IMapperBase)mapper.To).SourceType.Should().Be<Person>();
        ((IMapperBase)mapper.To).DestinationType.Should().Be<PersonDto>();
        ((IMapperBase)mapper.From).SourceType.Should().Be<PersonDto>();
        ((IMapperBase)mapper.From).DestinationType.Should().Be<Person>();
    }
}
