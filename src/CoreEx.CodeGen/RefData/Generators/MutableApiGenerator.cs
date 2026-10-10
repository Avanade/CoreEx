namespace CoreEx.CodeGen.RefData.Generators;

/// <summary>
/// Provides the API mutable code-generator.
/// </summary>
public class MutableApiGenerator : CodeGeneratorBase<CodeGenConfig, EntityConfig>
{
    /// <inheritdoc/>
    protected override IEnumerable<EntityConfig> SelectGenConfig(CodeGenConfig config) => (config.ApiDirectory?.Exists ?? false) ? config.EntitiesThatAreMutable?.Where(x => !(x.ExcludeApi ?? false)) ?? [] : [];
}
