// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.Extensions.Caching.Memory;

namespace CoreEx.Abstractions.Reflection
{
    /// <summary>
    /// Represents a cache for reflection operations.
    /// </summary>
    public interface IReflectionCache : IMemoryCache { }
}