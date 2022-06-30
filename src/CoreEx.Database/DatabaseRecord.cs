// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;
using System.Data.Common;

namespace CoreEx.Database
{
    /// <summary>
    /// Encapsulates the <see cref="DbDataReader"/> to provide requisite column value capabilities.
    /// </summary>
    public class DatabaseRecord
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseRecord"/> class.
        /// </summary>
        /// <param name="database">The owning <see cref="IDatabase"/>.</param>
        /// <param name="dataReader">The underlying <see cref="DbDataReader"/>.</param>
        public DatabaseRecord(IDatabase database, DbDataReader dataReader)
        {
            Database = database ?? throw new ArgumentNullException(nameof(database));
            DataReader = dataReader ?? throw new ArgumentNullException(nameof(dataReader));
        }

        /// <summary>
        /// Gets the underlying <see cref="IDatabase"/>.
        /// </summary>
        public IDatabase Database { get; }

        /// <summary>
        /// Gets the underlying <see cref="DbDataReader"/>.
        /// </summary>
        public DbDataReader DataReader { get; }

        /// <summary>
        /// Gets the named column value.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="columnName">The column name.</param>
        /// <returns>The value.</returns>
        public T GetValue<T>(string columnName) => GetValue<T>(DataReader.GetOrdinal(columnName ?? throw new ArgumentNullException(nameof(columnName))));

        /// <summary>
        /// Gets the specified column value.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="ordinal">The ordinal index.</param>
        /// <returns>The value.</returns>
        public T GetValue<T>(int ordinal)
        {
            if (DataReader.IsDBNull(ordinal))
                return default!;

            T val = DataReader.GetFieldValue<T>(ordinal);
            return val is DateTime dt ? (T)Convert.ChangeType(Cleaner.Clean(dt, Database.DateTimeTransform), typeof(DateTime), System.Globalization.CultureInfo.InvariantCulture) : val;
        }

        /// <summary>
        /// Indicates whether the named column is <see cref="DBNull"/>.
        /// </summary>
        /// <param name="columnName">The column name.</param>
        /// <param name="ordinal">The corresponding ordinal for the column name.</param>
        /// <returns><c>true</c> indicates that the column value has a <see cref="DBNull"/> value; otherwise, <c>false</c>.</returns>
        public bool IsDBNull(string columnName, out int ordinal)
        {
            ordinal = DataReader.GetOrdinal(columnName ?? throw new ArgumentNullException(nameof(columnName)));
            return DataReader.IsDBNull(ordinal);
        }

        /// <summary>
        /// Gets the named <c>RowVersion</c> column as a <see cref="string"/>.
        /// </summary>
        /// <param name="columnName">The name of the column.</param>
        /// <returns>The resultant value.</returns>
        /// <remarks>The <b>RowVersion</b> <see cref="byte"/> array will be converted to an <see cref="Convert.ToBase64String(byte[])">encoded</see> <see cref="string"/> value.</remarks>
        public string GetRowVersion(string columnName)
        {
            var i = DataReader.GetOrdinal(columnName ?? throw new ArgumentNullException(nameof(columnName)));
            return Convert.ToBase64String(DataReader.GetFieldValue<byte[]>(i));
        }
    }
}