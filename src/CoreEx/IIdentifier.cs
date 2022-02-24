// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx
{
    /// <summary>
    /// Enables the <see cref="GetIdentifier"/> capability.
    /// </summary>
    public interface IIdentifier
    {
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        object? GetIdentifier();
    }
}