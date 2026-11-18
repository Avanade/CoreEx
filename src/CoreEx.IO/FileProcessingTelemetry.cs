namespace CoreEx.IO;

/// <summary>Exposes aggregate file-processing metrics without high-cardinality source dimensions.</summary>
public static class FileProcessingTelemetry
{
    /// <summary>Gets the meter name to subscribe to when exporting file-processing metrics.</summary>
    public const string MeterName = "CoreEx.IO";

    private static readonly Meter _meter = new(MeterName);
    private static readonly Counter<long> _files = _meter.CreateCounter<long>("file.files.processed", "{file}", "File attempts by outcome, including faults and cancellations.");

    /// <summary>Records one attempt with a bounded outcome dimension.</summary>
    internal static void RecordFile(string outcome) => _files.Add(1, new KeyValuePair<string, object?>("file.outcome", outcome));
}
