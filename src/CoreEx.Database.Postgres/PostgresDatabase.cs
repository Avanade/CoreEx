// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Mapping.Converters;
using CoreEx.Results;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.Common;

namespace CoreEx.Database.Postgres
{
    /// <summary>
    /// Provides <see href="https://www.npgsql.org/">Npgsql (PostgreSQL)</see> database access functionality.
    /// </summary>
    /// <param name="create">The function to create the <see cref="NpgsqlConnection"/>.</param>
    /// <param name="logger">The optional <see cref="ILogger"/>.</param>
    /// <param name="invoker">The optional <see cref="DatabaseInvoker"/>.</param>
    public class PostgresDatabase(Func<NpgsqlConnection> create, ILogger<PostgresDatabase>? logger = null, DatabaseInvoker? invoker = null) : Database<NpgsqlConnection>(create, NpgsqlFactory.Instance, logger, invoker)
    {
        private static readonly PostgresDatabaseColumns _defaultColumns = new();

        /// <summary>
        /// Gets or sets the names of the pre-configured <see cref="PostgresDatabaseColumns"/>.
        /// </summary>
        /// <remarks>Do not update the default properties directly as a shared static instance is used (unless this is the desired behaviour); create a new <see cref="PostgresDatabaseColumns"/> instance for overridding.</remarks>
        public new PostgresDatabaseColumns DatabaseColumns { get; set; } = _defaultColumns;

        /// <inheritdoc/>
        public override IConverter RowVersionConverter => EncodedStringToUInt32Converter.Default;

        /// <summary>
        /// Indicates whether to transform the <see cref="PostgresException"/> into an <see cref="Abstractions.IExtendedException"/> equivalent based on the <see cref="PostgresException.SqlState"/>.
        /// </summary>
        /// <remarks>Transforms and throws the <see cref="Abstractions.IExtendedException"/> equivalent from the <see cref="PostgresException"/> known list.</remarks>
        public bool ThrowTransformedException { get; set; } = true;

        /// <summary>
        /// Indicates whether to check the <see cref="SqlDuplicateErrorNumbers"/> when catching the <see cref="PostgresException"/>.
        /// </summary>
        public bool CheckSqlDuplicateErrorNumbers { get; set; } = true;

        /// <summary>
        /// Gets or sets the list of known <see cref="PostgresException.SqlState"/> values that are considered a duplicate error.
        /// </summary>
        /// <remarks>See <see href="https://dev.Npgsql.com/doc/Npgsql-errors/8.0/en/server-error-reference.html"/>.</remarks>
        public List<string> SqlDuplicateErrorNumbers { get; set; } = new List<string>(new string[] { "23505" });

        /// <inheritdoc/>
        protected override Result? OnDbException(DbException dbex)
        {
            if (ThrowTransformedException && dbex is PostgresException pex)
            {
                var msg = pex.Message?.TrimEnd();
                if (string.IsNullOrEmpty(msg))
                    msg = null;

                switch (pex.SqlState)
                {
                    case "56001": return Result.Fail(new ValidationException(msg, pex));
                    case "56002": return Result.Fail(new BusinessException(msg, pex));
                    case "56003": return Result.Fail(new AuthorizationException(msg, pex));
                    case "56004": return Result.Fail(new ConcurrencyException(msg, pex));
                    case "56005": return Result.Fail(new NotFoundException(msg, pex));
                    case "56006": return Result.Fail(new ConflictException(msg, pex));
                    case "56007": return Result.Fail(new DuplicateException(msg, pex));
                    case "56010": return Result.Fail(new DataConsistencyException(msg, pex));

                    default:
                        if (CheckSqlDuplicateErrorNumbers && SqlDuplicateErrorNumbers.Contains(pex.SqlState))
                            return Result.Fail(new DuplicateException(null, pex));

                        break;
                }
            }

            return base.OnDbException(dbex);
        }
    }
}