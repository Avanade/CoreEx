namespace CoreEx.Gen.Utility;

/// <summary>
/// Enables access to the pluralization services.
/// </summary>
internal static class Pluralizer
{
    /// <summary>
    /// Gets the singleton <see cref="Pluralize.NET.IPluralize"/> instance.
    /// </summary>
    public static Pluralize.NET.IPluralize Instance { get; } = new Pluralize.NET.Pluralizer();
}