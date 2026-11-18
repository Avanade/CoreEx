namespace CoreEx.CodeGen;

/// <summary>
/// Discovers source files under the solution directories used by CodeGen commands.
/// </summary>
internal static class CodeGenDirectoryWalker
{
    /// <summary>
    /// Gets the solution directory based on the CodeGen output directory.
    /// </summary>
    /// <param name="outputDirectory">The CodeGen output directory.</param>
    /// <returns>The solution directory.</returns>
    public static DirectoryInfo GetSolutionDirectory(DirectoryInfo outputDirectory)
    {
        var directory = outputDirectory.Parent ?? outputDirectory;
        return directory.Parent ?? directory;
    }

    /// <summary>
    /// Enumerates files recursively, excluding build-output and hidden directories.
    /// </summary>
    /// <param name="directory">The directory to enumerate.</param>
    /// <returns>The discovered files.</returns>
    public static IEnumerable<FileInfo> EnumerateFiles(DirectoryInfo directory)
    {
        foreach (var child in EnumerateIncludedDirectories(directory))
            foreach (var file in EnumerateFiles(child))
                yield return file;

        foreach (var file in directory.EnumerateFiles())
            yield return file;
    }

    /// <summary>
    /// Enumerates child directories, excluding build-output and hidden directories.
    /// </summary>
    /// <param name="directory">The directory to enumerate.</param>
    /// <returns>The included child directories.</returns>
    public static IEnumerable<DirectoryInfo> EnumerateIncludedDirectories(DirectoryInfo directory)
        => directory.EnumerateDirectories().Where(child => !child.Name.Equals("obj", StringComparison.InvariantCultureIgnoreCase)
            && !child.Name.Equals("bin", StringComparison.InvariantCultureIgnoreCase)
            && !child.Name.StartsWith('.'));
}
