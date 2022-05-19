// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Entities
{
    /// <summary>
    /// Provides the ability to perform a deep <see cref="CopyFrom"/> another object.
    /// </summary>
    public interface ICopyFrom
    {
        /// <summary>
        /// Performs a deep copy from another object updating this instance.
        /// </summary>
        /// <param name="other">The other value to copy from.</param>
        void CopyFrom(object? other);
    }
}