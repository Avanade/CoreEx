// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreEx.Mapping
{
    /// <summary>
    /// Provides a simple (explicit) value mapper. 
    /// </summary>
    /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
    /// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
    public class Mapper<TSource, TDestination> : IMapper<TSource, TDestination> where TSource : class, new() where TDestination : class, new()
    {
        private readonly List<(Action<MapperOptions, TSource, TDestination> action, OperationTypes types, Func<TSource, bool>? isSourceInitial, Action<TDestination>? initializeDestination)> _mappings = [];
        private Mapper? _mapper;
        private Func<TSource, bool>? _isSourceInitial;
        private Func<TDestination, bool>? _initializeDestination;

        /// <inheritdoc/>
        public Mapper Owner
        {
            get => _mapper ?? throw new InvalidOperationException("Owner has not been set to a non-null value; this is automatically performed when registered.");
            set
            {
                _mapper = _mapper is null ? value : throw new InvalidOperationException("Owner can not be changed once set.");
                if (_mapper != null)
                    OnRegister(this);
            }
        }

        /// <summary>
        /// Adds a mapping.
        /// </summary>
        /// <param name="map">The action that performs the mapping.</param>
        /// <param name="operationTypes">The <see cref="OperationTypes"/> (condition) when the mapping should occur. Defaults to <see cref="OperationTypes.Any"/>.</param>
        /// <param name="isSourceInitial">The function to determine whether the source is initial (has default value).</param>
        /// <param name="initializeDestination">The action to initialize the destination as a result of a <see cref="Flatten"/> where the source property is <c>null</c>.</param>
        public Mapper<TSource, TDestination> Map(Action<TSource, TDestination> map, OperationTypes operationTypes = OperationTypes.Any, Func<TSource, bool>? isSourceInitial = null, Action<TDestination>? initializeDestination = null)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            void action(MapperOptions o, TSource s, TDestination d) => map(s, d);

            _mappings.Add((action, operationTypes, isSourceInitial, initializeDestination));
            return this;
        }

        /// <summary>
        /// Adds a mapping.
        /// </summary>
        /// <param name="map">The action that performs the mapping.</param>
        /// <param name="operationTypes">The <see cref="OperationTypes"/> (condition) when the mapping should occur. Defaults to <see cref="OperationTypes.Any"/>.</param>
        /// <param name="isSourceInitial">The function to determine whether the source is initial (has default value).</param>
        /// <param name="initializeDestination">The action to initialize the destination as a result of a <see cref="Flatten"/> where the source property is <c>null</c>.</param>
        public Mapper<TSource, TDestination> Map(Action<MapperOptions, TSource, TDestination> map, OperationTypes operationTypes = OperationTypes.Any, Func<TSource, bool>? isSourceInitial = null, Action<TDestination>? initializeDestination = null)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            _mappings.Add((map, operationTypes, isSourceInitial, initializeDestination));
            return this;
        }

        /// <summary>
        /// Adds a flatten-mapping of a nested property.
        /// </summary>
        /// <typeparam name="T">The <paramref name="source"/> <see cref="Type"/>.</typeparam>
        /// <param name="source">The function to get the source value.</param>
        /// <param name="operationTypes">The <see cref="OperationTypes"/> (condition) when the mapping should occur. Defaults to <see cref="OperationTypes.Any"/>.</param>
        /// <param name="isSourceInitial">The function to determine whether the source is initial (has default value).</param>
        /// <remarks>Flattening is the updating of the <typeparamref name="TDestination"/> from a <typeparamref name="TSource"/> property that is a nested class, that in turn contains the actual corresponding source properties.
        /// Where the <typeparamref name="TSource"/> property is <c>null</c> the corresponding <typeparamref name="TDestination"/> properties are still updated as a temporary source property instance is instantiated and used.
        /// <para>Expanding (<see cref="Expand{T}(Action{TDestination, T}, Func{TSource, TDestination, bool}?, OperationTypes, Action{TDestination}?)"/>) is the opposite of flattening.</para></remarks>
        public Mapper<TSource, TDestination> Flatten<T>(Func<TSource, T?> source, OperationTypes operationTypes = OperationTypes.Any, Func<TSource, bool>? isSourceInitial = null) where T : class, new()
            => Map((o, s, d) =>
            {
                var sv = source(s);
                if (sv is null && o.Owner.GetMapper<T, TDestination>().InitializeDestination(d))
                    return;

                o.Map(sv ?? new(), d);
            }, operationTypes, isSourceInitial: isSourceInitial);

        /// <summary>
        /// Adds an expand-mapping to a nested property.
        /// </summary>
        /// <typeparam name="T">The <typeparamref name="TDestination"/> property <see cref="Type"/>.</typeparam>
        /// <param name="map">The action that performs the mapping.</param>
        /// <param name="operationTypes">The <see cref="OperationTypes"/> (condition) when the mapping should occur. Defaults to <see cref="OperationTypes.Any"/>.</param>
        /// <param name="initializeDestination">The action to initialize the destination as a result of a <see cref="Flatten"/> where the source property is <c>null</c>.</param>
        public Mapper<TSource, TDestination> Expand<T>(Action<TDestination, T> map, OperationTypes operationTypes = OperationTypes.Any, Action<TDestination>? initializeDestination = null) where T : class, new()
            => Expand<T>(map, null, operationTypes, initializeDestination);

        /// <summary>
        /// Adds an expand-mapping to a nested property.
        /// </summary>
        /// <typeparam name="T">The <typeparamref name="TDestination"/> property <see cref="Type"/>.</typeparam>
        /// <param name="map">The action that performs the mapping.</param>
        /// <param name="operationTypes">The <see cref="OperationTypes"/> (condition) when the mapping should occur. Defaults to <see cref="OperationTypes.Any"/>.</param>
        /// <param name="condition">The condition that must be met for the <paramref name="map"/> to be invoked.</param>
        /// <param name="initializeDestination">The action to initialize the destination as a result of a <see cref="Flatten"/> where the source property is <c>null</c>.</param>
        /// <remarks>Where <paramref name="condition"/> is <c>null</c> then the <see cref="IsSourceInitial(TSource)"/> (from the expanding mapper) is used to determine whether mapping occurs (i.e. where result is <c>false</c> being <i>not</i> initial).</remarks>
        public Mapper<TSource, TDestination> Expand<T>(Action<TDestination, T> map, Func<TSource, TDestination, bool>? condition, OperationTypes operationTypes = OperationTypes.Any, Action<TDestination>? initializeDestination = null) where T : class, new()
            => Map((o, s, d) =>
            {
                if (condition is not null && !condition(s, d))
                    return;

                var em = o.Owner.GetMapper<TSource, T>();
                if (condition is null && em.IsSourceInitial(s))
                    return;

                map(d, em.Map(s)!);
            }, operationTypes, initializeDestination: initializeDestination);

        /// <summary>
        /// Adds a base mapping.
        /// </summary>
        /// <typeparam name="TBaseSource">The base source <see cref="Type"/>.</typeparam>
        /// <typeparam name="TBaseDestination">The base destination <see cref="Type"/>.</typeparam>
        public Mapper<TSource, TDestination> Base<TBaseSource, TBaseDestination>() where TBaseSource : class, new() where TBaseDestination : class, new()
        {
            VerifyBaseInstanceOf<TBaseSource, TBaseDestination>();
            return Map((o, s, d) => o.Map((TBaseSource)(object)s, (TBaseDestination)(object)d));
        }

        /// <summary>
        /// Adds a base mapping using the specified <paramref name="baseMapper"/>.
        /// </summary>
        /// <typeparam name="TBaseSource">The base source <see cref="Type"/>.</typeparam>
        /// <typeparam name="TBaseDestination">The base destination <see cref="Type"/>.</typeparam>
        /// <param name="baseMapper">The base <see cref="Mapper{TBaseSource, TBaseDestination}"/>.</param>
        public Mapper<TSource, TDestination> Base<TBaseSource, TBaseDestination>(Mapper<TBaseSource, TBaseDestination> baseMapper) where TBaseSource : class, new() where TBaseDestination : class, new()
        {
            VerifyBaseInstanceOf<TBaseSource, TBaseDestination>();
            return Map((o, s, d) => baseMapper.Map((TBaseSource)(object)s, (TBaseDestination)(object)d, o.OperationType));
        }

        /// <summary>
        /// Adds a base mapping.
        /// </summary>
        /// <typeparam name="TMapper">The <see cref="IMapperBase"/> <see cref="Type"/>.</typeparam>
        public Mapper<TSource, TDestination> Base<TMapper>() where TMapper : IMapperBase, new()
        {
            var mapper = new TMapper();
            VerifyBaseInstanceOf(mapper.SourceType, mapper.DestinationType);
            return Map((o, s, d) => o.Owner.GetMapper(mapper.SourceType, mapper.DestinationType).Map((object)s, (object)d));
        }

        /// <summary>
        /// Verify instance of base.
        /// </summary>
        private static void VerifyBaseInstanceOf<TBaseSource, TBaseDestination>() => VerifyBaseInstanceOf(typeof(TBaseSource), typeof(TBaseDestination));

        /// <summary>
        /// Verify instance of base.
        /// </summary>
        private static void VerifyBaseInstanceOf(Type baseSourceType, Type baseDestinationType)
        {
            if (!baseSourceType.IsAssignableFrom(typeof(TSource)))
                throw new ArgumentException($"Source Type '{baseSourceType.FullName}' must be assignable from '{typeof(TSource).FullName}'.");

            if (!baseDestinationType.IsAssignableFrom(typeof(TDestination)))
                throw new ArgumentException($"Destination Type '{baseDestinationType.FullName}' must be assignable from '{typeof(TDestination).FullName}'.");
        }

        /// <inheritdoc/>
        public virtual TDestination CreateSource() => new();

        /// <inheritdoc/>
        public virtual TDestination CreateDestination() => new();

        /// <inheritdoc/>
        /// <remarks>Defaults to <c>false</c> where no <i>Map</i>, <i>Flatten</i>, <i>Expand</i> or <see cref="IsSourceInitial(Func{TSource, bool})"/> is performed; otherwise, returns result of the aforementioned explicit checks.</remarks>
        public virtual bool IsSourceInitial(TSource source)
        {
            bool somethingConfigured = false;
            foreach (var (_, _, isSourceInitial, _) in _mappings)
            {
                if (isSourceInitial is not null)
                {
                    if (!isSourceInitial(source))
                        return false;

                    somethingConfigured = true;
                }
            }

            if (somethingConfigured)
                return _isSourceInitial is null || _isSourceInitial(source);

            return _isSourceInitial?.Invoke(source) ?? false;
        }

        /// <summary>
        /// Sets the underlying function to determine whether <see cref="IsSourceInitial(TSource)"/> extending the per <i>Map</i>, <i>Flatten</i> and <i>Expand</i> specifications (provides the base <see cref="IsSourceInitial(TSource)"/> functionality).
        /// </summary>
        /// <param name="isSourceInitial">The function to determine whether the source is initial.</param>
        /// <remarks>The <paramref name="isSourceInitial"/> function must return <c>true</c> where the source is initial; otherwise, <c>false</c>.</remarks>
        public Mapper<TSource, TDestination> IsSourceInitial(Func<TSource, bool> isSourceInitial)
        {
            if (_isSourceInitial != null)
                throw new ArgumentException($"{nameof(IsSourceInitial)} cannot be invoked more than once.", nameof(isSourceInitial));

            _isSourceInitial = isSourceInitial ?? throw new ArgumentNullException(nameof(isSourceInitial));
            return this;
        }

        /// <inheritdoc/>
        /// <remarks>Defaults to <c>true</c> where explicit initialization logic is performed (per <i>Map</i>, <i>Flatten</i>, <i>Expand</i> and/or <see cref="IsSourceInitial(Func{TSource, bool})"/>); otherwise,
        /// then a newly instantiated <typeparamref name="TSource"/> is mapped to achieve initialization.</remarks>
        public virtual bool InitializeDestination(TDestination destination)
        {
            bool somethingConfigured = false;
            foreach (var (_, _, _, initializeDestination) in _mappings)
            {
                if (initializeDestination is not null)
                {
                    initializeDestination(destination);
                    somethingConfigured = true;
                }
            }

            var wasInitialized = _initializeDestination?.Invoke(destination) ?? false;
            return somethingConfigured || wasInitialized;
        }

        /// <summary>
        /// Sets the underlying function to initialize the destination properties as a result of a <see cref="Flatten"/> (provides the base <see cref="InitializeDestination(TDestination)"/> functionality).
        /// </summary>
        /// <param name="initializeDestination">The function to initialize the destination properties.</param>
        /// <remarks>The <paramref name="initializeDestination"/> function must return <c>true</c> where initialization occured; otherwise, <c>false</c>.</remarks>
        public Mapper<TSource, TDestination> InitializeDestination(Func<TDestination, bool> initializeDestination)
        {
            if (_initializeDestination != null)
                throw new ArgumentException($"{nameof(InitializeDestination)} cannot be invoked more than once.", nameof(initializeDestination));

            _initializeDestination = initializeDestination ?? throw new ArgumentNullException(nameof(initializeDestination));
            return this;
        }

        /// <inheritdoc/>
        object? IMapperBase.Map(object? source, OperationTypes operationType) => source is null ? default : Map((TSource)source, null, operationType);

        /// <inheritdoc/>
        object? IMapperBase.Map(object? source, object? destination, OperationTypes operationType) => source is null ? default : Map((TSource)source, (TDestination?)destination, operationType);

        /// <inheritdoc/>
        TDestination? IMapper<TSource, TDestination>.Map(TSource? source, OperationTypes operationType) => Map(source, null, operationType);

        /// <inheritdoc/>
        TDestination? IMapper<TSource, TDestination>.Map(TSource? source, TDestination? destination, OperationTypes operationType) => Map(source, destination, operationType);

        /// <summary>
        /// Performs the mapping by iterating over the configuration.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination.</param>
        /// <param name="operationType">The singular <see cref="OperationTypes"/>.</param>
        internal virtual TDestination? Map(TSource? source, TDestination? destination, OperationTypes operationType)
        {
            if (source is null && destination is null)
                return OnMap(source, destination, operationType);

            if (source is null && destination is not null)
            {
                destination = default;
                return OnMap(source, destination, operationType);
            }

            if (destination is not null)
                source ??= new();

            destination ??= new();
            foreach (var (action, _, _, _) in _mappings.Where(m => m.types.HasFlag(operationType)))
            {
                action(new MapperOptions(Owner, operationType), source!, destination);
            }

            return OnMap(source, destination, operationType);
        }

        /// <summary>
        /// Invoked after the internal mapping is completed (where configured).
        /// </summary>
        /// <param name="source">The source value.</param>
        /// <param name="destination">The destination value.</param>
        /// <param name="operationType">The singular <see cref="OperationTypes"/>.</param>
        /// <returns>The destination value.</returns>
        protected virtual TDestination? OnMap(TSource? source, TDestination? destination, OperationTypes operationType) => destination;

        /// <summary>
        /// Invoked when the mapper is registered.
        /// </summary>
        /// <param name="mapper">The mapper instance being registered.</param>
        protected virtual void OnRegister(Mapper<TSource, TDestination> mapper) { }
    }
}