// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Mapping
{
    /// <summary>
    /// Represents an <see cref="AutoMapper.IMapper"/> wrapper to enable <i>CoreEx</i> <see cref="IMapper"/>.
    /// </summary>
    public class AutoMapperWrapper : IMapper
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AutoMapperWrapper"/> class.
        /// </summary>
        /// <param name="autoMapper">The <see cref="AutoMapper.IMapper"/> being wrapped.</param>
        public AutoMapperWrapper(AutoMapper.IMapper autoMapper) => Mapper = autoMapper ?? throw new ArgumentNullException(nameof(autoMapper));

        /// <summary>
        /// Gets the wrapped <see cref="AutoMapper.IMapper"/>
        /// </summary>
        public AutoMapper.IMapper Mapper { get; }

        /// <inheritdoc/>
        public TDestination? Map<TDestination>(object? source, OperationTypes operationType = OperationTypes.Unspecified) => Mapper.Map<TDestination>(source!, operationType);

        /// <inheritdoc/>
        public TDestination? Map<TSource, TDestination>(TSource? source, OperationTypes operationType = OperationTypes.Unspecified) => Mapper.Map<TSource, TDestination>(source!, operationType);

        /// <inheritdoc/>
        public TDestination? Map<TSource, TDestination>(TSource? source, TDestination? destination, OperationTypes operationType = OperationTypes.Unspecified) => Mapper.Map(source, destination, operationType);
    }
}