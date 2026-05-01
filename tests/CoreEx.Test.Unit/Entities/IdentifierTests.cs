using CoreEx.Entities;
using CoreEx.Entities.Abstractions;

namespace CoreEx.Test.Unit.Entities;

[TestFixture]
public class IdentifierTests
{
    public class ImmutableIdentifier : IReadOnlyIdentifier<int>
    {
        public int Id { get; init; }
    }

    public class MutableIdentifier : IIdentifier<int>
    {
        public int Id { get; set; }
    }

    [Test]
    public void ImmutableIdentifier_Test()
    {
        var ii = new ImmutableIdentifier { Id = 123 };
        ii.Id.Should().Be(123);
        ((IReadOnlyIdentifier)ii).IdType.Should().Be<int>();
        ((IReadOnlyIdentifier)ii).IsIdReadOnly.Should().BeTrue();
        Action act = () => ((IReadOnlyIdentifier)ii).SetIdentifier(456);
        act.Should().Throw<InvalidOperationException>().WithMessage("Identifier is read-only.");
    }

    [Test]
    public void MutableIdentifier_Test()
    {
        var mi = new MutableIdentifier { Id = 123 };
        mi.Id.Should().Be(123);
        ((IReadOnlyIdentifier)mi).IdType.Should().Be<int>();
        ((IReadOnlyIdentifier)mi).IsIdReadOnly.Should().BeFalse();
        ((IReadOnlyIdentifier)mi).SetIdentifier(456);
        mi.Id.Should().Be(456);

        ((IIdentifier)mi).Id = 789;
        mi.Id.Should().Be(789);
    }
}