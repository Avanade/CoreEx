using CoreEx.Entities;
using CoreEx.Mapping;

namespace CoreEx.Test.Unit.Mapping;

[TestFixture]
public class IntoMapperTests
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

    private class TestIntoMapper : IntoMapper<Person, PersonDto>
    {
        protected override void OnMapInto(Person source, PersonDto destination) => destination.FullName = source.Name;
    }

    private class NoStandardIntoMapper : IntoMapper<Person, PersonDto>
    {
        protected override bool UseMapStandardInto => false;

        protected override void OnMapInto(Person source, PersonDto destination) => destination.FullName = source.Name;
    }

    [Test]
    public void MapInto_MapsCustomAndStandardProperties()
    {
        var mapper = new TestIntoMapper();
        var person = new Person { Id = Guid.NewGuid(), Name = "Bob", ETag = "etag1" };
        var dto = new PersonDto();

        mapper.MapInto(person, dto);

        dto.FullName.Should().Be("Bob");
        dto.Id.Should().Be(person.Id);
        dto.ETag.Should().Be("etag1");
    }

    [Test]
    public void MapInto_UseMapStandardIntoFalse_SkipsStandardProperties()
    {
        var mapper = new NoStandardIntoMapper();
        var person = new Person { Id = Guid.NewGuid(), Name = "Bob", ETag = "etag1" };
        var dto = new PersonDto();

        mapper.MapInto(person, dto);

        dto.FullName.Should().Be("Bob");
        dto.Id.Should().Be(Guid.Empty);
        dto.ETag.Should().BeNull();
    }

    [Test]
    public void MapInto_NullSource_Throws()
    {
        var mapper = new TestIntoMapper();
        Action act = () => mapper.MapInto(null!, new PersonDto());
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void MapInto_NullDestination_Throws()
    {
        var mapper = new TestIntoMapper();
        Action act = () => mapper.MapInto(new Person(), null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void NonGenericMapperBridge_MapsValue()
    {
        var mapper = new TestIntoMapper();
        var person = new Person { Id = Guid.NewGuid(), Name = "Charlie" };
        var dto = new PersonDto();

        ((IIntoMapper)mapper).MapInto(person, dto);

        dto.FullName.Should().Be("Charlie");
    }

    [Test]
    public void SourceAndDestinationTypes_AreCorrect()
    {
        var mapper = new TestIntoMapper();
        ((IMapperBase)mapper).SourceType.Should().Be<Person>();
        ((IMapperBase)mapper).DestinationType.Should().Be<PersonDto>();
    }

    [Test]
    public void MapperCreateInto_CreatesOneOffIntoMapper()
    {
        var mapper = Mapper.CreateInto<Person, PersonDto>((s, d) => d.FullName = s.Name);
        var person = new Person { Name = "Dana" };
        var dto = new PersonDto();

        mapper.MapInto(person, dto);

        dto.FullName.Should().Be("Dana");
    }
}
