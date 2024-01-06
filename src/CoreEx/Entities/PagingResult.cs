// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Drawing;
using System.Text.Json.Serialization;

namespace CoreEx.Entities
{
    /// <summary>
    /// Represents the resulting paging response including <see cref="TotalCount"/> and <see cref="TotalPages"/> where applicable for the subsequent query.
    /// </summary>
    [System.Diagnostics.DebuggerStepThrough]
    public class PagingResult : PagingArgs
    {
        /// <summary>
        /// Creates a <see cref="PagingResult"/> for a specified page number and size.
        /// </summary>
        /// <param name="page">The <see cref="PagingArgs.Page"/> number.</param>
        /// <param name="size">The page <see cref="Size"/> (defaults to <see cref="PagingArgs.DefaultTake"/>).</param>
        /// <param name="isGetCount">Indicates whether to get the total count (see <see cref="TotalCount"/>) when performing the underlying query (defaults to <see cref="PagingArgs.DefaultIsGetCount"/> where <c>null</c>).</param>
        /// <returns>The <see cref="PagingResult"/>.</returns>
        public static new PagingResult CreatePageAndSize(long page, long? size = null, bool? isGetCount = null) => new(PagingArgs.CreatePageAndSize(page, size, isGetCount));

        /// <summary>
        /// Creates a <see cref="PagingResult"/> for a specified skip and take.
        /// </summary>
        /// <param name="skip">The <see cref="PagingArgs.Skip"/> value.</param>
        /// <param name="take">The <see cref="PagingArgs.Take"/> value (defaults to <see cref="PagingArgs.DefaultTake"/>).</param>
        /// <param name="isGetCount">Indicates whether to get the total count (see <see cref="TotalCount"/>) when performing the underlying query (defaults to <see cref="PagingArgs.DefaultIsGetCount"/> where <c>null</c>).</param>
        /// <returns>The <see cref="PagingResult"/>.</returns>
        public static new PagingResult CreateSkipAndTake(long skip, long? take = null, bool? isGetCount = null) => new(PagingArgs.CreateSkipAndTake(skip, take, isGetCount));

        /// <summary>
        /// Creates a <see cref="PagingResult"/> for a specified token and take.
        /// </summary>
        /// <param name="token">The <see cref="PagingArgs.Token"/> to use to get the next page of elements.</param>
        /// <param name="take">The <see cref="PagingArgs.Take"/> value (defaults to <see cref="PagingArgs.DefaultTake"/>).</param>
        /// <param name="isGetCount">Indicates whether to get the total count (see <see cref="PagingResult.TotalCount"/>) when performing the underlying query (defaults to <see cref="PagingArgs.DefaultIsGetCount"/> where <c>null</c>).</param>
        /// <returns>The <see cref="PagingResult"/>.</returns>
        public static new PagingResult CreateTokenAndTake(string token, long? take = null, bool? isGetCount = null) => new(PagingArgs.CreateTokenAndTake(token, take, isGetCount));

        /// <summary>
        /// Initializes a new instance of the <see cref="PagingResult"/> class from a <paramref name="pagingArgs"/> and optional <paramref name="totalCount"/>.
        /// </summary>
        /// <param name="pagingArgs">The <see cref="PagingArgs"/>.</param>
        /// <param name="totalCount">The total record count where applicable.</param>
        /// <remarks>Where the <paramref name="pagingArgs"/> and <paramref name="totalCount"/> are both provided the <see cref="TotalPages"/> will be automatically created.</remarks>
        public PagingResult(PagingArgs? pagingArgs = null, long? totalCount = null)
        {
            pagingArgs ??= new PagingArgs();

            Skip = pagingArgs.Skip;
            Take = pagingArgs.Take;
            Page = pagingArgs.Page;
            Token = pagingArgs.Token;
            IsGetCount = pagingArgs.IsGetCount;
            TotalCount = (totalCount.HasValue && totalCount.Value < 0) ? null : totalCount;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PagingResult"/> class from a <see cref="PagingResult"/> (copies values).
        /// </summary>
        /// <param name="pagingResult">The <see cref="PagingResult"/>.</param>
        public PagingResult(PagingResult pagingResult) : this(pagingResult, pagingResult?.TotalCount) { }

        /// <summary>
        /// Gets or sets the total count of the elements in the sequence (a <c>null</c> value indicates that the total count is unknown).
        /// </summary>
        public long? TotalCount { get; set; }

        /// <summary>
        /// Gets the calculated total pages for all elements in the sequence where the <see cref="PagingArgs.Option"/> is equal to <see cref="PagingOption.PageAndSize"/>.
        /// </summary>
        [JsonIgnore()]
        public long? TotalPages => Option == PagingOption.PageAndSize && TotalCount.HasValue ? (long)System.Math.Ceiling(TotalCount.Value / (double)Take) : null;
    }
}