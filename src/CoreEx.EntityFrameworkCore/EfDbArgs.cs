// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoreEx.EntityFrameworkCore
{
    /// <summary>
    /// Provides the extended <b>Entity Framework</b> arguments.
    /// </summary>
    public struct EfDbArgs
    {
        /// <summary>
        /// Creates an <see cref="EfDbArgs"/>.
        /// </summary>
        /// <param name="paging">The optional <see cref="PagingArgs"/>.</param>
        /// <returns>The <see cref="EfDbArgs"/>.</returns>
        public static EfDbArgs Create(PagingArgs? paging = null) => new() { Paging = paging == null ? null : (paging is PagingResult pr ? pr : new PagingResult(paging)) };

        /// <summary>
        /// Initializes a new instance of the <see cref="EfDbArgs"/> struct.
        /// </summary>
        public EfDbArgs() { }

        /// <summary>
        /// Gets or sets the <see cref="PagingResult"/> (where paging is required for a <b>query</b>).
        /// </summary>
        public PagingResult? Paging { get; set; } = null;

        /// <summary>
        /// Indicates that the underlying <see cref="DbContext"/> <see cref="DbContext.SaveChanges()"/> is to be performed automatically (defauls to <c>true</c>);
        /// </summary>
        public bool SaveChanges { get; set; } = true;

        /// <summary>
        /// Indicates whether the data should be refreshed (reselected where applicable) after a <b>save</b> operation (defaults to <c>true</c>); is dependent on <see cref="SaveChanges"/> being performed.
        /// </summary>
        public bool Refresh { get; set; } = true;
    }
}