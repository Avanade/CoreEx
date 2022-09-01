using Pulumi;

public static class Extensions
{
    public static Input<string> GetConfigValue(string name, Input<string> defaultValue)
    {
        var config = new Config();

        var configValue = config.Get(name);

        if (string.IsNullOrEmpty(configValue))
        {
            Log.Info($"Defaulting {name} because it wasn't present in configuration");
            return defaultValue;
        }
        else
        {
            return configValue!;
        }
    }
}