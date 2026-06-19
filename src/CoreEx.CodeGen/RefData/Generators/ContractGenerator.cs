namespace CoreEx.CodeGen.RefData.Generators;

/// <summary>
/// Provides the contracts code-generator.
/// </summary>
public class ContractGenerator : CodeGeneratorBase<CodeGenConfig, EntityConfig>
{
    /// <inheritdoc/>
    protected override IEnumerable<EntityConfig> SelectGenConfig(CodeGenConfig config) => config.Entities ?? [];
}