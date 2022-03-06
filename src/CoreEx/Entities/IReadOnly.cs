// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Entities
{
    /// <summary>
    /// Provides a means to <see cref="MakeReadOnly"/> the class.
    /// </summary>
    public interface IReadOnly
    {
        /// <summary>
        /// Indicates whether the entity is read only (see <see cref="MakeReadOnly"/>).
        /// </summary>
        public bool IsReadOnly { get; }

        /// <summary>
        /// Makes the entity read-only; such that it will no longer support any property changes (see <see cref="IsReadOnly"/>).
        /// </summary>
        public void MakeReadOnly();
    }
}