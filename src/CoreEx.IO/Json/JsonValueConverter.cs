namespace CoreEx.IO.Json;

/// <summary>Converts a non-null native JSON token without exception-based content failures.</summary>
/// <typeparam name="TValue">The converted scalar type.</typeparam>
/// <param name="element">The non-null JSON token.</param>
/// <param name="value">The converted value.</param>
/// <returns>Whether the token matches the required scalar representation.</returns>
public delegate bool JsonValueConverter<TValue>(JsonElement element, out TValue value);
