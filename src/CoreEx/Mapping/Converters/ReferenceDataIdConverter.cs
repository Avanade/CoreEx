// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.RefData;
using System;

namespace CoreEx.Mapping.Converters
{
    /// <summary>
    /// Represents an <see cref="IReferenceData"/> to <see cref="IReferenceData"/> <see cref="IIdentifier.Id"/> converter.
    /// </summary>
    /// <typeparam name="TRef">The <see cref="IReferenceData"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="TId">The <see cref="IReferenceData"/> <see cref="IIdentifier.Id"/> <see cref="Type"/>.</typeparam>
    public struct ReferenceDataIdConverter<TRef, TId> : IConverter<TRef?, TId> where TRef : IReferenceData, new()
    {
        private readonly ValueConverter<TRef?, TId> _convertToDestination = new(s => s == null ? default! : (TId)s.Id!);
        private readonly ValueConverter<TId, TRef?> _convertToSource = new(d => ReferenceDataOrchestrator.ConvertFromId<TRef>(d));

        /// <summary>
        /// Gets or sets the default (singleton) instance.
        /// </summary>
        public static ReferenceDataIdConverter<TRef, TId> Default { get; set; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceDataCodeConverter{TSrceProperty}"/> struct.
        /// </summary>
        public ReferenceDataIdConverter() { }

        /// <summary>
        /// Gets the source to destination <see cref="IValueConverter{TSource, TDestination}"/>.
        /// </summary>
        public IValueConverter<TRef?, TId> ToDestination => _convertToDestination;

        /// <summary>
        /// Gets the destination to source <see cref="IValueConverter{TDestination, TSource}"/>.
        /// </summary>
        public IValueConverter<TId, TRef?> ToSource => _convertToSource;
    }
}