namespace CoreEx.Data.GraphQL.Test.Unit;

[TestFixture]
public class GraphQLLiteArgsTests
{
    [Test]
    public void GetIdentifier_ExactTypeMatch_ReturnsValue()
    {
        var args = new GraphQLLiteArgs(new Dictionary<string, object?> { ["id"] = 2 });
        args.GetIdentifier<int>().Should().Be(2);
    }

    [Test]
    public void GetIdentifier_LongBoxedForIntTarget_Converts()
    {
        // Regression: a variable-supplied Int argument (from a real JSON request body) boxes as `long` (GraphQLValueConverter.FromJsonElement always tries TryGetInt64
        // first, regardless of magnitude), while a literal Int argument boxes as `int` - GetIdentifier<int>() previously did a strict `is TId` cast with no widening,
        // failing for the identical logical id whenever it arrived via a variable.
        var args = new GraphQLLiteArgs(new Dictionary<string, object?> { ["id"] = 2L });
        args.GetIdentifier<int>().Should().Be(2);
    }

    [Test]
    public void GetIdentifier_IntBoxedForLongTarget_Converts()
    {
        var args = new GraphQLLiteArgs(new Dictionary<string, object?> { ["id"] = 2 });
        args.GetIdentifier<long>().Should().Be(2L);
    }

    [Test]
    public void GetIdentifier_StringBoxedForGuidTarget_Converts()
    {
        // Regression: there is no native GraphQL Guid scalar, so a Guid identifier always arrives boxed as a string - GetIdentifier<Guid>() was previously impossible to
        // satisfy at all (a boxed string is never a Guid via a strict `is` cast).
        var guid = Guid.NewGuid();
        var args = new GraphQLLiteArgs(new Dictionary<string, object?> { ["id"] = guid.ToString() });
        args.GetIdentifier<Guid>().Should().Be(guid);
    }

    [Test]
    public void GetIdentifier_StringExactMatch_ReturnsValue()
    {
        var args = new GraphQLLiteArgs(new Dictionary<string, object?> { ["id"] = "ABC123" });
        args.GetIdentifier<string>().Should().Be("ABC123");
    }

    [Test]
    public void GetIdentifier_Missing_Throws()
    {
        var args = new GraphQLLiteArgs(new Dictionary<string, object?>());
        var act = () => args.GetIdentifier<int>();
        act.Should().Throw<ArgumentException>().WithMessage("*required*");
    }

    [Test]
    public void GetIdentifier_Null_Throws()
    {
        var args = new GraphQLLiteArgs(new Dictionary<string, object?> { ["id"] = null });
        var act = () => args.GetIdentifier<int>();
        act.Should().Throw<ArgumentException>().WithMessage("*required*");
    }

    [Test]
    public void GetIdentifier_EmptyString_Throws()
    {
        var args = new GraphQLLiteArgs(new Dictionary<string, object?> { ["id"] = "" });
        var act = () => args.GetIdentifier<string>();
        act.Should().Throw<ArgumentException>().WithMessage("*non-empty*");
    }

    [Test]
    public void GetIdentifier_NotConvertible_Throws()
    {
        var args = new GraphQLLiteArgs(new Dictionary<string, object?> { ["id"] = "not-a-number" });
        var act = () => args.GetIdentifier<int>();
        act.Should().Throw<ArgumentException>().WithMessage("*must be of type Int32*");
    }
}
