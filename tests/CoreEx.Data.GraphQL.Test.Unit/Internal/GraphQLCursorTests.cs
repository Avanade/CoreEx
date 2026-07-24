using CoreEx.Data.GraphQL.Internal;
using System.Text;

namespace CoreEx.Data.GraphQL.Test.Unit.Internal;

[TestFixture]
public class GraphQLCursorTests
{
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(42)]
    [TestCase(int.MaxValue)]
    public void Encode_TryDecode_RoundTrips(int offset)
    {
        var cursor = GraphQLCursor.Encode(offset);
        GraphQLCursor.TryDecode(cursor, out var decoded).Should().BeTrue();
        decoded.Should().Be(offset);
    }

    [Test]
    public void TryDecode_InvalidBase64_ReturnsFalse() => GraphQLCursor.TryDecode("not-a-valid-cursor", out _).Should().BeFalse();

    [Test]
    public void TryDecode_ValidBase64ButWrongPrefix_ReturnsFalse()
    {
        var cursor = Convert.ToBase64String(Encoding.UTF8.GetBytes("bogus:1"));
        GraphQLCursor.TryDecode(cursor, out _).Should().BeFalse();
    }

    [Test]
    public void TryDecode_NegativeOffset_ReturnsFalse()
    {
        var cursor = Convert.ToBase64String(Encoding.UTF8.GetBytes("offset:-1"));
        GraphQLCursor.TryDecode(cursor, out _).Should().BeFalse();
    }

    [Test]
    public void Encode_DifferentOffsets_ProduceDifferentCursors() => GraphQLCursor.Encode(1).Should().NotBe(GraphQLCursor.Encode(2));
}
