// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreEx.Mapping
{
    /// <summary>
    /// Provides a simple (explicit) <see cref="ICollection{T}"/> value mapper. 
    /// </summary>
    /// <typeparam name="TSourceColl">The source collection <see cref="Type"/>.</typeparam>
    /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
    /// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
    /// <typeparam name="TDestinationColl">The destination collection <see cref="Type"/>.</typeparam>
    public class CollectionMapper<TSourceColl, TSource, TDestinationColl, TDestination> : IMapper<TSourceColl, TDestinationColl>
        where TSourceColl : class, ICollection<TSource>, new() where TSource : class, new()
        where TDestinationColl : class, ICollection<TDestination>, new() where TDestination : class, new()
    {
        private Mapper? _mapper;

        /// <inheritdoc/>
        public Mapper Owner { get => _mapper ?? throw new InvalidOperationException("Mapper has not been set to a non-null value; this is automatically performed when registered."); set => _mapper = value; }

        /// <inheritdoc/>
        public TDestinationColl CreateSource() => new();

        /// <inheritdoc/>
        public TDestinationColl CreateDestination() => new();

        /// <inheritdoc/>
        public virtual bool IsSourceInitial(TSourceColl source) => false;

        /// <inheritdoc/>
        object? IMapperBase.Map(object? source, OperationTypes operationType) => Map((TSourceColl?)source, null, operationType);

        /// <inheritdoc/>
        object? IMapperBase.Map(object? source, object? destination, OperationTypes operationType) => Map((TSourceColl?)source, (TDestinationColl?)destination, operationType);

        /// <inheritdoc/>
        TDestinationColl? IMapper<TSourceColl, TDestinationColl>.Map(TSourceColl? source, OperationTypes operationType) => Map(source, null, operationType);

        /// <inheritdoc/>
        TDestinationColl? IMapper<TSourceColl, TDestinationColl>.Map(TSourceColl? source, TDestinationColl? destination, OperationTypes operationType) => Map(source, destination, operationType);

        /// <param name="source">The source.</param>
        /// <param name="destination">The destination.</param>
        /// <param name="operationType">The singular <see cref="OperationTypes"/>.</param>
        internal TDestinationColl? Map(TSourceColl? source, TDestinationColl? destination, OperationTypes operationType = OperationTypes.Unspecified)
        {
            if (source is null && destination is null)
                return destination;

            if (source is null && destination is not null)
            {
                destination = default;
                return destination;
            }

            source ??= new();
            destination ??= new();
            var itemMapper = Owner.GetMapper<TSource, TDestination>();
            source.ForEach(x => destination.Add(itemMapper.Map(x, operationType)!));
            return destination;
        }
    }
}