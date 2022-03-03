// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Entities
{
    /// <summary>
    /// Provides a means to <see cref="CleanUp"/> the class.
    /// </summary>
    /// <remarks>See <see cref="Cleaner"/>.</remarks>
    public interface ICleanUp
    {
        /// <summary>
        /// Cleans up the properties/state of the class.
        /// </summary>
        void CleanUp();
    }
}