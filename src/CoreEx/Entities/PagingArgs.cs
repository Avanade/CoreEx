// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using System;
using System.Text.Json.Serialization;

namespace CoreEx.Entities
{
    /// <summary>
    /// Represents position-based paging being a) <see cref="Page"/> and <see cref="Size"/>, b) <see cref="Skip"/> and <see cref="Take"/>, or c) <see cref="Token"/> and <see cref="Take"/>. The <see cref="DefaultTake"/> and <see cref="MaxTake"/> (and <see cref="DefaultIsGetCount"/>) 
    /// are static settings to encourage page-size consistency, as well as limit the maximum value possible. 
    /// </summary>
    [System.Diagnostics.DebuggerStepThrough]
    public class PagingArgs : IEquatable<PagingArgs>
    {
        private static long? _defaultTake;
        private static long? _maxTake;

        /// <summary>
        /// Gets or sets the default <see cref="Take"/> size.
        /// </summary>
        /// <remarks>Defaults to <see cref="SettingsBase.PagingDefaultTake"/> where specified; otherwise, <c>100</c>.</remarks>
        public static long DefaultTake
        {
            get => _defaultTake ?? ExecutionContext.GetService<SettingsBase>()?.PagingDefaultTake ?? 100;

            set
            {
                if (value > 0)
                    _defaultTake = value;
            }
        }

        /// <summary>
        /// Gets or sets the absolute maximum <see cref="Take"/> size allowed.
        /// </summary>
        /// <remarks>Defaults to <see cref="SettingsBase.PagingMaxTake"/> where specified; otherwise, <c>1000</c>.</remarks>
        public static long MaxTake
        {
            get => _maxTake ?? ExecutionContext.GetService<SettingsBase>()?.PagingMaxTake ?? 1000;

            set
            {
                if (value > 0)
                    _maxTake = value;
            }
        }

        /// <summary>
        /// Gets or sets the default <see cref="IsGetCount"/>.
        /// </summary>
        public static bool DefaultIsGetCount { get; set; }

        /// <summary>
        /// Creates a <see cref="PagingArgs"/> for a specified page number and size.
        /// </summary>
        /// <param name="page">The <see cref="Page"/> number.</param>
        /// <param name="size">The page <see cref="Size"/> (defaults to <see cref="DefaultTake"/>).</param>
        /// <param name="isGetCount">Indicates whether to get the total count (see <see cref="PagingResult.TotalCount"/>) when performing the underlying query (defaults to <see cref="DefaultIsGetCount"/> where <c>null</c>).</param>
        /// <returns>The <see cref="PagingArgs"/>.</returns>
        public static PagingArgs CreatePageAndSize(long page, long? size = null, bool? isGetCount = null)
        {
            var pa = new PagingArgs
            {
                Page = page < 0 ? 1 : page,
                Take = !size.HasValue || size.Value < 1 ? DefaultTake : (size.Value > MaxTake ? MaxTake : size.Value),
                IsGetCount = isGetCount == null ? DefaultIsGetCount : isGetCount.Value
            };

            pa.Skip = (pa.Page.Value - 1) * pa.Size;
            return pa;
        }

        /// <summary>
        /// Creates a <see cref="PagingArgs"/> for a specified skip and take.
        /// </summary>
        /// <param name="skip">The <see cref="Skip"/> value.</param>
        /// <param name="take">The <see cref="Take"/> value (defaults to <see cref="DefaultTake"/>).</param>
        /// <param name="isGetCount">Indicates whether to get the total count (see <see cref="PagingResult.TotalCount"/>) when performing the underlying query (defaults to <see cref="DefaultIsGetCount"/> where <c>null</c>).</param>
        /// <returns>The <see cref="PagingArgs"/>.</returns>
        public static PagingArgs CreateSkipAndTake(long skip, long? take = null, bool? isGetCount = null) => new()
        {
            Skip = skip < 0 ? 0 : skip,
            Take = !take.HasValue || take.Value < 1 ? DefaultTake : (take.Value > MaxTake ? MaxTake : take.Value),
            IsGetCount = isGetCount == null ? DefaultIsGetCount : isGetCount.Value
        };

