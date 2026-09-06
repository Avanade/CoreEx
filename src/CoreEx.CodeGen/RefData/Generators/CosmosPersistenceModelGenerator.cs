namespace CoreEx.CodeGen.RefData.Generators;

/// <summary>
/// Provides the Cosmos persistence model code-generator.
/// </summary>
public class CosmosPersistenceModelGenerator : CodeGeneratorBase<CodeGenConfig, EntityConfig>
{
    /// <inheritdoc/>
    protected override IEnumerable<EntityConfig> SelectGenConfig(CodeGenConfig config) => config.CosmosPersistenceModels ?? [];
}
