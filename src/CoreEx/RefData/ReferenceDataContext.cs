// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;
using System.Collections.Concurrent;

namespace CoreEx.RefData
{
    /// <summary>
    /// Provides the contextual validation <see cref="Date"/> for a <see cref="IReferenceData.StartDate"/> and <see cref="IReferenceData.EndDate"/> <see cref="IReferenceData.IsValid"/> verification.
    /// </summary>
    /// <remarks>All dates when set go through <see cref="Cleaner.Clean(DateTime, DateTimeTransform)"/> with <see cref="DateTimeTransform.DateOnly"/>.</remarks>
    public class ReferenceDataContext : IReferenceDataContext
    {
        private DateTime? _date;
        private readonly ConcurrentDictionary<Type, DateTime?> _coll = new();

        /// <summary>
        /// Gets or sets the <see cref="IReferenceData"/> <see cref="IReferenceData.StartDate"/> and <see cref="IReferenceData.EndDate"/> contextual validation date.
        /// </summary>
        /// <remarks>Defaults to <see cref="ExecutionContext.SystemTime"/>.</remarks>
        public DateTime? Date
        {
            get => _date ??= Cleaner.Clean(ExecutionContext.SystemTime.UtcNow, DateTimeTransform.DateOnly);
            set => _date = Cleaner.Clean(value, DateTimeTransform.DateOnly);
        }

        /// <summary>
        /// Gets or sets a contextual validation date for a specific <see cref="IReferenceData"/> <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="IReferenceData"/> <see cref="Type"/>.</param>
        /// <returns>The contextual validation date.</returns>
        public DateTime? this[Type type]
        {
            get => (_coll.TryGetValue(type.ThrowIfNull(nameof(type)), out var date) ? date : Date) ?? Date;
            set => _coll.AddOrUpdate(type, Cleaner.Clean(value, DateTimeTransform.DateOnly), (_, __) => Cleaner.Clean(value, DateTimeTransform.DateOnly));
        }

        /// <summary>
        /// Resets all dates.
        /// </summary>
        public void Reset()
        {
            _date = null;
            _coll.Clear();
        }
    }
}