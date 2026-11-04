namespace CoreEx.CodeGen.Files.Generators;

/// <summary>Selects non-leaf business positions for assembled group representations.</summary>
public class GroupGenerator : CodeGeneratorBase<Config.CodeGenConfig, Config.NodeConfig>
{
    /// <inheritdoc/>
    protected override IEnumerable<Config.NodeConfig> SelectGenConfig(Config.CodeGenConfig config) => config.Groups;
}
