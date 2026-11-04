namespace CoreEx.IO.Csv;

/// <summary>Exposes aggregate CSV processing metrics without high-cardinality source dimensions.</summary>
public static class CsvProcessingTelemetry
{
    /// <summary>Gets the meter name to subscribe to when exporting CSV metrics.</summary>
    public const string MeterName = "CoreEx.IO.Csv";

    private static readonly Meter _meter = new(MeterName);
    private static readonly Counter<long> _files = _meter.CreateCounter<long>("csv.files.processed", "{file}", "File attempts by outcome, including faults and cancellations.");

    /// <summary>Records one attempt with a bounded outcome dimension.</summary>
    internal static void RecordFile(string outcome) => _files.Add(1, new KeyValuePair<string, object?>("csv.outcome", outcome));
}
