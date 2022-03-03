// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Entities
{
    /// <summary>
    /// Provides a means to determine if a class is in its initial/default state.
    /// </summary>
    /// <remarks>See <see cref="Cleaner"/>.</remarks>
    public interface IInitial
    {
        /// <summary>
        /// Indicates whether considered initial; i.e. all properties have their initial/default value.
        /// </summary>
        bool IsInitial { get; }
    }
}