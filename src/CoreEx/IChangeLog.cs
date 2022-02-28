// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx
{
    /// <summary>
    /// Provides the <see cref="ChangeLog"/> audit.
    /// </summary>
    public interface IChangeLog
    {
        /// <summary>
        /// Gets or set the <see cref="ChangeLog"/>.
        /// </summary>
        public ChangeLog? ChangeLog { get; set; }
    }
}