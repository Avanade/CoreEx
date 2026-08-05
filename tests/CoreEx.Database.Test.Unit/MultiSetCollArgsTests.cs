using CoreEx.Database.Extended;

namespace CoreEx.Database.Test.Unit;

[TestFixture]
public class MultiSetCollArgsTests
{
    [TestCase(0, 10)]  // Regression: previously threw for any valid (minimumRows <= maximumRows) combination.
    [TestCase(5, 5)]   // Equal bounds are valid.
    [TestCase(0, null)]
    public void Constructor_ValidBounds_DoesNotThrow(int minimumRows, int? maximumRows)
    {
        Action act = () => new TestMultiSetCollArgs(minimumRows, maximumRows);
        act.Should().NotThrow();
    }

    [TestCase(10, 5)]  // Regression: previously did not throw despite minimumRows > maximumRows.
    [TestCase(1, 0)]
    public void Constructor_InvalidBounds_Throws(int minimumRows, int? maximumRows)
    {
        Action act = () => new TestMultiSetCollArgs(minimumRows, maximumRows);
        act.Should().Throw<ArgumentException>().WithParameterName(nameof(maximumRows));
    }

    private sealed class TestMultiSetCollArgs(int minimumRows = 0, int? maximumRows = null, bool stopOnNull = false) : MultiSetCollArgs(minimumRows, maximumRows, stopOnNull)
    {
        public override void DatasetRecord(DatabaseRecord dr) { }
    }
}
