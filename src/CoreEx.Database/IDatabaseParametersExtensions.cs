// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Database.Mapping;
using CoreEx.Entities;
using CoreEx.Mapping.Converters;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace CoreEx.Database
{
    /// <summary>
    /// Provides <see cref="IDatabaseParameters{TSelf}"/> extension methods.
    /// </summary>
    public static class IDatabaseParametersExtensions
    {
        /// <summary>
        /// Add one or more parameters by invoking a delegate.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="action">The delegate to enable parameter addition.</param>
        /// <returns>The current <see cref="DatabaseCommand"/> instance to support chaining (fluent interface).</returns>
        public static TSelf Params<TSelf>(this IDatabaseParameters<TSelf> parameters, Action<DatabaseParameterCollection> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            action.Invoke((parameters ?? throw new ArgumentNullException(nameof(parameters))).Parameters);
            return (TSelf)parameters;
        }

        /// <summary>
        /// Adds the <see cref="DbParameter"/> <paramref name="list"/>.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="list">The <see cref="DbParameter"/> list.</param>
        /// <returns>The current <see cref="DatabaseCommand"/> instance to support chaining (fluent interface).</returns>
        public static TSelf Params<TSelf>(this IDatabaseParameters<TSelf> parameters, IEnumerable<DbParameter> list)
        {
            if (list != null && list != parameters.Parameters)
                parameters.Parameters.AddRange(list);

            return (TSelf)parameters;
        }

        #region Param

        /// <summary>
        /// Adds the named parameter and value, using the specified <paramref name="direction"/>, to the <see cref="DbCommand.Parameters"/>.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="name">The parameter name.</param>
        /// <param name="value">The parameter value.</param>
        /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
        /// <returns>The <see cref="DatabaseParameterCollection"/> to support fluent-style method-chaining.</returns>
        public static TSelf Param<TSelf>(this IDatabaseParameters<TSelf> parameters, string name, object? value, ParameterDirection direction = ParameterDirection.Input)
        {
            (parameters ?? throw new ArgumentNullException(nameof(parameters))).Parameters.AddParameter(name, value, direction);
            return (TSelf)parameters;
        }

        /// <summary>
        /// Adds the named parameter and value, using the specified <paramref name="dbType"/> and <paramref name="direction"/>, to the <see cref="DbCommand.Parameters"/>.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="name">The parameter name.</param>
        /// <param name="value">The parameter value.</param>
        /// <param name="dbType">The parameter <see cref="DbType"/>.</param>
        /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
        /// <returns>The <see cref="DatabaseParameterCollection"/> to support fluent-style method-chaining.</returns>
        public static TSelf Param<TSelf>(this IDatabaseParameters<TSelf> parameters, string name, object? value, DbType dbType, ParameterDirection direction = ParameterDirection.Input)
        {
            (parameters ?? throw new ArgumentNullException(nameof(parameters))).Parameters.AddParameter(name, value, dbType, direction);
            return (TSelf)parameters;
        }

        /// <summary>
        /// Adds the named parameter and value, using the specified <paramref name="dbType"/> and <paramref name="direction"/>, to the <see cref="DbCommand.Parameters"/>.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="name">The parameter name.</param>
        /// <param name="dbType">The parameter <see cref="DbType"/>.</param>
        /// <param name="size">The maximum size (in bytes).</param>
        /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
        /// <returns>The <see cref="DatabaseParameterCollection"/> to support fluent-style method-chaining.</returns>
        public static TSelf Param<TSelf>(this IDatabaseParameters<TSelf> parameters, string name, DbType dbType, int size, ParameterDirection direction = ParameterDirection.Input)
        {
            (parameters ?? throw new ArgumentNullException(nameof(parameters))).Parameters.AddParameter(name, dbType, size, direction);
            return (TSelf)parameters;
        }

        /// <summary>
        /// Adds the named parameter and value, using the specified <paramref name="direction"/>, to the <see cref="DbCommand.Parameters"/>.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="mapper">The <see cref="IPropertyColumnMapper"/>.</param>
        /// <param name="value">The parameter value.</param>
        /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
        /// <returns>The <see cref="DatabaseParameterCollection"/> to support fluent-style method-chaining.</returns>
        public static TSelf Param<TSelf>(this IDatabaseParameters<TSelf> parameters, IPropertyColumnMapper mapper, object? value, ParameterDirection direction = ParameterDirection.Input)
            => Param(parameters, mapper?.ParameterName!, value, direction);

        /// <summary>
        /// Adds the named parameter and value, using the specified <paramref name="dbType"/> and <paramref name="direction"/>, to the <see cref="DbCommand.Parameters"/>.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="mapper">The <see cref="IPropertyColumnMapper"/>.</param>
        /// <param name="value">The parameter value.</param>
        /// <param name="dbType">The parameter <see cref="DbType"/>.</param>
        /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
        /// <returns>The <see cref="DatabaseParameterCollection"/> to support fluent-style method-chaining.</returns>
        public static TSelf Param<TSelf>(this IDatabaseParameters<TSelf> parameters, IPropertyColumnMapper mapper, object? value, DbType dbType, ParameterDirection direction = ParameterDirection.Input)
            => Param(parameters, mapper?.ParameterName!, value, dbType, direction);

        /// <summary>
        /// Adds the named parameter and value, using the specified <paramref name="dbType"/> and <paramref name="direction"/>, to the <see cref="DbCommand.Parameters"/>.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="mapper">The <see cref="IPropertyColumnMapper"/>.</param>
        /// <param name="dbType">The parameter <see cref="DbType"/>.</param>
        /// <param name="size">The maximum size (in bytes).</param>
        /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
        /// <returns>The <see cref="DatabaseParameterCollection"/> to support fluent-style method-chaining.</returns>
        public static TSelf Param<TSelf>(this IDatabaseParameters<TSelf> parameters, IPropertyColumnMapper mapper, DbType dbType, int size, ParameterDirection direction = ParameterDirection.Input)
            => Param(parameters, mapper?.ParameterName!, dbType, size, direction);

        #endregion

        #region ParamWhen

        /// <summary>
        /// Adds a named parameter and value <paramref name="when"/> <c>true</c>.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <typeparam name="T">The parameter <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="when">Adds the parameter when <c>true</c>.</param>
        /// <param name="name">The parameter name.</param>
        /// <param name="value">The parameter value.</param>
        /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
        /// <returns>The <see cref="DatabaseParameterCollection"/> to support fluent-style method-chaining.</returns>
        public static TSelf ParamWhen<TSelf, T>(this IDatabaseParameters<TSelf> parameters, bool? when, string name, Func<T> value, ParameterDirection direction = ParameterDirection.Input)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (when == true)
                (parameters ?? throw new ArgumentNullException(nameof(parameters))).Parameters.AddParameter(name, value(), direction);

            return (TSelf)parameters;
        }

        /// <summary>
        /// Adds a named parameter and value <paramref name="when"/> <c>true</c>.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <typeparam name="T">The parameter <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="when">Adds the parameter when <c>true</c>.</param>
        /// <param name="name">The parameter name.</param>
        /// <param name="value">The parameter value.</param>
        /// <param name="dbType">The parameter <see cref="DbType"/>.</param>
        /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
        /// <returns>The <see cref="DatabaseParameterCollection"/> to support fluent-style method-chaining.</returns>
        public static TSelf ParamWhen<TSelf, T>(this IDatabaseParameters<TSelf> parameters, bool? when, string name, Func<T> value, DbType dbType, ParameterDirection direction = ParameterDirection.Input)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (when == true)
                (parameters ?? throw new ArgumentNullException(nameof(parameters))).Parameters.AddParameter(name, value(), dbType, direction);

            return (TSelf)parameters;
        }

        /// <summary>
        /// Adds a named parameter and value <paramref name="when"/> <c>true</c>.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <typeparam name="T">The parameter <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="when">Adds the parameter when <c>true</c>.</param>
        /// <param name="mapper">The <see cref="IPropertyColumnMapper"/>.</param>
        /// <param name="value">The parameter value.</param>
        /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
        /// <returns>The <see cref="DatabaseParameterCollection"/> to support fluent-style method-chaining.</returns>
        public static TSelf ParamWhen<TSelf, T>(this IDatabaseParameters<TSelf> parameters, bool? when, IPropertyColumnMapper mapper, Func<T> value, ParameterDirection direction = ParameterDirection.Input)
            => ParamWhen(parameters, when, mapper?.ParameterName!, value, direction);

        /// <summary>
        /// Adds a named parameter and value <paramref name="when"/> <c>true</c>.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <typeparam name="T">The parameter <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="when">Adds the parameter when <c>true</c>.</param>
        /// <param name="mapper">The <see cref="IPropertyColumnMapper"/>.</param>
        /// <param name="value">The parameter value.</param>
        /// <param name="dbType">The parameter <see cref="DbType"/>.</param>
        /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
        /// <returns>The <see cref="DatabaseParameterCollection"/> to support fluent-style method-chaining.</returns>
        public static TSelf ParamWhen<TSelf, T>(this IDatabaseParameters<TSelf> parameters, bool? when, IPropertyColumnMapper mapper, Func<T> value, DbType dbType, ParameterDirection direction = ParameterDirection.Input)
            => ParamWhen(parameters, when, mapper?.ParameterName!, value, dbType, direction);

        #endregion

        #region ParamWith

        /// <summary>
        /// Adds a named parameter when invoked <paramref name="with"/> a non-default value.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <typeparam name="T">The parameter <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="with">The value <b>with</b> which to verify is non-default.</param>
        /// <param name="name">The parameter name.</param>
        /// <param name="value">The parameter value.</param>
        /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
        /// <returns>The current <see cref="DatabaseParameterCollection"/> instance to support chaining (fluent interface).</returns>
        public static TSelf ParamWith<TSelf, T>(this IDatabaseParameters<TSelf> parameters, object? with, string name, Func<T> value, ParameterDirection direction = ParameterDirection.Input)
            => ParamWhen(parameters, with != null && Comparer<T>.Default.Compare((T)with, default!) != 0, name, value, direction);

        /// <summary>
        /// Adds a named parameter when invoked <paramref name="with"/> a non-default value.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <typeparam name="T">The parameter <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="with">The value <b>with</b> which to verify is non-default.</param>
        /// <param name="name">The parameter name.</param>
        /// <param name="value">The parameter value; where not specified the <paramref name="with"/> value will be used.</param>
        /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
        /// <returns>The current <see cref="DatabaseParameterCollection"/> instance to support chaining (fluent interface).</returns>
        public static TSelf ParamWith<TSelf, T>(this IDatabaseParameters<TSelf> parameters, T? with, string name, Func<T>? value = null, ParameterDirection direction = ParameterDirection.Input)
            => ParamWhen(parameters, with != null && Comparer<T>.Default.Compare(with, default!) != 0, name, value ?? (() => with!), direction);

        /// <summary>
        /// Adds a named parameter when invoked <paramref name="with"/> a non-default value.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <typeparam name="T">The parameter <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="with">The value <b>with</b> which to verify is non-default.</param>
        /// <param name="name">The parameter name.</param>
        /// <param name="value">The parameter value.</param>
        /// <param name="dbType">The parameter <see cref="DbType"/>.</param>
        /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
        /// <returns>The current <see cref="DatabaseParameterCollection"/> instance to support chaining (fluent interface).</returns>
        public static TSelf ParamWith<TSelf, T>(this IDatabaseParameters<TSelf> parameters, object? with, string name, Func<T> value, DbType dbType, ParameterDirection direction = ParameterDirection.Input)
            => ParamWhen(parameters, with != null && Comparer<T>.Default.Compare((T)with, default!) != 0, name, value, dbType, direction);

        /// <summary>
        /// Adds a named parameter when invoked <paramref name="with"/> a non-default value.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <typeparam name="T">The parameter <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="with">The value <b>with</b> which to verify is non-default.</param>
        /// <param name="name">The parameter name.</param>
        /// <param name="value">The parameter value; where not specified the <paramref name="with"/> value will be used.</param>
        /// <param name="dbType">The parameter <see cref="DbType"/>.</param>
        /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
        /// <returns>The current <see cref="DatabaseParameterCollection"/> instance to support chaining (fluent interface).</returns>
        public static TSelf ParamWith<TSelf, T>(this IDatabaseParameters<TSelf> parameters, T? with, string name, Func<T>? value, DbType dbType, ParameterDirection direction = ParameterDirection.Input)
            => ParamWhen(parameters, with != null && Comparer<T>.Default.Compare(with, default!) != 0, name, value ?? (() => with!), dbType, direction);

        /// <summary>
        /// Adds a named parameter when invoked <paramref name="with"/> a non-default value.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <typeparam name="T">The parameter <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="with">The value <b>with</b> which to verify is non-default.</param>
        /// <param name="mapper">The <see cref="IPropertyColumnMapper"/>.</param>
        /// <param name="value">The parameter value.</param>
        /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
        /// <returns>The current <see cref="DatabaseParameterCollection"/> instance to support chaining (fluent interface).</returns>
        public static TSelf ParamWith<TSelf, T>(this IDatabaseParameters<TSelf> parameters, object? with, IPropertyColumnMapper mapper, Func<T> value, ParameterDirection direction = ParameterDirection.Input)
            => ParamWith(parameters, with, mapper, value, direction);

        /// <summary>
        /// Adds a named parameter when invoked <paramref name="with"/> a non-default value.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <typeparam name="T">The parameter <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="with">The value <b>with</b> which to verify is non-default.</param>
        /// <param name="mapper">The <see cref="IPropertyColumnMapper"/>.</param>
        /// <param name="value">The parameter value; where not specified the <paramref name="with"/> value will be used.</param>
        /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
        /// <returns>The current <see cref="DatabaseParameterCollection"/> instance to support chaining (fluent interface).</returns>
        public static TSelf ParamWith<TSelf, T>(this IDatabaseParameters<TSelf> parameters, T? with, IPropertyColumnMapper mapper, Func<T>? value = null, ParameterDirection direction = ParameterDirection.Input)
            => ParamWith(parameters, with, mapper, value ?? (() => with!), direction);

        /// <summary>
        /// Adds a named parameter when invoked <paramref name="with"/> a non-default value.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <typeparam name="T">The parameter <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="with">The value <b>with</b> which to verify is non-default.</param>
        /// <param name="mapper">The <see cref="IPropertyColumnMapper"/>.</param>
        /// <param name="value">The parameter value.</param>
        /// <param name="dbType">The parameter <see cref="DbType"/>.</param>
        /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
        /// <returns>The current <see cref="DatabaseParameterCollection"/> instance to support chaining (fluent interface).</returns>
        public static TSelf ParamWith<TSelf, T>(this IDatabaseParameters<TSelf> parameters, object? with, IPropertyColumnMapper mapper, Func<T> value, DbType dbType, ParameterDirection direction = ParameterDirection.Input)
            => ParamWith(parameters, with, mapper, value, dbType, direction);

        /// <summary>
        /// Adds a named parameter when invoked <paramref name="with"/> a non-default value.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <typeparam name="T">The parameter <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="with">The value <b>with</b> which to verify is non-default.</param>
        /// <param name="mapper">The <see cref="IPropertyColumnMapper"/>.</param>
        /// <param name="value">The parameter value; where not specified the <paramref name="with"/> value will be used.</param>
        /// <param name="dbType">The parameter <see cref="DbType"/>.</param>
        /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
        /// <returns>The current <see cref="DatabaseParameterCollection"/> instance to support chaining (fluent interface).</returns>
        public static TSelf ParamWith<TSelf, T>(this IDatabaseParameters<TSelf> parameters, T? with, IPropertyColumnMapper mapper, Func<T>? value, DbType dbType, ParameterDirection direction = ParameterDirection.Input)
            => ParamWith(parameters, with, mapper, value ?? (() => with!), dbType, direction);

        #endregion

        #region ParamWithWildcard

        /// <summary>
        /// Adds a named parameter when invoked with a non-default <paramref name="wildcard"/> (converted for the database).
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="wildcard">The wildcard <b>with</b> which to verify is non-default and apply.</param>
        /// <param name="name">The parameter name.</param>
        /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
        /// <returns>The current <see cref="DatabaseParameterCollection"/> instance to support chaining (fluent interface).</returns>
        public static TSelf ParamWithWildcard<TSelf>(this IDatabaseParameters<TSelf> parameters, string? wildcard, string name, ParameterDirection direction = ParameterDirection.Input)
            => ParamWith(parameters, wildcard, name, () => parameters.Database.Wildcard.Replace(wildcard), direction);

        /// <summary>
        /// Adds a named parameter when invoked with a non-default <paramref name="wildcard"/> (converted for the database).
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="wildcard">The wildcard <b>with</b> which to verify is non-default and apply.</param>
        /// <param name="mapper">The <see cref="IPropertyColumnMapper"/>.</param>
        /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
        /// <returns>The current <see cref="DatabaseParameterCollection"/> instance to support chaining (fluent interface).</returns>
        public static TSelf ParamWithWildcard<TSelf>(this IDatabaseParameters<TSelf> parameters, string? wildcard, IPropertyColumnMapper mapper, ParameterDirection direction = ParameterDirection.Input)
            => ParamWithWildcard(parameters, wildcard, mapper?.ParameterName!, direction);

        #endregion

        #region RowVersionParam

        /// <summary>
        /// Adds a named parameter with a <b>RowVersion</b> value.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="name">The parameter name.</param>
        /// <param name="value">The row version value.</param>
        /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
        /// <returns>The current <see cref="DatabaseParameterCollection"/> instance to support chaining (fluent interface).</returns>
        public static TSelf RowVersionParam<TSelf>(this IDatabaseParameters<TSelf> parameters, string name, string? value, ParameterDirection direction = ParameterDirection.Input)
            => Param(parameters, name ?? parameters.Database.DatabaseColumns.RowVersionName, new StringToBase64Converter().ToDestination.Convert(value), direction);

        /// <summary>
        /// Adds a named parameter with a <b>RowVersion</b> value.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="mapper">The <see cref="IPropertyColumnMapper"/>.</param>
        /// <param name="value">The row version value.</param>
        /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
        /// <returns>The current <see cref="DatabaseParameterCollection"/> instance to support chaining (fluent interface).</returns>
        public static TSelf RowVersionParam<TSelf>(this IDatabaseParameters<TSelf> parameters, IPropertyColumnMapper mapper, string? value, ParameterDirection direction = ParameterDirection.Input)
            => RowVersionParam(parameters, mapper?.ParameterName!, value, direction);

        /// <summary>
        /// Adds a named (<see cref="Extended.DatabaseColumns.RowVersionName"/>) parameter with a <b>RowVersion</b> value.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="value">The row version value.</param>
        /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
        /// <returns>The current <see cref="DatabaseParameterCollection"/> instance to support chaining (fluent interface).</returns>
        public static TSelf RowVersionParam<TSelf>(this IDatabaseParameters<TSelf> parameters, string? value, ParameterDirection direction = ParameterDirection.Input)
            => RowVersionParam(parameters, parameters.Database.DatabaseColumns.RowVersionName, value, direction);

        #endregion

        #region ReselectRecordParam

        /// <summary>
        /// Adds a named parameter (<see cref="Extended.DatabaseColumns.ReselectRecordName"/>) to <paramref name="reselect"/> the data.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="reselect">Indicates whether to reselect after the operation.</param>
        /// <returns>The current <see cref="DatabaseParameterCollection"/> instance to support chaining (fluent interface).</returns>
        public static TSelf ReselectRecordParam<TSelf>(this IDatabaseParameters<TSelf> parameters, bool reselect = true)
        {
            (parameters ?? throw new ArgumentNullException(nameof(parameters))).Parameters.AddReselectRecordParam(reselect);
            return (TSelf)parameters;
        }

        /// <summary>
        /// Adds a named parameter (<see cref="Extended.DatabaseColumns.ReselectRecordName"/>) to <paramref name="reselect"/> the data <paramref name="when"/> <c>true</c>.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="when">Adds the parameter when <c>true</c>.</param>
        /// <param name="reselect">Indicates whether to reselect after the operation.</param>
        /// <returns>The current <see cref="DatabaseParameterCollection"/> instance to support chaining (fluent interface).</returns>
        public static TSelf ReselectRecordParamWhen<TSelf>(this IDatabaseParameters<TSelf> parameters, bool? when, bool reselect = true)
        {
            if (when == true)
                ReselectRecordParam(parameters, reselect);

            return (TSelf)parameters;
        }

        #endregion

        #region PagingParams

        /// <summary>
        /// Adds the <see cref="PagingArgs"/> as parameters.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="paging">The <see cref="PagingArgs"/>.</param>
        /// <returns>The current <see cref="DatabaseParameterCollection"/> instance to support chaining (fluent interface).</returns>
        public static TSelf PagingParams<TSelf>(this IDatabaseParameters<TSelf> parameters, PagingArgs? paging)
        {
            if (paging != null)
            {
                parameters.Param(parameters.Database.DatabaseColumns.PagingSkipName, paging.Skip);
                parameters.Param(parameters.Database.DatabaseColumns.PagingTakeName, paging.Take);
                parameters.ParamWhen(paging.IsGetCount, parameters.Database.DatabaseColumns.PagingCountName, () => paging.IsGetCount);
            }

            return (TSelf)parameters;
        }

        #endregion 
    }
}