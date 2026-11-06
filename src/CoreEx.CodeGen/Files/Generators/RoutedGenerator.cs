namespace CoreEx.CodeGen.Files.Generators;

/// <summary>Selects only unordered routed definitions for common record and typed handler-dispatch artifacts.</summary>
public class RoutedGenerator : CodeGeneratorBase<Config.CodeGenConfig, Config.CodeGenConfig>
{
    /// <inheritdoc/>
    protected override IEnumerable<Config.CodeGenConfig> SelectGenConfig(Config.CodeGenConfig config) => config.IsUnordered ? [config] : [];
}
