// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Caching;

namespace CoreEx.Abstractions
{
    /// <summary>
    /// Identifies a type as containing a unique key of some sort without enabling the what or how.
    /// </summary>
    /// <remarks>Should then implement <see cref="IPrimaryKey"/> and/or <see cref="ICacheKey"/>.</remarks>
    public interface IUniqueKey { }
}