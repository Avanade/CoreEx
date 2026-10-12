namespace CoreEx.CodeGen.RefData.Generators;

/// <summary>
/// Provides the mutability code-generator.
/// </summary>
public class RootMutableGenerator : CodeGeneratorBase<CodeGenConfig, CodeGenConfig>
{
    /// <inheritdoc/>
    protected override IEnumerable<CodeGenConfig> SelectGenConfig(CodeGenConfig config) => config.HasMutableEntities ? [config] : [];
}
