// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Mapping.Converters
{
    /// <summary>
    /// Wraps an <see cref="IConverter"/> to enable <see cref="AutoMapper.IValueConverter{TSourceMember, TDestinationMember}"/> to/from capabilities.
    /// </summary>
    /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
    /// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
    /// <typeparam name="TConverter">The <see cref="IConverter{TSource, TDestination}"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="TSelf">The declaring <see cref="Type"/> itself to enable <see cref="Default"/>.</typeparam>
    public abstract class AutoMapperConverterWrapper<TSource, TDestination, TConverter, TSelf>
        where TConverter : IConverter<TSource, TDestination>, new()
        where TSelf : AutoMapperConverterWrapper<TSource, TDestination, TConverter, TSelf>, new()
    {
        private readonly Func<TConverter> _create;

        /// <summary>
        /// Gets or sets the default (singleton) instance.
        /// </summary>
        public static TSelf Default { get; set; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoMapperConverterWrapper{TSource, TDestination, TConverter, TSelf}"/> class.
        /// </summary>
        /// <param name="create">The optional function to create the <typeparamref name="TConverter"/> instance.</param>
        public AutoMapperConverterWrapper(Func<TConverter>? create = null) => _create = create ?? (() => new TConverter());

        /// <summary>
        /// Gets the source to destination <see cref="AutoMapper.IValueConverter{TSource, TDestination}"/>.
        /// </summary>
        public AutoMapper.IValueConverter<TSource, TDestination> ToDestination => new ToDestinationMapper(_create().ToDestination);

        /// <summary>
        /// Gets the destination to source <see cref="AutoMapper.IValueConverter{TDestination, TSource}"/>.
        /// </summary>
        public AutoMapper.IValueConverter<TDestination, TSource> ToSource => new ToSourceMapper(_create().ToSource);

        /// <summary>
        /// Represents the source to destination <see cref="AutoMapper.IValueConverter{TSource, TDestination}"/> struct.
        /// </summary>
        public struct ToDestinationMapper : AutoMapper.IValueConverter<TSource, TDestination>
        {
            private readonly IValueConverter<TSource, TDestination> _valueConverter;

            internal ToDestinationMapper(IValueConverter<TSource, TDestination> valueConverter) => _valueConverter = valueConverter;

            /// <inheritdoc/>
            public TDestination Convert(TSource sourceMember, AutoMapper.ResolutionContext context) => _valueConverter.Convert(sourceMember)!;
        }

        /// <summary>
        /// Represents the destination to source <see cref="AutoMapper.IValueConverter{TDestination, TSource}"/> struct.
        /// </summary>
        public struct ToSourceMapper : AutoMapper.IValueConverter<TDestination, TSource>
        {
            private readonly IValueConverter<TDestination, TSource> _valueConverter;

            internal ToSourceMapper(IValueConverter<TDestination, TSource> valueConverter) => _valueConverter = valueConverter;

            /// <inheritdoc/>
            public TSource Convert(TDestination sourceMember, AutoMapper.ResolutionContext context) => _valueConverter.Convert(sourceMember)!;
        }
    }
}