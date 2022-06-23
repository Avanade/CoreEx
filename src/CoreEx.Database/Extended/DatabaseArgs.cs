// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;

namespace CoreEx.Database.Extended
{
    /// <summary>
    /// Provides the extended <see cref="IDatabase"/> arguments.
    /// </summary>
    public struct DatabaseArgs
    {
        /// <summary>
        /// Creates a <see cref="DatabaseArgs"/>.
        /// </summary>
        /// <param name="mapper">The <see cref="IDatabaseMapper"/>.</param>
        /// <param name="paging">The optional <see cref="PagingArgs"/>.</param>
        /// <returns>The <see cref="DatabaseArgs"/>.</returns>
        public static DatabaseArgs Create(IDatabaseMapper mapper, PagingArgs? paging = null) => new(mapper) { Paging = paging == null ? null : (paging is PagingResult pr ? pr : new PagingResult(paging)) };

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseArgs"/> struct.
        /// </summary>
        /// <param name="mapper">The <see cref="IDatabaseMapper"/>.</param>
        public DatabaseArgs(IDatabaseMapper mapper) => Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

        /// <summary>
        /// Gets the <see cref="IDatabaseMapper"/>.
        /// </summary>
        public IDatabaseMapper Mapper { get;}

        /// <summary>
        /// Gets or sets the <see cref="PagingResult"/> (where paging is required for a <b>query</b>).
        /// </summary>
        public PagingResult? Paging { get; set; } = null;

        /// <summary>
        /// Indicates whether the data should be refreshed (reselected where applicable) after a <b>save</b> operation (defaults to <c>true</c>).
        /// </summary>
        public bool Refresh { get; set; } = true;
    }
}