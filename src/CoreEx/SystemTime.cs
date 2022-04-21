// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx
{
    /// <summary>
    /// Provides the system time in UTC; defaults to <see cref="DateTime.UtcNow"/>.
    /// </summary>
    public class SystemTime : ISystemTime
    {
        /// <summary>
        /// Gets the default <see cref="SystemTime"/> instance. 
        /// </summary>
        public static SystemTime Default { get; } = new SystemTime();

        /// <inheritdoc/>
        public DateTime UtcNow => DateTime.UtcNow;
    }
}