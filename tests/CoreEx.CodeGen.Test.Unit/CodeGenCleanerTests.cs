namespace CoreEx.CodeGen.Test.Unit;

[TestFixture]
public class CodeGenCleanerTests
{
    private string _directory = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), "CoreEx.CodeGen.Clean." + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);
    }

    [TestCase(false, false)]
    [TestCase(true, true)]
    public async Task CleanRemovesGeneratedFilesAndPreservesExcludedFiles(bool simulation, bool expectGeneratedFiles)
    {
        var codegenDirectory = Directory.CreateDirectory(Path.Combine(_directory, "tools", "CodeGen"));
        var sourceDirectory = Directory.CreateDirectory(Path.Combine(_directory, "src"));
        var generatedFile = Path.Combine(sourceDirectory.FullName, "Thing.g.cs");
        var generatedOtherExtension = Path.Combine(sourceDirectory.FullName, "Thing.g.txt");
        var handAuthoredFile = Path.Combine(sourceDirectory.FullName, "Thing.cs");
        var binFile = Path.Combine(Directory.CreateDirectory(Path.Combine(sourceDirectory.FullName, "bin")).FullName, "Build.g.cs");
        var hiddenFile = Path.Combine(Directory.CreateDirectory(Path.Combine(_directory, ".hidden")).FullName, "Hidden.g.cs");

        System.IO.File.WriteAllText(generatedFile, "generated");
        System.IO.File.WriteAllText(generatedOtherExtension, "generated");
        System.IO.File.WriteAllText(handAuthoredFile, "hand-authored");
        System.IO.File.WriteAllText(binFile, "build output");
        System.IO.File.WriteAllText(hiddenFile, "hidden");

        var console = CodeGenConsole.Create();
        console.Args.OutputDirectory = codegenDirectory;
        string[] command = simulation ? ["clean", "--simulation"] : ["clean"];

        (await console.RunAsync(command)).Should().Be(0);

        System.IO.File.Exists(generatedFile).Should().Be(expectGeneratedFiles);
        System.IO.File.Exists(generatedOtherExtension).Should().Be(expectGeneratedFiles);
        System.IO.File.Exists(handAuthoredFile).Should().BeTrue();
        System.IO.File.Exists(binFile).Should().BeTrue();
        System.IO.File.Exists(hiddenFile).Should().BeTrue();
    }
}
