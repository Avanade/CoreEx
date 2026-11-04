namespace CoreEx.CodeGen.Files.Generators;

/// <summary>Selects the prepared File definition for definition-wide artifacts.</summary>
public class RootGenerator : CodeGeneratorBase<Config.CodeGenConfig, Config.CodeGenConfig>
{
    /// <inheritdoc/>
    protected override IEnumerable<Config.CodeGenConfig> SelectGenConfig(Config.CodeGenConfig config) => [config];
}
