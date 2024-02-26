// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace CoreEx.Database.SqlServer
{
    /// <summary>
    /// Represents a SQL-Server table-valued parameter (see <see href="https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/sql/table-valued-parameters"/>).
    /// </summary>
    /// <param name="typeName">The SQL type name of the table-valued parameter.</param>
    /// <param name="value">The <see cref="DataTable"/> value.</param>
    public class TableValuedParameter(string typeName, DataTable value)
    {
        /// <summary>
        /// Gets or sets the SQL type name of the table-valued parameter.
        /// </summary>
        public string TypeName { get; } = typeName.ThrowIfNull(nameof(typeName));

        /// <summary>
        /// Gets or sets the <see cref="DataTable"/> value.
        /// </summary>
        public DataTable Value { get; } = value.ThrowIfNull(nameof(value));

        /// <summary>
        /// Adds a new <see cref="DataRow"/> to the <see cref="Value"/> using the specified <paramref name="columnValues"/>.
        /// </summary>
        /// <param name="columnValues">The column values.</param>
        public void AddRow(params object?[] columnValues)
        {
            var r = Value.NewRow();
            for (int i = 0; i < columnValues.Length; i++)
            {
                r[i] = columnValues[i] ?? DBNull.Value;
            }

            Value.Rows.Add(r);
        }

        /// <summary>
        /// Adds a <see cref="DataRow"/> per each of the <paramref name="items"/> using the <paramref name="mapper"/> to get each of the column values. 
        /// </summary>
        /// <typeparam name="T">The item <see cref="Type"/>.</typeparam>
        /// <param name="database">The <see cref="IDatabase"/>.</param>
        /// <param name="mapper">The corresponding <see cref="IDatabaseMapper{TSource}"/>.</param>
        /// <param name="items">Zero or more items to add.</param>
        public void AddRows<T>(IDatabase database, IDatabaseMapper<T> mapper, IEnumerable<T>? items)
        {
            database.ThrowIfNull(nameof(database));
            mapper.ThrowIfNull(nameof(mapper));

            if (items == null)
                return;

            var dpc = new DatabaseParameterCollection(database);
            foreach (var item in items)
            {
                dpc.Clear();
                mapper.MapToDb(item, dpc);
                AddRow(dpc.Select(x => x.Value).ToArray());
            }
        }
    }
}