// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Data.Querying
{
    /// <summary>
    /// Provides the query order-by direction.
    /// </summary>
    [Flags]
    public enum QueryOrderByDirection
    {
        /// <summary>
        /// Ascending order.
        /// </summary>
        Ascending = 1,

        /// <summary>
        /// Descending order.
        /// </summary>
        Descending = 2,

        /// <summary>
        /// Both <see cref="Ascending"/> and <see cref="Descending"/> order.
        /// </summary>
        Both = Ascending | Descending
    }
}