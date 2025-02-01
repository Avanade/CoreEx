// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;

namespace CoreEx
{
    /// <summary>
    /// Provides the system time in UTC; defaults to <see cref="DateTime.UtcNow"/>.
    /// </summary>
    public class SystemTime : ISystemTime
    {
        private DateTime? _time;

        /// <summary>
        /// Gets the default <see cref="SystemTime"/> instance which returns the current <see cref="DateTime.UtcNow"/>. 
        /// </summary>
        public static SystemTime Default { get; } = new SystemTime();

        /// <summary>
        /// Creates a <see cref="SystemTime"/> with a fixed specified <paramref name="time"/>.
        /// </summary>
        /// <param name="time">The fixed time (converted to UTC).</param>
        /// <returns>The fixed <see cref="SystemTime"/>.</returns>
        /// <remarks>This is generally intended for testing purposes.</remarks>
        public static SystemTime CreateFixed(DateTime time) => new() { _time = Cleaner.Clean(time, DateTimeTransform.DateTimeUtc) };

        /// <summary>
        /// Gets the <see cref="ISystemTime"/> instance from the <see cref="ExecutionContext.ServiceProvider"/> where configured; otherwise, returns a new instance of <see cref="SystemTime"/>.
        /// </summary>
        /// <returns>The <see cref="ISystemTime"/> using the <see cref="ExecutionContext.ServiceProvider"/> where configured; otherwise, a new instance of <see cref="SystemTime"/>.</returns>
        public static ISystemTime Get() => ExecutionContext.GetService<ISystemTime>() ?? new SystemTime();

        /// <summary>
        /// Gets the timestamp for the <see cref="ExecutionContext"/> lifetime; i.e (to enable consistent execution-related timestamping).
        /// </summary>
        /// <remarks>Where the <see cref="ExecutionContext.HasCurrent"/> then the <see cref="ExecutionContext.Current"/> <see cref="ExecutionContext.Timestamp"/> will be used; otherwise, will use <see cref="Get"/> <see cref="UtcNow"/> (passed through <see cref="Cleaner.Clean(DateTime)"/>).</remarks>
        public static DateTime Timestamp => ExecutionContext.HasCurrent ? ExecutionContext.Current.Timestamp : Cleaner.Clean(Get().UtcNow);

        /// <inheritdoc/>
        public DateTime UtcNow => _time ?? DateTime.UtcNow;
    }
}