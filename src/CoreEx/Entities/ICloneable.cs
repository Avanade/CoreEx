// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Entities
{
    /// <summary>
    /// Provides a means to <see cref="Clone"/> a copy of a class instance.
    /// </summary>
    /// <typeparam name="T">The <see cref="System.Type"/> to clone.</typeparam>
    public interface ICloneable<T> where T : class
    {
        /// <summary>
        /// Creates a new object that is a deep copy of the current instance.
        /// </summary>
        /// <returns>A cloned instance.</returns>
        T Clone();
    }
}