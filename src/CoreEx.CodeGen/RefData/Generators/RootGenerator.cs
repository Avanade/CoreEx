namespace CoreEx.CodeGen.RefData.Generators;

/// <summary>
/// Provides the root configuration code-generator.
/// </summary>
public class RootGenerator : CodeGeneratorBase<CodeGenConfig, CodeGenConfig>
{
    /// <inheritdoc/>
    protected override IEnumerable<CodeGenConfig> SelectGenConfig(CodeGenConfig config) => [config];
}