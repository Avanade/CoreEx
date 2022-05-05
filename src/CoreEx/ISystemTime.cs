// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx
{
    /// <summary>
    /// Enables the specification/overrinding of the system time.
    /// </summary>
    public interface ISystemTime
    {
        /// <summary>
        /// Gets the current system time in UTC.
        /// </summary>
        DateTime UtcNow { get; }
    }
}