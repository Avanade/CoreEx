namespace CoreEx.IO.JsonLines;

/// <summary>Defines JSONL framing and optional explicit direct-member routing.</summary>
public sealed record JsonLinesFileOptions
{
    /// <summary>Gets shared JSON mapping and syntax settings.</summary>
    public CoreEx.IO.Json.JsonFileOptions Json { get; init; } = new();

    /// <summary>Gets the direct string discriminator member, or null for ordinary object units.</summary>
    public string? Discriminator { get; init; }

    /// <summary>Gets the blank-line policy; defaults to Reject.</summary>
    public JsonLinesBlankLinePolicy BlankLinePolicy { get; init; }
}
