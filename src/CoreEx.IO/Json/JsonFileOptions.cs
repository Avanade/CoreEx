namespace CoreEx.IO.Json;

/// <summary>Defines shared JSON member handling and parser depth, not document selection.</summary>
public sealed record JsonFileOptions
{
    /// <summary>Gets the unknown-member policy; defaults to Reject.</summary>
    public JsonUnknownMemberPolicy UnknownMemberPolicy { get; init; }

    /// <summary>Gets the maximum object/array nesting depth; defaults to 64.</summary>
    public int MaxDepth { get; init; } = 64;

    /// <summary>Checks configuration before opening input.</summary>
    internal void Validate()
    {
        MaxDepth.ThrowIfLessThanOrEqualToZero();
        if (!Enum.IsDefined(UnknownMemberPolicy))
            throw new ArgumentOutOfRangeException(nameof(UnknownMemberPolicy));
    }
}
