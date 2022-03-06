// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Entities
{
    /// <summary>
    /// Provides a <see cref="Paging"/> <see cref="PagingResult"/>.
    /// </summary>
    public interface IPagingResult
    {
        /// <summary>
        /// Gets or sets the <see cref="PagingResult"/>.
        /// </summary>
        PagingResult? Paging { get; set; }
    }
}