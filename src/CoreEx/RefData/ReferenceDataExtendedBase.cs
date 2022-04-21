// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;
using System.Text.Json.Serialization;

namespace CoreEx.RefData
{
    /// <summary>
    /// Represents the extended <see cref="IReferenceDataExtended"/> base implementation.
    /// </summary>
    public abstract class ReferenceDataExtendedBase<T> : IReferenceDataExtended, IIdentifier<T>
    {
        /// <inheritdoc/>
        public T Id { get; set; } = default!;

        /// <inheritdoc/>
        public string? Code { get; set; }

        /// <inheritdoc/>
        public string? Text { get; set; }

        /// <inheritdoc/>
        public string? Description { get; set; }

        /// <inheritdoc/>
        public int SortOrder { get; set; }

        /// <inheritdoc/>
        public bool IsActive { get; set; }

        /// <inheritdoc/>
        public DateTime? StartDate { get; set; }

        /// <inheritdoc/>
        public DateTime? EndDate { get; set; }

        /// <inheritdoc/>
        /// <remarks>Note to classes that override: the base <see cref="IsValid"/> should be called as it verifies <see cref="IsActive"/>, and that the <see cref="StartDate"/> and <see cref="EndDate"/> are not outside of the 
        /// <see cref="IReferenceDataContext"/> <see cref="IReferenceDataContext.Date"/> where configured. This is accessed via <see cref="ExecutionContext.Current"/> <see cref="ExecutionContext.ReferenceDataContext"/>.</remarks>
        [JsonIgnore]
        public virtual bool IsValid
        {
            get
            {
                if (!IsActive)
                    return false;

                if (ExecutionContext.HasCurrent)
                {
                    if (StartDate != null && ExecutionContext.Current.ReferenceDataContext[GetType()] < Cleaner.Clean(StartDate, DateTimeTransform.DateOnly))
                        return false;

                    if (EndDate != null && ExecutionContext.Current.ReferenceDataContext[GetType()] > Cleaner.Clean(EndDate, DateTimeTransform.DateOnly))
                        return false;
                }

                return true;
            }
        }
    }
}