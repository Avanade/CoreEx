namespace CoreEx.IO.Test.Unit;

[TestFixture]
public class FileBaselineFixtureTests
{
    [Test]
    public void BaselineManifest_FixturesArePresentAndDiagnosticAnchorsRemainValid()
    {
        var directory = Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "baseline.json")));
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (var item in manifest.RootElement.EnumerateArray())
        {
            var name = item.GetProperty("file").GetString();
            name.Should().NotBeNullOrWhiteSpace();
            names.Add(name!).Should().BeTrue();
            var content = File.ReadAllText(Path.Combine(directory, name!));
            content.Should().EndWith("\n").And.NotEndWith("\n\n").And.NotContain("\r");
            var lines = content.Split('\n')[..^1];
            lines.Should().HaveCount(item.GetProperty("physicalLines").GetInt32());
            item.GetProperty("recordsRead").GetInt64().Should().BePositive();
            item.GetProperty("requirement").GetString().Should().NotBeNullOrWhiteSpace();
            var outcome = Enum.Parse<FileProcessingOutcome>(item.GetProperty("outcome").GetString()!);
            var rejected = outcome == FileProcessingOutcome.ContentFailed;
            item.GetProperty("unitsExecuted").GetInt64().Should().Be(rejected ? 0 : item.GetProperty("unitsValidated").GetInt64());

            var anchors = item.GetProperty("diagnosticLines").EnumerateArray().Select(x => x.GetInt32()).ToArray();
            anchors.Should().OnlyContain(x => x > 0 && x <= lines.Length);
            if (rejected)
                anchors.Should().NotBeEmpty();
            else
                anchors.Should().BeEmpty();
        }

        names.Should().HaveCount(5);
    }

    [Test]
    public void FlatFixture_PreservesMultilineRecordRatherThanEquatingLinesWithRecords()
    {
        var content = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "flat-valid.csv"));
        content.Should().Contain("1,\"Example, Inc\",2\n2,\"Two\nphysical lines\",0\n");
    }
}
