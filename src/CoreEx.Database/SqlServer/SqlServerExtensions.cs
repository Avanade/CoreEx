// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Database.Extended;
using CoreEx.Database.Mapping;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace CoreEx.Database.SqlServer
{
    /// <summary>
    /// Provides <see href="https://docs.microsoft.com/en-us/sql/connect/ado-net/microsoft-ado-net-sql-server">SQL Server</see> extension methods.
    /// </summary>
    public static class SqlServerExtensions
    {
        /// <summary>
        /// Adds the named parameter and value, using the specified <see cref="DbType"/> and <see cref="ParameterDirection"/>, to the <see cref="DbCommand.Parameters"/>.
        /// </summary>
        /// <param name="dpc">The <see cref="DatabaseParameterCollection"/>.</param>
        /// <param name="name">The parameter name.</param>
        /// <param name="value">The parameter value.</param>
        /// <param name="sqlDbType">The parameter <see cref="SqlDbType"/>.</param>
        /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
        /// <returns>A <see cref="DbParameter"/>.</returns>
        public static SqlParameter AddParameter(this DatabaseParameterCollection dpc, string name, object? value, SqlDbType? sqlDbType = null, ParameterDirection direction = ParameterDirection.Input)
        {
            var p = (SqlParameter)(dpc ?? throw new ArgumentNullException(nameof(dpc))).Database.Provider.CreateParameter();
            p.ParameterName = DatabaseParameterCollection.ParameterizeName(name);
            if (sqlDbType.HasValue)
                p.SqlDbType = sqlDbType.Value;

            p.Value = value;
            p.Direction = direction;

            dpc.Add(p);
            return p;
        }

        /// <summary>
        /// Adds the named <see cref="TableValuedParameter"/> value to the <see cref="DbCommand.Parameters"/>.
        /// </summary>
        /// <param name="dpc">The <see cref="DatabaseParameterCollection"/>.</param>
        /// <param name="name">The parameter name.</param>
        /// <param name="tvp">The <see cref="TableValuedParameter"/> value.</param>
        /// <returns>A <see cref="DbParameter"/>.</returns>
        /// <remarks>This specifically implies that the <see cref="SqlParameter"/> is being used; if not then an exception will be thrown.</remarks>
        public static SqlParameter AddTableValuedParameter(this DatabaseParameterCollection dpc, string name, TableValuedParameter tvp)
        {
            var p = (SqlParameter)(dpc ?? throw new ArgumentNullException(nameof(dpc))).Database.Provider.CreateParameter();
            p.ParameterName = DatabaseParameterCollection.ParameterizeName(name);
            p.SqlDbType = SqlDbType.Structured;
            p.TypeName = (tvp ?? throw new ArgumentNullException(nameof(tvp))).TypeName;
            p.Value = tvp.Value;
            p.Direction = ParameterDirection.Input;

            dpc.Add(p);
            return p;
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
        /// <param name="sqlDbType">The parameter <see cref="SqlDbType"/>.</param>
        /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
        /// <returns>The <see cref="DatabaseParameterCollection"/> to support fluent-style method-chaining.</returns>
        public static TSelf ParamWhen<TSelf, T>(this IDatabaseParameters<TSelf> parameters, bool? when, string name, Func<T> value, SqlDbType sqlDbType, ParameterDirection direction = ParameterDirection.Input)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (when == true)
                parameters.Parameters.AddParameter(name, value(), sqlDbType, direction);

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
        /// <param name="sqlDbType">The parameter <see cref="SqlDbType"/>.</param>
        /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
        /// <returns>The <see cref="DatabaseParameterCollection"/> to support fluent-style method-chaining.</returns>
        public static TSelf ParamWhen<TSelf, T>(this IDatabaseParameters<TSelf> parameters, bool? when, IPropertyColumnMapper mapper, Func<T> value, SqlDbType sqlDbType, ParameterDirection direction = ParameterDirection.Input)
            => ParamWhen(parameters, when, mapper?.ParameterName!, value, sqlDbType, direction);

        /// <summary>
        /// Adds a named parameter when invoked <paramref name="with"/> a non-default value.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <typeparam name="T">The parameter <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="with">The value <b>with</b> which to verify is non-default.</param>
        /// <param name="name">The parameter name.</param>
        /// <param name="value">The parameter value.</param>
        /// <param name="sqlDbType">The parameter <see cref="SqlDbType"/>.</param>
        /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
        /// <returns>The current <see cref="DatabaseParameterCollection"/> instance to support chaining (fluent interface).</returns>
        public static TSelf ParamWith<TSelf, T>(this IDatabaseParameters<TSelf> parameters, object? with, string name, Func<T> value, SqlDbType sqlDbType, ParameterDirection direction = ParameterDirection.Input)
            => ParamWhen(parameters, with != null && Comparer<T>.Default.Compare((T)with, default!) != 0, name, value, sqlDbType, direction);

        /// <summary>
        /// Adds a named parameter when invoked <paramref name="with"/> a non-default value.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <typeparam name="T">The parameter <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="with">The value <b>with</b> which to verify is non-default.</param>
        /// <param name="name">The parameter name.</param>
        /// <param name="value">The parameter value; where not specified the <paramref name="with"/> vaue will be used.</param>
        /// <param name="sqlDbType">The parameter <see cref="SqlDbType"/>.</param>
        /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
        /// <returns>The current <see cref="DatabaseParameterCollection"/> instance to support chaining (fluent interface).</returns>
        public static TSelf ParamWith<TSelf, T>(this IDatabaseParameters<TSelf> parameters, T? with, string name, Func<T>? value, SqlDbType sqlDbType, ParameterDirection direction = ParameterDirection.Input)
            => ParamWhen(parameters, with != null && Comparer<T>.Default.Compare(with, default!) != 0, name, value ?? (() => with!), sqlDbType, direction);

        /// <summary>
        /// Adds a named parameter when invoked <paramref name="with"/> a non-default value.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <typeparam name="T">The parameter <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="with">The value <b>with</b> which to verify is non-default.</param>
        /// <param name="mapper">The <see cref="IPropertyColumnMapper"/>.</param>
        /// <param name="value">The parameter value.</param>
        /// <param name="sqlDbType">The parameter <see cref="SqlDbType"/>.</param>
        /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
        /// <returns>The current <see cref="DatabaseParameterCollection"/> instance to support chaining (fluent interface).</returns>
        public static TSelf ParamWith<TSelf, T>(this IDatabaseParameters<TSelf> parameters, object? with, IPropertyColumnMapper mapper, Func<T> value, SqlDbType sqlDbType, ParameterDirection direction = ParameterDirection.Input)
            => ParamWith(parameters, with, mapper?.ParameterName!, value, sqlDbType, direction);

        /// <summary>
        /// Adds a named parameter when invoked <paramref name="with"/> a non-default value.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <typeparam name="T">The parameter <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="with">The value <b>with</b> which to verify is non-default.</param>
        /// <param name="mapper">The <see cref="IPropertyColumnMapper"/>.</param>
        /// <param name="value">The parameter value; where not specified the <paramref name="with"/> vaue will be used.</param>
        /// <param name="sqlDbType">The parameter <see cref="SqlDbType"/>.</param>
        /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
        /// <returns>The current <see cref="DatabaseParameterCollection"/> instance to support chaining (fluent interface).</returns>
        public static TSelf ParamWith<TSelf, T>(this IDatabaseParameters<TSelf> parameters, T? with, IPropertyColumnMapper mapper, Func<T>? value, SqlDbType sqlDbType, ParameterDirection direction = ParameterDirection.Input)
            => ParamWith(parameters, with, mapper?.ParameterName!, value, sqlDbType, direction);

        /// <summary>
        /// Adds the named <see cref="TableValuedParameter"/> value to the <see cref="DbCommand.Parameters"/>.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="name">The parameter name.</param>
        /// <param name="tvp">The <see cref="TableValuedParameter"/> value.</param>
        /// <returns>The <see cref="DatabaseParameterCollection"/> to support fluent-style method-chaining.</returns>
        /// <remarks>This specifically implies that the <see cref="SqlParameter"/> is being used; if not then an exception will be thrown.</remarks>
        public static TSelf TableValuedParam<TSelf>(this IDatabaseParameters<TSelf> parameters, string name, TableValuedParameter tvp)
        {
            AddTableValuedParameter(parameters.Parameters, name, tvp);
            return (TSelf)parameters;
        }

        /// <summary>
        /// Adds the named <see cref="TableValuedParameter"/> value to the <see cref="DbCommand.Parameters"/> <paramref name="when"/> <c>true</c>.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="when">Adds the parameter when <c>true</c>.</param>
        /// <param name="name">The parameter name.</param>
        /// <param name="tvp">The <see cref="TableValuedParameter"/> value.</param>
        /// <returns>The <see cref="DatabaseParameterCollection"/> to support fluent-style method-chaining.</returns>
        /// <remarks>This specifically implies that the <see cref="SqlParameter"/> is being used; if not then an exception will be thrown.</remarks>
        public static TSelf TableValuedParamWhen<TSelf>(this IDatabaseParameters<TSelf> parameters, bool? when, string name, Func<TableValuedParameter> tvp)
        {
            if (when == true)
                TableValuedParam(parameters, name, tvp());

            return (TSelf)parameters;
        }

        /// <summary>
        /// Adds the named <see cref="TableValuedParameter"/> value to the <see cref="DbCommand.Parameters"/> <paramref name="with"/> a non-default value.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="with">The value <b>with</b> which to verify is non-default.</param>
        /// <param name="name">The parameter name.</param>
        /// <param name="tvp">The <see cref="TableValuedParameter"/> value.</param>
        /// <returns>The <see cref="DatabaseParameterCollection"/> to support fluent-style method-chaining.</returns>
        /// <remarks>This specifically implies that the <see cref="SqlParameter"/> is being used; if not then an exception will be thrown.</remarks>
        public static TSelf TableValuedParamWith<TSelf>(this IDatabaseParameters<TSelf> parameters, object? with, string name, Func<TableValuedParameter> tvp)
            => TableValuedParamWhen(parameters, with != null, name, tvp);

        /// <summary>
        /// Adds the named <see cref="TableValuedParameter"/> value to the <see cref="DbCommand.Parameters"/>.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="mapper">The <see cref="IPropertyColumnMapper"/>.</param>
        /// <param name="tvp">The <see cref="TableValuedParameter"/> value.</param>
        /// <returns>The <see cref="DatabaseParameterCollection"/> to support fluent-style method-chaining.</returns>
        /// <remarks>This specifically implies that the <see cref="SqlParameter"/> is being used; if not then an exception will be thrown.</remarks>
        public static TSelf TableValuedParam<TSelf>(this IDatabaseParameters<TSelf> parameters, IPropertyColumnMapper mapper, TableValuedParameter tvp)
            => TableValuedParam(parameters, mapper?.ParameterName!, tvp);

        /// <summary>
        /// Adds the named <see cref="TableValuedParameter"/> value to the <see cref="DbCommand.Parameters"/> <paramref name="when"/> <c>true</c>.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="when">Adds the parameter when <c>true</c>.</param>
        /// <param name="mapper">The <see cref="IPropertyColumnMapper"/>.</param>
        /// <param name="tvp">The <see cref="TableValuedParameter"/> value.</param>
        /// <returns>The <see cref="DatabaseParameterCollection"/> to support fluent-style method-chaining.</returns>
        /// <remarks>This specifically implies that the <see cref="SqlParameter"/> is being used; if not then an exception will be thrown.</remarks>
        public static TSelf TableValuedParamWhen<TSelf>(this IDatabaseParameters<TSelf> parameters, bool? when, IPropertyColumnMapper mapper, Func<TableValuedParameter> tvp)
            => TableValuedParamWhen(parameters, when, mapper?.ParameterName!, tvp);

        /// <summary>
        /// Adds the named <see cref="TableValuedParameter"/> value to the <see cref="DbCommand.Parameters"/> <paramref name="with"/> a non-default value.
        /// </summary>
        /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
        /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
        /// <param name="with">The value <b>with</b> which to verify is non-default.</param>
        /// <param name="mapper">The <see cref="IPropertyColumnMapper"/>.</param>
        /// <param name="tvp">The <see cref="TableValuedParameter"/> value.</param>
        /// <returns>The <see cref="DatabaseParameterCollection"/> to support fluent-style method-chaining.</returns>
        /// <remarks>This specifically implies that the <see cref="SqlParameter"/> is being used; if not then an exception will be thrown.</remarks>
        public static TSelf TableValuedParamWith<TSelf>(this IDatabaseParameters<TSelf> parameters, object? with, IPropertyColumnMapper mapper, Func<TableValuedParameter> tvp)
            => TableValuedParamWith(parameters, with != null, mapper?.ParameterName!, tvp);

        #region CreateTableValuedParameter

        /// <summary>
        /// Creates a <see cref="DatabaseColumns.TvpStringListTypeName"/> <see cref="TableValuedParameter"/> for the <see cref="string"/> <paramref name="list"/>.
        /// </summary>
        /// <param name="database">The<see cref="IDatabase"/>.</param>
        /// <param name="list">The list.</param>
        /// <returns>The <see cref="TableValuedParameter"/>.</returns>
        public static TableValuedParameter CreateTableValuedParameter(this IDatabase database, IEnumerable<string?> list) => CreateTableValuedParameter(database, database.DatabaseColumns.TvpStringListTypeName, list);

        /// <summary>
        /// Creates a <see cref="TableValuedParameter"/> for the <see cref="string"/> <paramref name="list"/>.
        /// </summary>
        /// <param name="database">The<see cref="IDatabase"/>.</param>
        /// <param name="typeName">The SQL type name of the table-valued parameter.</param>
        /// <param name="list">The list.</param>
        /// <returns>The <see cref="TableValuedParameter"/>.</returns>
        public static TableValuedParameter CreateTableValuedParameter(this IDatabase database, string typeName, IEnumerable<string?> list)
        {
            using var dt = new DataTable();
            dt.Columns.Add(database.DatabaseColumns.TvpListValueColumnName, typeof(string));

            if (list != null)
            {
                foreach (var item in list)
                {
                    dt.Rows.Add(item);
                }
            }

            return new TableValuedParameter(typeName, dt);
        }

        /// <summary>
        /// Creates a <see cref="DatabaseColumns.TvpInt32ListTypeName"/> <see cref="TableValuedParameter"/> for the <see cref="int"/> <paramref name="list"/>.
        /// </summary>
        /// <param name="database">The<see cref="IDatabase"/>.</param>
        /// <param name="list">The list.</param>
        /// <returns>The <see cref="TableValuedParameter"/>.</returns>
        public static TableValuedParameter CreateTableValuedParameter(this IDatabase database, IEnumerable<int> list) => CreateTableValuedParameter(database, database.DatabaseColumns.TvpInt32ListTypeName, list);

        /// <summary>
        /// Creates a <see cref="TableValuedParameter"/> for the <see cref="int"/> <paramref name="list"/>.
        /// </summary>
        /// <param name="database">The<see cref="IDatabase"/>.</param>
        /// <param name="typeName">The SQL type name of the table-valued parameter.</param>
        /// <param name="list">The list.</param>
        /// <returns>The <see cref="TableValuedParameter"/>.</returns>
        public static TableValuedParameter CreateTableValuedParameter(this IDatabase database, string typeName, IEnumerable<int> list)
        {
            using var dt = new DataTable();
            dt.Columns.Add(database.DatabaseColumns.TvpListValueColumnName, typeof(int));

            if (list != null)
            {
                foreach (var item in list)
                {
                    dt.Rows.Add(item);
                }
            }

            return new TableValuedParameter(typeName, dt);
        }

        /// <summary>
        /// Creates a <see cref="DatabaseColumns.TvpInt64ListTypeName"/> <see cref="TableValuedParameter"/> for the <see cref="long"/> <paramref name="list"/>.
        /// </summary>
        /// <param name="database">The<see cref="IDatabase"/>.</param>
        /// <param name="list">The list.</param>
        /// <returns>The <see cref="TableValuedParameter"/>.</returns>
        public static TableValuedParameter CreateTableValuedParameter(this IDatabase database, IEnumerable<long> list) => CreateTableValuedParameter(database, database.DatabaseColumns.TvpInt64ListTypeName, list);

        /// <summary>
        /// Creates a <see cref="TableValuedParameter"/> for the <see cref="long"/> <paramref name="list"/>.
        /// </summary>
        /// <param name="database">The<see cref="IDatabase"/>.</param>
        /// <param name="typeName">The SQL type name of the table-valued parameter.</param>
        /// <param name="list">The list.</param>
        /// <returns>The <see cref="TableValuedParameter"/>.</returns>
        public static TableValuedParameter CreateTableValuedParameter(this IDatabase database, string typeName, IEnumerable<long> list)
        {
            using var dt = new DataTable();
            dt.Columns.Add(database.DatabaseColumns.TvpListValueColumnName, typeof(long));

            if (list != null)
            {
                foreach (var item in list)
                {
                    dt.Rows.Add(item);
                }
            }

            return new TableValuedParameter(typeName, dt);
        }

        /// <summary>
        /// Creates a <see cref="DatabaseColumns.TvpGuidListTypeName"/> <see cref="TableValuedParameter"/> for the <see cref="Guid"/> <paramref name="list"/>.
        /// </summary>
        /// <param name="database">The<see cref="IDatabase"/>.</param>
        /// <param name="list">The list.</param>
        /// <returns>The <see cref="TableValuedParameter"/>.</returns>
        public static TableValuedParameter CreateTableValuedParameter(this IDatabase database, IEnumerable<Guid> list) => CreateTableValuedParameter(database, database.DatabaseColumns.TvpGuidListTypeName, list);

        /// <summary>
        /// Creates a <see cref="TableValuedParameter"/> for the <see cref="Guid"/> <paramref name="list"/>.
        /// </summary>
        /// <param name="database">The<see cref="IDatabase"/>.</param>
        /// <param name="typeName">The SQL type name of the table-valued parameter.</param>
        /// <param name="list">The list.</param>
        /// <returns>The <see cref="TableValuedParameter"/>.</returns>
        public static TableValuedParameter CreateTableValuedParameter(this IDatabase database, string typeName, IEnumerable<Guid> list)
        {
            using var dt = new DataTable();
            dt.Columns.Add(database.DatabaseColumns.TvpListValueColumnName, typeof(Guid));

            if (list != null)
            {
                foreach (var item in list)
                {
                    dt.Rows.Add(item);
                }
            }

            return new TableValuedParameter(typeName, dt);
        }

        /// <summary>
        /// Creates a <see cref="DatabaseColumns.TvpDateTimeListTypeName"/> <see cref="TableValuedParameter"/> for the <see cref="DateTime"/> <paramref name="list"/>.
        /// </summary>
        /// <param name="database">The<see cref="IDatabase"/>.</param>
        /// <param name="list">The list.</param>
        /// <returns>The <see cref="TableValuedParameter"/>.</returns>
        public static TableValuedParameter CreateTableValuedParameter(this IDatabase database, IEnumerable<DateTime> list) => CreateTableValuedParameter(database, database.DatabaseColumns.TvpGuidListTypeName, list);

        /// <summary>
        /// Creates a <see cref="TableValuedParameter"/> for the <see cref="DateTime"/> <paramref name="list"/>.
        /// </summary>
        /// <param name="database">The<see cref="IDatabase"/>.</param>
        /// <param name="typeName">The SQL type name of the table-valued parameter.</param>
        /// <param name="list">The list.</param>
        /// <returns>The <see cref="TableValuedParameter"/>.</returns>
        public static TableValuedParameter CreateTableValuedParameter(this IDatabase database, string typeName, IEnumerable<DateTime> list)
        {
            using var dt = new DataTable();
            dt.Columns.Add(database.DatabaseColumns.TvpListValueColumnName, typeof(DateTime));

            if (list != null)
            {
                foreach (var item in list)
                {
                    dt.Rows.Add(item);
                }
            }

            return new TableValuedParameter(typeName, dt);
        }

        #endregion
    }
}