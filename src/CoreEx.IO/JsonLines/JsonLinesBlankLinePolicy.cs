namespace CoreEx.IO.JsonLines;

/// <summary>Specifies whether empty or JSON-whitespace-only physical lines are rejected or skipped.</summary>
public enum JsonLinesBlankLinePolicy
{
    /// <summary>Counts and rejects a blank source record.</summary>
    Reject,

    /// <summary>Skips blank lines without changing record/unit counts.</summary>
    Skip
}
