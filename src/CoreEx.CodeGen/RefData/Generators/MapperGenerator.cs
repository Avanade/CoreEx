namespace CoreEx.CodeGen.RefData.Generators;

/// <summary>
/// Provides the mappers code-generator.
/// </summary>
public class MapperGenerator : CodeGeneratorBase<CodeGenConfig, EntityConfig>
{
    /// <inheritdoc/>
    protected override IEnumerable<EntityConfig> SelectGenConfig(CodeGenConfig config) => config.Entities?.Where(e => !(e.ExcludeMapper ?? false)) ?? [];
}