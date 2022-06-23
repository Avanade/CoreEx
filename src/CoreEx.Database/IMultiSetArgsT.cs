// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Database
{
    /// <summary>
    /// Enables the <b>Database</b> multi-set arguments with a <see cref="Mapper"/>.
    /// </summary>
    /// <typeparam name="T">The item <see cref="Type"/>.</typeparam>
    public interface IMultiSetArgs<T> : IMultiSetArgs where T : class, new()
    {
        /// <summary>
        /// Gets the <see cref="IDatabaseMapper{TItem}"/> for the <see cref="DatabaseRecord"/>.
        /// </summary>
        IDatabaseMapper<T> Mapper { get; }
    }
}