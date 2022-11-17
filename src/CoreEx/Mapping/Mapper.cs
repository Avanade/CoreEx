// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions.Reflection;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace CoreEx.Mapping
{
    /// <summary>
    /// Provides the simple (explicit) value <see cref="IMapper"/> capability.  
    /// </summary>
    public class Mapper : IMapper
    {
        private readonly ConcurrentDictionary<(Type, Type), IMapperBase> _mappers = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="Mapper"/> class.
        /// </summary>
        /// <remarks>Also, automatically registers the mapping Cartesian product between <see cref="Entities.ChangeLog"/> and <see cref="Entities.Models.ChangeLog"/>.</remarks>
        public Mapper()
        {
            Register(new Mapper<Entities.ChangeLog, Entities.ChangeLog>()
                .Map((s, d) => d.CreatedBy = s.CreatedBy, OperationTypes.AnyExceptUpdate)
                .Map((s, d) => d.CreatedDate = s.CreatedDate, OperationTypes.AnyExceptUpdate)
                .Map((s, d) => d.UpdatedBy = s.UpdatedBy, OperationTypes.AnyExceptCreate)
                .Map((s, d) => d.UpdatedDate = s.UpdatedDate, OperationTypes.AnyExceptCreate));

            Register(new Mapper<Entities.Models.ChangeLog, Entities.Models.ChangeLog>()
                .Map((s, d) => d.CreatedBy = s.CreatedBy, OperationTypes.AnyExceptUpdate)
                .Map((s, d) => d.CreatedDate = s.CreatedDate, OperationTypes.AnyExceptUpdate)
                .Map((s, d) => d.UpdatedBy = s.UpdatedBy, OperationTypes.AnyExceptCreate)
                .Map((s, d) => d.UpdatedDate = s.UpdatedDate, OperationTypes.AnyExceptCreate));

            Register(new Mapper<Entities.ChangeLog, Entities.Models.ChangeLog>()
                .Map((s, d) => d.CreatedBy = s.CreatedBy, OperationTypes.AnyExceptUpdate)
                .Map((s, d) => d.CreatedDate = s.CreatedDate, OperationTypes.AnyExceptUpdate)
                .Map((s, d) => d.UpdatedBy = s.UpdatedBy, OperationTypes.AnyExceptCreate)
                .Map((s, d) => d.UpdatedDate = s.UpdatedDate, OperationTypes.AnyExceptCreate));

            Register(new Mapper<Entities.Models.ChangeLog, Entities.ChangeLog>()
                .Map((s, d) => d.CreatedBy = s.CreatedBy, OperationTypes.AnyExceptUpdate)
                .Map((s, d) => d.CreatedDate = s.CreatedDate, OperationTypes.AnyExceptUpdate)
                .Map((s, d) => d.UpdatedBy = s.UpdatedBy, OperationTypes.AnyExceptCreate)
                .Map((s, d) => d.UpdatedDate = s.UpdatedDate, OperationTypes.AnyExceptCreate));
        }

        /// <summary>
        /// Registers (adds) an individual <see cref="IMapper{TSource, TDestination}"/>.
        /// </summary>
        /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
        /// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
        /// <param name="mapper">The <see cref="IMapper{TSource, TDestination}"/>.</param>
        /// <remarks>Where an attempt is made to add a mapper for a <typeparamref name="TSource"/> and <typeparamref name="TDestination"/> more than once only the first will succeed; no exception will be thrown for subsequent adds.</remarks>
        public void Register<TSource, TDestination>(IMapper<TSource, TDestination> mapper)
            => _mappers.TryAdd(((mapper ?? throw new ArgumentNullException(nameof(mapper))).SourceType, mapper.DestinationType), mapper.Adjust(x => x.Owner = this));

        /// <inheritdoc/>
        [return: NotNullIfNotNull(nameof(source))]
        public TDestination? Map<TDestination>(object? source, OperationTypes operationType = OperationTypes.Unspecified)
        {
            if (source is null)
                return default!;

            return (TDestination)GetMapper(source.GetType(), typeof(TDestination)).Map(source, operationType)!;
        }

        /// <summary>
        /// Gets the mapper for the specified <paramref name="source"/> and <paramref name="destination"/> types.
        /// </summary>
        /// <param name="source">The source <see cref="Type"/>.</param>
        /// <param name="destination">The destination <see cref="Type"/>.</param>
        public IMapperBase GetMapper(Type source, Type destination)
        { 
            if (_mappers.TryGetValue((source, destination), out var mapper))
                return mapper;

            // Check if the types are collection and automatically create where possible.
            var si = TypeReflector.GetCollectionItemType(source);
            if (si.TypeCode == TypeReflectorTypeCode.ICollection)
            {
                var di = TypeReflector.GetCollectionItemType(destination);
                if (di.TypeCode == TypeReflectorTypeCode.ICollection)
                    return _mappers.GetOrAdd((source, destination), _ =>
                    {
                        var t = typeof(CollectionMapper<,,,>).MakeGenericType(source, si.ItemType, destination, di.ItemType);
                        var mapper = (IMapperBase)Activator.CreateInstance(t);
                        mapper.Owner = this;
                        return mapper;
                    });
            }

            throw new InvalidOperationException($"No mapper has been registered for source '{source.FullName}' and destination '{destination.FullName}' types.");
        }

        /// <summary>
        /// Gets the mapper for the <typeparamref name="TSource"/> and <typeparamref name="TDestination"/> types.
        /// </summary>
        /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
        /// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
        public IMapper<TSource, TDestination> GetMapper<TSource, TDestination>()
            => (IMapper<TSource, TDestination>)GetMapper(typeof(TSource), typeof(TDestination));

        /// <inheritdoc/>
        [return: NotNullIfNotNull(nameof(source))]
        public TDestination? Map<TSource, TDestination>(TSource? source, OperationTypes operationType = OperationTypes.Unspecified)
            => source is null ? default! : GetMapper<TSource, TDestination>().Map(source, operationType)!;

        /// <inheritdoc/>
        [return: NotNullIfNotNull(nameof(source))]
        public TDestination? Map<TSource, TDestination>(TSource? source, TDestination? destination, OperationTypes operationType = OperationTypes.Unspecified)
            => source is null ? default! : GetMapper<TSource, TDestination>().Map(source, destination, operationType)!;
    }
}