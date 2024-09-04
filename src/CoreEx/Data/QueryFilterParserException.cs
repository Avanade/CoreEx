// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Data
{
    /// <summary>
    /// Represents a <see cref="QueryFilterParser"/> <see cref="ValidationException"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    public class QueryFilterParserException(string message) : ValidationException(message) { }
}