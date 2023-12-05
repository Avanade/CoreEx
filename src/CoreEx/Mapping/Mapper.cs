// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions.Reflection;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

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
        /// <remarks>Also, automatically registers the mapping Cartesian product between <see cref="Entities.ChangeLog"/> and <see cref="Entities.Extended.ChangeLogEx"/> (i.e. all combinations thereof).</remarks>
        public Mapper()
        {
            Register(new Mapper<Entities.ChangeLog, Entities.ChangeLog>()
                .Map((s, d) => d.CreatedBy = s.CreatedBy, OperationTypes.AnyExceptUpdate)
                .Map((s, d) => d.CreatedDate = s.CreatedDate, OperationTypes.AnyExceptUpdate)
                .Map((s, d) => d.UpdatedBy = s.UpdatedBy, OperationTypes.AnyExceptCreate)
                .Map((s, d) => d.UpdatedDate = s.UpdatedDate, OperationTypes.AnyExceptCreate));

            Register(new Mapper<Entities.Extended.ChangeLogEx, Entities.Extended.ChangeLogEx>()
                .Map((s, d) => d.CreatedBy = s.CreatedBy, OperationTypes.AnyExceptUpdate)
                .Map((s, d) => d.CreatedDate = s.CreatedDate, OperationTypes.AnyExceptUpdate)
                .Map((s, d) => d.UpdatedBy = s.UpdatedBy, OperationTypes.AnyExceptCreate)
                .Map((s, d) => d.UpdatedDate = s.UpdatedDate, OperationTypes.AnyExceptCreate));

            Register(new Mapper<Entities.ChangeLog, Entities.Extended.ChangeLogEx>()
                .Map((s, d) => d.CreatedBy = s.CreatedBy, OperationTypes.AnyExceptUpdate)
                .Map((s, d) => d.CreatedDate = s.CreatedDate, OperationTypes.AnyExceptUpdate)
                .Map((s, d) => d.UpdatedBy = s.UpdatedBy, OperationTypes.AnyExceptCreate)
                .Map((s, d) => d.UpdatedDate = s.UpdatedDate, OperationTypes.AnyExceptCreate));

            Register(new Mapper<Entities.Extended.ChangeLogEx, Entities.ChangeLog>()
                .Map((s, d) => d.CreatedBy = s.CreatedBy, OperationTypes.AnyExceptUpdate)
                .Map((s, d) => d.CreatedDate = s.CreatedDate, OperationTypes.AnyExceptUpdate)
                .Map((s, d) => d.UpdatedBy = s.UpdatedBy, OperationTypes.AnyExceptCreate)
                .Map((s, d) => d.UpdatedDate = s.UpdatedDate, OperationTypes.AnyExceptCreate));
        }

        /// <summary>
        /// Indicates whether to convert empty collections to <c>null</c> where supported. Defaults to <c>true</c>.
        /// </summary>
        public bool ConvertEmptyCollectionsToNull { get; set; } = true;

        /// <summary>
        /// Register (adds) all the <see cref="IMapper{TSource, TDestination}"/> and <see cref="IBidirectionalMapper{TFrom, TTo}"/> types (instances) from the <see cref="Assembly"/> from the specified <typeparamref name="TAssembly"/> <see cref="Type"/>.
        /// </summary>
        /// <typeparam name="TAssembly">The <see cref="Assembly"/> <see cref="Type"/>.</typeparam>
        public void Register<TAssembly>() => Register(typeof(TAssembly).Assembly);

        /// <summary>
        /// Register (adds) all <see cref="IMapper{TSource, TDestination}"/> and <see cref="IBidirectionalMapper{TFrom, TTo}"/> types (instances) from the specified <paramref name="assemblies"/>.
        /// </summary>
        /// <param name="assemblies">The assemblies.</param>
        public void Register(params Assembly[] assemblies)
        {
            foreach (var assembly in assemblies.Distinct())
            {
                foreach (var match in from type in assembly.GetTypes()
                                      where !type.IsAbstract && !type.IsGenericTypeDefinition
                                      let interfaces = type.GetInterfaces()
                                      let genericInterfaces = interfaces.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMapper<,>))
                                      let @interface = genericInterfaces.FirstOrDefault()
                                      let sourceType = @interface?.GetGenericArguments().Length == 2 ? @interface?.GetGenericArguments()[0] : null
                                      let destinationType = @interface?.GetGenericArguments().Length == 2 ? @interface?.GetGenericArguments()[1] : null
                                      where @interface != null
                                      select new { type, sourceType, destinationType })
                {
                    Register(match.sourceType, match.destinationType, (IMapperBase)Activator.CreateInstance(match.type)!);
                }

                foreach (var match in from type in assembly.GetTypes()
                                      where !type.IsAbstract && !type.IsGenericTypeDefinition
                                      let interfaces = type.GetInterfaces()
                                      let genericInterfaces = interfaces.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IBidirectionalMapper<,>))
                                      let @interface = genericInterfaces.FirstOrDefault()
                                      where @interface != null
                                      select new { type })
                {
                    var bimapper = (IBidirectionalMapperBase)Activator.CreateInstance(match.type)!;
                    _mappers.TryAdd((bimapper.MapperFromTo.SourceType, bimapper.MapperFromTo.DestinationType), bimapper.MapperFromTo);
                    _mappers.TryAdd((bimapper.MapperToFrom.SourceType, bimapper.MapperToFrom.DestinationType), bimapper.MapperToFrom);
                }
            }
        }

        /// <summary>
        /// Perform the actual mapper registration and linking.
        /// </summary>
        private void Register(Type s, Type d, IMapperBase mapper)
        {
            mapper.Owner = this;
            _mappers.TryAdd((s, d), mapper);
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
                        var t = typeof(CollectionMapper<,,,>).MakeGenericType(source, si.ItemType!, destination, di.ItemType!);
                        var mapper = (IMapperBase)Activator.CreateInstance(t)!;
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
            => GetMapper<TSource, TDestination>().Map(source, operationType)!;

        /// <inheritdoc/>
        [return: NotNullIfNotNull(nameof(source))]
        public TDestination? Map<TSource, TDestination>(TSource? source, TDestination? destination, OperationTypes operationType = OperationTypes.Unspecified)
            => GetMapper<TSource, TDestination>().Map(source, destination, operationType)!;
    }
}