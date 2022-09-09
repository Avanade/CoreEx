// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.EntityFrameworkCore
{
    /// <summary>
    /// Enables the common <b>Entity Framework</b> entity capabilities.
    /// </summary>
    public interface IEfDbEntity
    {
        /// <summary>
        /// Gets the owning <see cref="IEfDb"/>.
        /// </summary>
        IEfDb EfDb { get; }
    }
}