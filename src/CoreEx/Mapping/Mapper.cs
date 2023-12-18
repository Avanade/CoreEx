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
        /// Gets an empty <see cref="IMapper"/>; i.e. one that does not perform any mapping and will always throw a <see cref="NotImplementedException"/> where a <c>Map</c> operation is invoked.
        /// </summary>
        public static EmptyMapper Empty { get; } = new EmptyMapper();

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

        /// <summary>
        /// Registers (adds) an individual <see cref="IBidirectionalMapper{TSource, TDestination}"/>.
        /// </summary>
        /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
        /// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
        /// <param name="bidirectionalMapper">The see cref="IBidirectionalMapper{TSource, TDestination}"/>.</param>
        /// <remarks>Where an attempt is made to add a mapper for a <typeparamref name="TSource"/> and <typeparamref name="TDestination"/> more than once only the first will succeed; no exception will be thrown for subsequent adds.</remarks>
        public void Register<TSource, TDestination>(IBidirectionalMapper<TSource, TDestination> bidirectionalMapper)
        {
            var mapperFromTo = bidirectionalMapper.ThrowIfNull(nameof(bidirectionalMapper)).MapperFromTo.Adjust(x => x.Owner = this);
            var mapperToFrom = bidirectionalMapper.MapperToFrom.Adjust(x => x.Owner = this);
            _mappers.TryAdd((mapperFromTo.SourceType, mapperFromTo.DestinationType), mapperFromTo);
            _mappers.TryAdd((mapperToFrom.SourceType, mapperToFrom.DestinationType), mapperToFrom);
        }

        /// <inheritdoc/>
        [return: NotNullIfNotNull(nameof(source))]
        public TDestination? Map<TDestination>(object? source, OperationTypes operationType = OperationTypes.Unspecified)
        {
            if (source is null)
                return default!;

            return (TDestination)GetMapper(source.GetType(), typeof(TDestination)).Map(source, operationType)!;
        }

        /// <summary>
        /// Gets the <see cref="IMapperBase"/> for the specified <paramref name="source"/> and <paramref name="destination"/> types as previously <see cref="Register{TSource, TDestination}(IMapper{TSource, TDestination})">registered</see>.
        /// </summary>
        /// <param name="source">The source <see cref="Type"/>.</param>
        /// <param name="destination">The destination <see cref="Type"/>.</param>
        /// <returns>The previously registered <see cref="IMapperBase"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown where not previously registered.</exception>
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
        /// Gets the <see cref="IMapper{TSource, TDestination}"/> for the specified <typeparamref name="TSource"/> and <typeparamref name="TDestination"/> types as previously <see cref="Register{TSource, TDestination}(IMapper{TSource, TDestination})">registered</see>.
        /// </summary>
        /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
        /// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
        /// <returns>The previously registered <see cref="IMapper{TSource, TDestination}"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown where not previously registered.</exception>
        public IMapper<TSource, TDestination> GetMapper<TSource, TDestination>() => (IMapper<TSource, TDestination>)GetMapper(typeof(TSource), typeof(TDestination));

        /// <inheritdoc/>
        [return: NotNullIfNotNull(nameof(source))]
        public TDestination? Map<TSource, TDestination>(TSource? source, OperationTypes operationType = OperationTypes.Unspecified)
            => GetMapper<TSource, TDestination>().Map(source, operationType)!;

        /// <inheritdoc/>
        [return: NotNullIfNotNull(nameof(source))]
        public TDestination? Map<TSource, TDestination>(TSource? source, TDestination? destination, OperationTypes operationType = OperationTypes.Unspecified)
            => GetMapper<TSource, TDestination>().Map(source, destination, operationType)!;

        /// <summary>
        /// Represents an empty <see cref="IMapper"/>; i.e. one that does not perform any mapping and will always throw a <see cref="NotImplementedException"/>.
        /// </summary>
        public class EmptyMapper : IMapper
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="EmptyMapper"/> class.
            /// </summary>
            internal EmptyMapper() { }

            /// <inheritdoc/>
            [return: NotNullIfNotNull(nameof(source))]
            public TDestination? Map<TDestination>(object? source, OperationTypes operationType = OperationTypes.Unspecified) => throw new NotImplementedException();

            /// <inheritdoc/>
            [return: NotNullIfNotNull(nameof(source))]
            public TDestination? Map<TSource, TDestination>(TSource? source, OperationTypes operationType = OperationTypes.Unspecified) => throw new NotImplementedException();

            /// <inheritdoc/>
            [return: NotNullIfNotNull(nameof(source))]
            public TDestination? Map<TSource, TDestination>(TSource? source, TDestination? destination, OperationTypes operationType = OperationTypes.Unspecified) => throw new NotImplementedException();
        }

        /// <summary>
        /// When <paramref name="operationType"/> is a <see cref="OperationTypes.Get"/> then the action is invoked.
        /// </summary>
        /// <param name="operationType">The singular <see cref="OperationTypes"/>.</param>
        /// <param name="action">The action to invoke.</param>
        public static void WhenGet(OperationTypes operationType, Action action) => WhenOperationType(OperationTypes.Get, operationType, action);

        /// <summary>
        /// When <paramref name="operationType"/> is a <see cref="OperationTypes.Create"/> then the action is invoked.
        /// </summary>
        /// <param name="operationType">The singular <see cref="OperationTypes"/>.</param>
        /// <param name="action">The action to invoke.</param>
        public static void WhenCreate(OperationTypes operationType, Action action) => WhenOperationType(OperationTypes.Create, operationType, action);

        /// <summary>
        /// When <paramref name="operationType"/> is an <see cref="OperationTypes.Update"/> then the action is invoked.
        /// </summary>
        /// <param name="operationType">The singular <see cref="OperationTypes"/>.</param>
        /// <param name="action">The action to invoke.</param>
        public static void WhenUpdate(OperationTypes operationType, Action action) => WhenOperationType(OperationTypes.Update, operationType, action);

        /// <summary>
        /// When <paramref name="operationType"/> is a <see cref="OperationTypes.Delete"/> then the action is invoked.
        /// </summary>
        /// <param name="operationType">The singular <see cref="OperationTypes"/>.</param>
        /// <param name="action">The action to invoke.</param>
        public static void WhenDelete(OperationTypes operationType, Action action) => WhenOperationType(OperationTypes.Delete, operationType, action);

        /// <summary>
        /// When <paramref name="operationType"/> is a <see cref="OperationTypes.AnyExceptGet"/> then the action is invoked.
        /// </summary>
        /// <param name="operationType">The singular <see cref="OperationTypes"/>.</param>
        /// <param name="action">The action to invoke.</param>
        public static void WhenAnyExceptGet(OperationTypes operationType, Action action) => WhenOperationType(OperationTypes.AnyExceptGet, operationType, action);

        /// <summary>
        /// When <paramref name="operationType"/> is a <see cref="OperationTypes.AnyExceptCreate"/> then the action is invoked.
        /// </summary>
        /// <param name="operationType">The singular <see cref="OperationTypes"/>.</param>
        /// <param name="action">The action to invoke.</param>
        public static void WhenAnyExceptCreate(OperationTypes operationType, Action action) => WhenOperationType(OperationTypes.AnyExceptCreate, operationType, action);

        /// <summary>
        /// When <paramref name="operationType"/> is a <see cref="OperationTypes.AnyExceptUpdate"/> then the action is invoked.
        /// </summary>
        /// <param name="operationType">The singular <see cref="OperationTypes"/>.</param>
        /// <param name="action">The action to invoke.</param>
        public static void WhenAnyExceptUpdate(OperationTypes operationType, Action action) => WhenOperationType(OperationTypes.AnyExceptUpdate, operationType, action);

        /// <summary>
        /// When the <paramref name="operationType"/> matches the <paramref name="expectedOperationTypes"/> then the <paramref name="action"/> is invoked.
        /// </summary>
        private static void WhenOperationType(OperationTypes expectedOperationTypes, OperationTypes operationType, Action action)
        {
            if (expectedOperationTypes.HasFlag(operationType))
                action?.Invoke();
        }
    }
}