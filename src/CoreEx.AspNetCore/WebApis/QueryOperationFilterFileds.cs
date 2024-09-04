// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Http;
using System;

namespace CoreEx.AspNetCore.WebApis
{
    /// <summary>
    /// Provides the <see cref="QueryOperationFilter"/> fields.
    /// </summary>
    [Flags]
    public enum QueryOperationFilterFields
    {
        /// <summary>
        /// Indicates to include the <see cref="QueryArgs.Filter"/> field (named <see cref="HttpConsts.QueryArgsFilterQueryStringName"/>).
        /// </summary>
        Filter = 1,

        /// <summary>
        /// Indicates to include the <see cref="QueryArgs.OrderBy"/> field (named <see cref="HttpConsts.QueryArgsOrderByQueryStringName"/>).
        /// </summary>
        OrderBy = 2,

        /// <summary>
        /// Indicates to include all fields.
        /// </summary>
        All = Filter | OrderBy
    }
}