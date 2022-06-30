// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Collections.Generic;
using System.Linq;

namespace CoreEx.Database
{
    /// <summary>
    /// Provides <see cref="IMultiSetArgs"/> helpers.
    /// </summary>
    public static class MultiSetArgs
    {
        /// <summary>
        /// Creates an <see cref="IMultiSetArgs"/> <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <param name="args">The <see cref="IMultiSetArgs"/> arguments.</param>
        /// <returns>The <see cref="IEnumerable{T}"/> <see cref="IMultiSetArgs"/>.</returns>
        public static IEnumerable<IMultiSetArgs> Create(params IMultiSetArgs[] args) => args.AsEnumerable();
    }
}