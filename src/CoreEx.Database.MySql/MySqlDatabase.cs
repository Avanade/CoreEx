// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Mapping.Converters;
using CoreEx.Results;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;

namespace CoreEx.Database.MySql
{
    /// <summary>
    /// Provides <see href="https://dev.mysql.com/">MySQL</see> database access functionality.
    /// </summary>
    public class MySqlDatabase : Database<MySqlConnection>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MySqlConnection"/> class.
        /// </summary>
        /// <param name="create">The function to create the <see cref="MySqlConnection"/>.</param>
        /// <param name="logger">The optional <see cref="ILogger"/>.</param>
        /// <param name="invoker">The optional <see cref="DatabaseInvoker"/>.</param>
        public MySqlDatabase(Func<MySqlConnection> create, ILogger<MySqlDatabase>? logger = null, DatabaseInvoker? invoker = null)
            : base(create, MySqlClientFactory.Instance, logger, invoker) { }

        /// <inheritdoc/>
        public override IConverter RowVersionConverter => EncodedStringToDateTimeConverter.Default;

        /// <summary>
        /// Indicates whether to transform the <see cref="MySqlException"/> into an <see cref="Abstractions.IExtendedException"/> equivalent based on the <see cref="MySqlException.Number"/>.
        /// </summary>
        /// <remarks>Transforms and throws the <see cref="Abstractions.IExtendedException"/> equivalent from the <see cref="MySqlException"/> known list.</remarks>
        public bool ThrowTransformedException { get; set; } = true;

        /// <summary>
        /// Indicates whether to check the <see cref="SqlDuplicateErrorNumbers"/> when catching the <see cref="MySqlException"/>.
        /// </summary>
        public bool CheckSqlDuplicateErrorNumbers { get; set; } = true;

        /// <summary>
        /// Gets or sets the list of known <see cref="MySqlException.Number"/> values that are considered a duplicate error.
        /// </summary>
        /// <remarks>See <see href="https://dev.mysql.com/doc/mysql-errors/8.0/en/server-error-reference.html"/>.</remarks>
        public List<int> SqlDuplicateErrorNumbers { get; set; } = new List<int>(new int[] { 1022, 1062, 1088, 1291, 1586, 1859 });

        /// <inheritdoc/>
        protected override Result? OnDbException(DbException dbex)
        {
            if (!ThrowTransformedException)
                return base.OnDbException(dbex);

            if (dbex is MySqlException sex)
            {
                var msg = sex.Message?.TrimEnd();
                if (string.IsNullOrEmpty(msg))
                    msg = null;

                switch (sex.Number)
                {
                    case 56001: return Result.Fail(new ValidationException(msg, sex));
                    case 56002: return Result.Fail(new BusinessException(msg, sex));
                    case 56003: return Result.Fail(new AuthorizationException(msg, sex));
                    case 56004: return Result.Fail(new ConcurrencyException(msg, sex));
                    case 56005: return Result.Fail(new NotFoundException(msg, sex));
                    case 56006: return Result.Fail(new ConflictException(msg, sex));
                    case 56007: return Result.Fail(new DuplicateException(msg, sex));

                    default:
                        if (CheckSqlDuplicateErrorNumbers && SqlDuplicateErrorNumbers.Contains(sex.Number))
                            return Result.Fail(new DuplicateException(null, sex));

                        break;
                }
            }

            return base.OnDbException(dbex);
        }
    }
}