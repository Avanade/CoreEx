namespace app-name.Functions;

/// <summary>
/// Provides the Functions worker entry point.
/// </summary>
public class Program
{
    /// <summary>
    /// Builds and runs the Functions worker.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    private static void Main(string[] args)
    {
        var builder = FunctionsApplication.CreateBuilder(args);
        builder.AddHostSettings();
        builder.Services.AddExecutionContext();
        builder.UseCoreExExecutionContext();
        builder.Build().Run();
    }
}
