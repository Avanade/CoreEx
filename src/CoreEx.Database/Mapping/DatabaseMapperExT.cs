// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Mapping;
using System;

namespace CoreEx.Database.Mapping
{
    /// <summary>
    /// Provides mapping from a <typeparamref name="TSource"/> <see cref="Type"/> and database with the extended/explicitly provided logic.
    /// </summary>
    /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
    /// <param name="mapFromDb">The <see cref="MapFromDb(DatabaseRecord, OperationTypes)"/> implementation.</param>
    /// <param name="mapKeyToDb">The <see cref="IDatabaseMapper.MapKeyToDb(CompositeKey, DatabaseParameterCollection)"/> implementation.</param>
    /// <param name="mapToDb">The <see cref="MapToDb(TSource?, DatabaseParameterCollection, OperationTypes)"/> implementation.</param>
    /// <remarks>This enables the most optimized performance by enabling explicit code to be specified.</remarks>
    public class DatabaseMapperEx<TSource>(Action<DatabaseRecord, TSource, OperationTypes>? mapFromDb = null, Action<CompositeKey, DatabaseParameterCollection>? mapKeyToDb = null, Action<TSource?, DatabaseParameterCollection, OperationTypes>? mapToDb = null) : IDatabaseMapperEx<TSource> where TSource : class, new()
    {
        private readonly Action<DatabaseRecord, TSource, OperationTypes>? _mapFromDb = mapFromDb;
        private readonly Action<TSource?, DatabaseParameterCollection, OperationTypes>? _mapToDb = mapToDb;
        private readonly Action<CompositeKey, DatabaseParameterCollection>? _mapKeyToDb = mapKeyToDb;
        private IDatabaseMapperEx? _extendMapper;

        /// <summary>
        /// Indicates that a <c>null</c> should be returned from <see cref="MapFromDb(DatabaseRecord, OperationTypes)"/> where the resulting value <see cref="IInitial.IsInitial"/>.
        /// </summary>
        /// <remarks>Defaults to <c>true</c>.</remarks>
        public bool NullWhenInitial { get; set; } = true;

        /// <summary>
        /// Inherits (includes) the mappings from the selected <paramref name="baseMapper"/>.
        /// </summary>
        /// <typeparam name="TBase">The <paramref name="baseMapper"/> source <see cref="Type"/>; <typeparamref name="TSource"/> must inherit from <typeparamref name="TBase"/>.</typeparam>
        /// <param name="baseMapper">The <see cref="IDatabaseMapperEx{TSource}"/> to inherit from.</param>
        /// <returns>The <see cref="DatabaseMapperEx{TSource}"/> to support fluent-style method-chaining.</returns>
        public DatabaseMapperEx<TSource> InheritMapper<TBase>(IDatabaseMapperEx<TBase> baseMapper) where TBase : class, new()
        {
            if (_extendMapper is not null)
                throw new InvalidOperationException($"An {nameof(InheritMapper)} may only be invoked once for a mapper.");

            if (!typeof(TSource).IsSubclassOf(typeof(TBase)))
                throw new ArgumentException($"Type {typeof(TSource).Name} must inherit from {typeof(TBase).Name}.", nameof(baseMapper));

            _extendMapper = baseMapper.ThrowIfNull(nameof(baseMapper));
            return this;
        }

        /// <inheritdoc/>
        public void MapFromDb(DatabaseRecord record, TSource value, OperationTypes operationType)
        {
            record.ThrowIfNull(nameof(record));
            value.ThrowIfNull(nameof(value));

            _extendMapper?.MapFromDb(record, value, operationType);
            _mapFromDb?.Invoke(record, value, operationType);
            OnMapFromDb(record, value, operationType);
        }

        /// <inheritdoc/>
        public TSource? MapFromDb(DatabaseRecord record, OperationTypes operationType = OperationTypes.Unspecified)
        {
            var value = new TSource();
            MapFromDb(record, value, operationType);
            return NullWhenInitial ? ((value is not null && value is IInitial ii && ii.IsInitial) ? null : value) : null;
        }

        /// <summary>
        /// Extension opportunity when performing a <see cref="MapFromDb(DatabaseRecord, OperationTypes)"/>.
        /// </summary>
        /// <param name="value">The source value.</param>
        /// <param name="record">The <see cref="DatabaseRecord"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        /// <returns>The source value.</returns>
        protected virtual void OnMapFromDb(DatabaseRecord record, TSource value, OperationTypes operationType) { }

        /// <inheritdoc/>
        public void MapToDb(TSource? value, DatabaseParameterCollection parameters, OperationTypes operationType = OperationTypes.Unspecified)
        {
            parameters.ThrowIfNull(nameof(parameters));
            if (value is not null)
            {
                _extendMapper?.MapToDb(value, parameters, operationType);
                _mapToDb?.Invoke(value, parameters, operationType);
                OnMapToDb(value, parameters, operationType);
            }
        }

        /// <summary>
        /// Extension opportunity when performing a <see cref="MapToDb(TSource, DatabaseParameterCollection, OperationTypes)"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="parameters">The <see cref="DatabaseParameterCollection"/> to update from the <paramref name="value"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        protected virtual void OnMapToDb(TSource value, DatabaseParameterCollection parameters, OperationTypes operationType) { }

        /// <inheritdoc/>
        void IDatabaseMapper.MapKeyToDb(CompositeKey key, DatabaseParameterCollection parameters)
        {
            parameters.ThrowIfNull(nameof(parameters));
            _extendMapper?.MapKeyToDb(key, parameters);
            _mapKeyToDb?.Invoke(key, parameters);
            OnMapKeyToDb(key, parameters);
        }

        /// <summary>
        /// Extension opportunity when performing a <see cref="IDatabaseMapper.MapKeyToDb"/>.
        /// </summary>
        /// <param name="key">The primary <see cref="CompositeKey"/>.</param>
        /// <param name="parameters">The <see cref="DatabaseParameterCollection"/>.</param>
        /// <remarks>This is used to map the only the key parameters; for example a <b>Get</b> or <b>Delete</b> operation.</remarks>
        protected virtual void OnMapKeyToDb(CompositeKey key, DatabaseParameterCollection parameters) { }

        #region When*

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

        #endregion
    }
}