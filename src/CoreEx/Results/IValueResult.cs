// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Results
{
    /// <summary>
    /// Enables the use of a <c>Result</c> type with a <see cref="Value"/> to represent the outcome of an operation.
    /// </summary>
    /// <typeparam name="T">The <see cref="Value"/> <see cref="Type"/>.</typeparam>
    public interface IResultValue<out T> : IResult
    {
        /// <summary>
        /// Gets the underlying value.
        /// </summary>
        T Value { get; }
    }
}