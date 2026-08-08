namespace CoreEx.UnitTesting.Test.Unit;

public class UnitTestAssertionsTests
{
    [Test]
    public void BeJson_ValidJson_Succeeds()
        => """{ "name": "value" }""".Should().BeJson();

    [Test]
    public void BeJson_InvalidJson_Fails()
    {
        Action act = () => "not json".Should().BeJson();
        act.Should().Throw<Exception>();
    }

    [Test]
    public void BeJson_Null_Fails()
    {
        string? json = null;
        Action act = () => json.Should().BeJson();
        act.Should().Throw<Exception>();
    }

    [Test]
    public void ContainAll_AllPathsPresent_Succeeds()
        => """{ "a": { "b": 1 }, "c": 2 }""".Should().BeJson().ContainAll(["$.a.b", "$.c"]);

    [Test]
    public void ContainAll_MissingPath_Fails()
    {
        Action act = () => """{ "a": 1 }""".Should().BeJson().ContainAll(["$.missing"]);
        act.Should().Throw<Exception>();
    }

    [Test]
    public void NotContainAny_NoPathsPresent_Succeeds()
        => """{ "a": 1 }""".Should().BeJson().NotContainAny(["$.b", "$.c"]);

    [Test]
    public void NotContainAny_PathPresent_Fails()
    {
        Action act = () => """{ "a": 1 }""".Should().BeJson().NotContainAny(["$.a"]);
        act.Should().Throw<Exception>();
    }

    [Test]
    public void HavePath_Exists_ReturnsNode()
    {
        var node = """{ "a": { "b": 42 } }""".Should().BeJson().HavePath("$.a.b");
        node.GetValue<int>().Should().Be(42);
    }

    [Test]
    public void HavePath_DoesNotExist_Fails()
    {
        Action act = () => """{ "a": 1 }""".Should().BeJson().HavePath("$.missing");
        act.Should().Throw<Exception>();
    }
}
