// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Http;
using System;

namespace CoreEx.AspNetCore.WebApis
{
    /// <summary>
    /// Provides the <see cref="PagingOperationFilter"/> fields.
    /// </summary>
    [Flags]
    public enum PagingOperationFilterFields
    {
        /// <summary>
        /// Indicates to include <see cref="PagingArgs.Skip"/> field (named <see cref="HttpConsts.PagingArgsSkipQueryStringName"/>).
        /// </summary>
        Skip = 1,

        /// <summary>
        /// Indicates to include <see cref="PagingArgs.Take"/> field (named <see cref="HttpConsts.PagingArgsTakeQueryStringName"/>).
        /// </summary>
        Take = 2,

        /// <summary>
        /// Indicates to include <see cref="PagingArgs.Page"/> field (named <see cref="HttpConsts.PagingArgsPageQueryStringName"/>).
        /// </summary>
        Page = 4,

        /// <summary>
        /// Indicates to include <see cref="PagingArgs.Size"/> field (named <see cref="HttpConsts.PagingArgsSizeQueryStringName"/>).
        /// </summary>
        Size = 8,

        /// <summary>
        /// Indicates to include <see cref="PagingArgs.IsGetCount"/> field (named <see cref="HttpConsts.PagingArgsCountQueryStringName"/>).
        /// </summary>
        Count = 16,

        /// <summary>
        /// Indicates to include <see cref="Skip"/> and <see cref="Take"/> fields.
        /// </summary>
        SkipTake = Skip | Take,

        /// <summary>
        /// Indicates to include <see cref="Skip"/>, <see cref="Take"/> and <see cref="Count"/> fields.
        /// </summary>
        SkipTakeCount = Skip | Take | Count,

        /// <summary>
        /// Indicates to include <see cref="Page"/> and <see cref="Size"/> fields.
        /// </summary>
        PageSize = Page | Size,

        /// <summary>
        /// Indicates to include <see cref="Page"/>, <see cref="Size"/> and <see cref="Count"/> fields.
        /// </summary>
        PageSizeCount = Page | Size | Count,

        /// <summary>
        /// Indicates to include all fields.
        /// </summary>
        All = Skip | Take | Page | Size | Count
    }
}