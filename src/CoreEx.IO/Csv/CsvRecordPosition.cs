namespace CoreEx.IO.Csv;

/// <summary>Defines an unordered route's optional file-boundary restriction.</summary>
public enum CsvRecordPosition
{
    /// <summary>Any parsed record position.</summary>
    Any,
    /// <summary>The first parsed record, ignoring skipped blank and comment lines.</summary>
    First,
    /// <summary>The last parsed record; no parsed record may follow it.</summary>
    Last
}
