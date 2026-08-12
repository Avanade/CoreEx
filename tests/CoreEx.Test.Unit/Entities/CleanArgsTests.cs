using CoreEx.Entities;

namespace CoreEx.Test.Unit.Entities;

[TestFixture]
public class CleanArgsTests
{
    [Test]
    public void Default_Has_All_Options_False()
    {
        var args = CleanArgs.Default;
        args.CleanAndDefaultNested.Should().BeFalse();
        args.CleanAndDefaultRoot.Should().BeFalse();
    }

    [Test]
    public void Default_ParameterlessValue_Matches_Static_Default()
    {
        default(CleanArgs).Should().BeEquivalentTo(CleanArgs.Default);
    }

    [Test]
    public void Init_Sets_Properties()
    {
        var args = new CleanArgs { CleanAndDefaultNested = true, CleanAndDefaultRoot = true };
        args.CleanAndDefaultNested.Should().BeTrue();
        args.CleanAndDefaultRoot.Should().BeTrue();
    }
}
