// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Entities
{
    /// <summary>
    /// Provides the ability to perform a deep <see cref="CopyFrom"/> another object.
    /// </summary>
    /// <typeparam name="T">The <see cref="System.Type"/> to copy from.</typeparam>
    public interface ICopyFrom<T>
    {
        /// <summary>
        /// Performs a deep copy from another object updating this instance.
        /// </summary>
        /// <param name="from">The value to copy from.</param>
        void CopyFrom(T from);
    }
}