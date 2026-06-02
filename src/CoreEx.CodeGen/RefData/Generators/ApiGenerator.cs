namespace CoreEx.CodeGen.RefData.Generators;

/// <summary>
/// Provides the API code-generator.
/// </summary>
public class ApiGenerator : CodeGeneratorBase<CodeGenConfig, CodeGenConfig>
{
    /// <inheritdoc/>
    protected override IEnumerable<CodeGenConfig> SelectGenConfig(CodeGenConfig config) => (config.ApiDirectory?.Exists ?? false) ? [config] : [];
}