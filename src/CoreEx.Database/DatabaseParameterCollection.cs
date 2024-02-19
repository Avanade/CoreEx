// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace CoreEx.Database
{
    /// <summary>
    /// Provides a <see cref="DbParameter"/> collection used for a <see cref="DbCommand"/>.
    /// </summary>
    /// <param name="database">The <see cref="IDatabase"/>.</param>
    public sealed class DatabaseParameterCollection(IDatabase database) : ICollection<DbParameter>, IDatabaseParameters<DatabaseParameterCollection>
    {
        private readonly List<DbParameter> _parameters = [];

        /// <summary>
        /// Gets the underlying <see cref="IDatabase"/>.
        /// </summary>
        public IDatabase Database { get; } = database.ThrowIfNull(nameof(database));

        /// <inheritdoc/>
        DatabaseParameterCollection IDatabaseParameters<DatabaseParameterCollection>.Parameters => this;

        /// <inheritdoc/>
        public int Count => _parameters.Count;

        /// <inheritdoc/>
        bool ICollection<DbParameter>.IsReadOnly => false;

        /// <summary>
        /// Indicates whether a <see cref="DbParameter"/> with the specified <paramref name="name"/> exists in the collection.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <returns><c>true</c> indicates that the parameter exists in the collection; otherwise, <c>false</c>.</returns>
        public bool Contains(string name) => _parameters.Any(x => x.ParameterName == name);

        /// <summary>
        /// Adds the named parameter and value, using the specified <paramref name="direction"/>, to the <see cref="DbCommand.Parameters"/>.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="value">The parameter value.</param>
        /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
        /// <returns>A <see cref="DbParameter"/>.</returns>
        public DbParameter AddParameter(string name, object? value, ParameterDirection direction = ParameterDirection.Input)
        {
            var p = Database.Provider.CreateParameter() ?? throw new InvalidOperationException($"The {nameof(DbProviderFactory)}.{nameof(DbProviderFactory.CreateParameter)} returned a null.");
            p.ParameterName = ParameterizeName(name);
            p.Value = value ?? DBNull.Value;
            p.Direction = direction;

            _parameters.Add(p);
            return p;
        }

        /// <summary>
        /// Adds the named parameter and value, using the specified <paramref name="dbType"/> and <paramref name="direction"/>, to the <see cref="DbCommand.Parameters"/>.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="value">The parameter value.</param>
        /// <param name="dbType">The parameter <see cref="DbType"/>.</param>
        /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
        /// <returns>A <see cref="DbParameter"/>.</returns>
        public DbParameter AddParameter(string name, object? value, DbType dbType, ParameterDirection direction = ParameterDirection.Input)
        {
            var p = Database.Provider.CreateParameter() ?? throw new InvalidOperationException($"The {nameof(DbProviderFactory)}.{nameof(DbProviderFactory.CreateParameter)} returned a null.");
            p.ParameterName = ParameterizeName(name);
            p.DbType = dbType;
            p.Value = value ?? DBNull.Value;
            p.Direction = direction;

            _parameters.Add(p);
            return p;
        }

        /// <summary>
        /// Adds the named parameter and value, using the specified <paramref name="dbType"/>, <paramref name="size"/> and <paramref name="direction"/>, to the <see cref="DbCommand.Parameters"/>.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="dbType">The parameter <see cref="DbType"/>.</param>
        /// <param name="size">The maximum size (in bytes).</param>
        /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
        /// <returns>A <see cref="DbParameter"/>.</returns>
        public DbParameter AddParameter(string name, DbType dbType, int size, ParameterDirection direction = ParameterDirection.Input)
        {
            var p = Database.Provider.CreateParameter() ?? throw new InvalidOperationException($"The {nameof(DbProviderFactory)}.{nameof(DbProviderFactory.CreateParameter)} returned a null.");
            p.ParameterName = ParameterizeName(name);
            p.DbType = dbType;
            p.Size = size;
            p.Direction = direction;

            _parameters.Add(p);
            return p;
        }

        /// <summary>
        /// Adds an <see cref="int"/> <see cref="ParameterDirection.ReturnValue"/> parameter.
        /// </summary>
        /// <returns>A <see cref="DbParameter"/>.</returns>
        public DbParameter AddReturnValueParameter()
        {
            var p = Database.Provider.CreateParameter() ?? throw new InvalidOperationException($"The {nameof(DbProviderFactory)}.{nameof(DbProviderFactory.CreateParameter)} returned a null.");
            p.ParameterName = ParameterizeName(Database.DatabaseColumns.ReturnValueName);
            p.DbType = DbType.Int32;
            p.Direction = ParameterDirection.ReturnValue;

            _parameters.Add(p);
            return p;
        }

        /// <summary>
        /// Adds a named parameter (<see cref="Extended.DatabaseColumns.ReselectRecordName"/>) to <paramref name="reselect"/> the data.
        /// </summary>
        /// <param name="reselect">Indicates whether to reselect after the operation.</param>
        /// <returns>A <see cref="DbParameter"/>.</returns>
        public DbParameter AddReselectRecordParam(bool reselect = true) => AddParameter(Database.DatabaseColumns.ReselectRecordName, reselect);

        /// <summary>
        /// Parameterizes the name by ensuring it starts with an '@' character.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <returns>The parameterized name.</returns>
        public static string ParameterizeName(string name) => name.ThrowIfNull(nameof(name)).StartsWith('@') ? name : $"@{name}";

        /// <summary>
        /// Gets or sets the <see cref="DbParameter"/> at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based index.</param>
        /// <returns>The <see cref="DbParameter"/>.</returns>
        public DbParameter this[int index] => _parameters[index];

        /// <inheritdoc/>
        public void Add(DbParameter item) => _parameters.Add(item);

        /// <summary>
        /// Adds <see cref="DbParameter"/> <paramref name="list"/>.
        /// </summary>
        /// <param name="list">The <see cref="DbParameter"/> list to add.</param>
        public void AddRange(IEnumerable<DbParameter> list) => _parameters.AddRange(list);

        /// <inheritdoc/>
        public void Clear() => _parameters.Clear();

        /// <inheritdoc/>
        public bool Contains(DbParameter item) => _parameters.Contains(item);

        /// <inheritdoc/>
        public void CopyTo(DbParameter[] array, int arrayIndex) => _parameters.CopyTo(array, arrayIndex);

        /// <inheritdoc/>
        public bool Remove(DbParameter item) => _parameters.Remove(item);

        /// <inheritdoc/>
        public IEnumerator<DbParameter> GetEnumerator() => _parameters.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}