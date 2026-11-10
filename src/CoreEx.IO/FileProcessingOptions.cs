namespace CoreEx.IO;

/// <summary>Defines format-neutral processing budgets.</summary>
public sealed record FileProcessingOptions
{
    /// <summary>Gets the maximum source bytes consumed per pass, including a one-byte overflow probe; defaults to 64 MiB.</summary>
    public long MaxInputBytes { get; init; } = 64 * 1024 * 1024;

    /// <summary>Gets the maximum candidate business units per file; defaults to 100,000.</summary>
    public long MaxUnits { get; init; } = 100_000;

    /// <summary>Gets the maximum ordinary diagnostics per file; defaults to 1,000.</summary>
    public long MaxDiagnostics { get; init; } = 1_000;
}
