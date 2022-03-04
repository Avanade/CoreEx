// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Entities
{
    /// <summary>
    /// Provides a means to determine if a class is in its initial/default state and therefore the reference to this value should be reset to <c>null</c> during a <see cref="Cleaner.Clean{T}(T)"/> or <see cref="Cleaner.Clean{T}(T, bool)"/>
    /// as per the <c>overrideWithNullWhenIsInitial</c> parameter.
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