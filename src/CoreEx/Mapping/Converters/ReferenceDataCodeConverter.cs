// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.RefData;
using System;

namespace CoreEx.Mapping.Converters
{
    /// <summary>
    /// Represents an <see cref="IReferenceData"/> to <see cref="IReferenceData.Code"/> (<see cref="string"/>) converter.
    /// </summary>
    /// <typeparam name="TRef">The <see cref="IReferenceData"/> <see cref="Type"/>.</typeparam>
    public struct ReferenceDataCodeConverter<TRef> : IConverter<TRef?, string?> where TRef : IReferenceData, new()
    {
        private readonly ValueConverter<TRef, string?> _convertToDestination = new(s => s?.Code);
        private readonly ValueConverter<string?, TRef> _convertToSource = new(d => ReferenceDataOrchestrator.ConvertFromCode<TRef>(d)!);

        /// <summary>
        /// Gets or sets the default (singleton) instance.
        /// </summary>
        public static ReferenceDataCodeConverter<TRef> Default { get; set; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceDataCodeConverter{TSrceProperty}"/> struct.
        /// </summary>
        public ReferenceDataCodeConverter() { }

        /// <summary>
        /// Gets the source to destination <see cref="IValueConverter{TSource, TDestination}"/>.
        /// </summary>
        public IValueConverter<TRef?, string?> ToDestination => _convertToDestination;

        /// <summary>
        /// Gets the destination to source <see cref="IValueConverter{TDestination, TSource}"/>.
        /// </summary>
        public IValueConverter<string?, TRef?> ToSource => _convertToSource;
    }
}