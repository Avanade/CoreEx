namespace CoreEx.CodeGen.Files.Generators;

/// <summary>Selects each typed wire record for separate model, mapping, and validator files.</summary>
public class RecordGenerator : CodeGeneratorBase<Config.CodeGenConfig, Config.RecordConfig>
{
    /// <inheritdoc/>
    protected override IEnumerable<Config.RecordConfig> SelectGenConfig(Config.CodeGenConfig config) => config.Records ?? [];
}
