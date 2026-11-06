namespace CoreEx.IO.Csv;

/// <summary>Holds one typed projected record and its concrete validator dispatch, never file-wide history.</summary>
/// <typeparam name="TUnit">The common projected unit type.</typeparam>
/// <param name="Value">The projected value, or null on mapping failure.</param>
/// <param name="Diagnostics">The current mapping diagnostics.</param>
/// <param name="ValidateAsync">The concrete wire-record validation callback.</param>
internal sealed record CsvRoutedInput<TUnit>(TUnit? Value, IReadOnlyList<FileDiagnostic> Diagnostics,
    Func<long, Func<FileDiagnostic, CancellationToken, Task>, CancellationToken, Task> ValidateAsync) where TUnit : class;
