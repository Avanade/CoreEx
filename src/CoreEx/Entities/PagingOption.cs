// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Entities
{
    /// <summary>
    /// Represents the <see cref="PagingArgs"/> option.
    /// </summary>
    public enum PagingOption
    {
        /// <summary>
        /// Indicates that the <see cref="PagingArgs.CreatePageAndSize(long, long?, bool?)"/> was used to instantiate.
        /// </summary>
        PageAndSize,

        /// <summary>
        /// Indicates that the <see cref="PagingArgs.CreateSkipAndTake(long, long?, bool?)"/> was used to instantiate.
        /// </summary>
        SkipAndTake,

        /// <summary>
        /// Indicates that the <see cref="PagingArgs.CreateTokenAndTake(string?, long?, bool?)"/> was used to instantiate.
        /// </summary>
        TokenAndTake
    }
}