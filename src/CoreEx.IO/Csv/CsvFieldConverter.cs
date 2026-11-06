namespace CoreEx.IO.Csv;

/// <summary>Tries to convert an unquoted, unescaped field without exceptions for invalid content.</summary>
/// <typeparam name="TValue">The converted value type.</typeparam>
/// <param name="text">The decoded field, valid only during the call.</param>
/// <param name="value">The converted value; ignored when conversion fails.</param>
/// <returns>Whether conversion succeeded. Unexpected converter faults propagate.</returns>
public delegate bool CsvFieldConverter<TValue>(ReadOnlySpan<char> text, out TValue value);
