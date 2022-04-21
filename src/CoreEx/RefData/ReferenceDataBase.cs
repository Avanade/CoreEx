// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.RefData
{
    /// <summary>
    /// Represents the base <see cref="IReferenceData"/> base implementation.
    /// </summary>
    public abstract class ReferenceDataBase<T> : IReferenceData<T>
    {
        /// <inheritdoc/>
        public T Id { get; set; } = default!;

        /// <inheritdoc/>
        public string? Code { get; set; }

        /// <inheritdoc/>
        public string? Text { get; set; }
    }
}