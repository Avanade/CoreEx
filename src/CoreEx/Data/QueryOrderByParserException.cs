// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Data
{
    /// <summary>
    /// Represents a <see cref="QueryOrderByParser"/> <see cref="ValidationException"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    public class QueryOrderByParserException(string message) : ValidationException(message) { }
}