        /// <summary>
        /// Creates a <see cref="PagingArgs"/> for a specified token and take.
        /// </summary>
        /// <param name="token">The <see cref="Token"/> to use to get the next page of elements.</param>
        /// <param name="take">The <see cref="Take"/> value (defaults to <see cref="DefaultTake"/>).</param>
        /// <param name="isGetCount">Indicates whether to get the total count (see <see cref="PagingResult.TotalCount"/>) when performing the underlying query (defaults to <see cref="DefaultIsGetCount"/> where <c>null</c>).</param>
        /// <returns>The <see cref="PagingArgs"/>.</returns>
        public static PagingArgs CreateTokenAndTake(string token, long? take = null, bool? isGetCount = null) => new ()
        {
            Token = token.ThrowIfNullOrEmpty(),
            Take = !take.HasValue || take.Value< 1 ? DefaultTake : (take.Value > MaxTake? MaxTake : take.Value),
            IsGetCount = isGetCount == null ? DefaultIsGetCount : isGetCount.Value
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="PagingArgs"/> class with default <see cref="Skip"/> and <see cref="Take"/>.
        /// </summary>
        public PagingArgs()
        {
            Skip = 0;
            Take = DefaultTake;
            IsGetCount = DefaultIsGetCount;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PagingArgs"/> class copying the values from <paramref name="pagingArgs"/>.
        /// </summary>
        /// <param name="pagingArgs">The <see cref="PagingArgs"/> to copy from.</param>
        public PagingArgs(PagingArgs pagingArgs)
        {
            Skip = pagingArgs.ThrowIfNull().Skip;
            Take = pagingArgs.Take;
            Page = pagingArgs.Page;
            Token = pagingArgs.Token;
            IsGetCount = pagingArgs.IsGetCount;
        }

        /// <summary>
        /// Gets the page number for the elements in a sequence to select (see <see cref="CreatePageAndSize(long, long?, bool?)"/>).
        /// </summary>
        public long? Page { get; internal protected set; }

        /// <summary>
        /// Gets the specified number of elements in a sequence to bypass.
        /// </summary>
        public long? Skip { get; internal protected set; }

        /// <summary>
        /// Gets the token to use to get the next page of elements (see <see cref="CreateTokenAndTake(string?, long?, bool?)"/>).
        /// </summary>
        public string? Token { get; internal protected set; }

        /// <summary>
        /// Indicates the <see cref="PagingOption"/>.
        /// </summary>
        public PagingOption Option => Page is not null ? PagingOption.PageAndSize : (Token is not null ? PagingOption.TokenAndTake : PagingOption.SkipAndTake);

        /// <summary>
        /// Gets the page size (synonym for <see cref="Take"/>).
        /// </summary>
        [JsonIgnore]
        public long Size => Take;

        /// <summary>
        /// Gets the specified number of contiguous elements from the start of a sequence.
        /// </summary>
        public long Take { get; internal protected set; }

        /// <summary>
        /// Overrides/updates the <see cref="Skip"/> value.
        /// </summary>
        /// <param name="skip">The new skip value.</param>
        /// <returns>The <see cref="PagingArgs"/> instance to support fluent-style method chaining.</returns>
        public PagingArgs OverrideSkip(long skip)
        {
            if (Option == PagingOption.TokenAndTake)
                throw new InvalidOperationException($"Cannot override {nameof(Skip)} where {nameof(Option)} is {nameof(PagingOption.TokenAndTake)}.");

            if (skip == Skip)
                return this;

            Skip = skip < 0 ? 0 : skip;
            return this;
        }

        /// <summary>
        /// Overrides/updates the <see cref="Take"/> value bypassing the <see cref="MaxTake"/> checking.
        /// </summary>
        /// <param name="take">The new take value.</param>
        /// <returns>The <see cref="PagingArgs"/> instance to support fluent-style method chaining.</returns>
        public PagingArgs OverrideTake(long take)
        {
            Take = take < 0 ? 0 : take;
            return this;
        }

        /// <summary>
        /// Indicates whether to get the total count (see <see cref="PagingResult.TotalCount"/>) when performing the underlying query (defaults to <c>false</c>).
        /// </summary>
        [JsonPropertyName("count")]
        public bool IsGetCount { get; set; } = false;

        #region Equality

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is PagingArgs pa && Equals(pa);

        /// <inheritdoc/>
        public bool Equals(PagingArgs? other) => other is not null && Skip == other.Skip && Take == other.Take && Page == other.Page && Token == other.Token && IsGetCount == other.IsGetCount;

        /// <summary>
        /// Indicates whether the current <see cref="PagingArgs"/> is equal to another <see cref="PagingArgs"/>.
        /// </summary>
        public static bool operator ==(PagingArgs? left, PagingArgs? right) => (left is null && right is null) || (left is not null && right is not null && left.Equals(right));

        /// <summary>
        /// Indicates whether the current <see cref="PagingArgs"/> is not equal to another <see cref="PagingArgs"/>.
        /// </summary>
        public static bool operator !=(PagingArgs? left, PagingArgs? right) => !(left == right);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Skip, Take, Page, Token, IsGetCount);

        #endregion
    }
}