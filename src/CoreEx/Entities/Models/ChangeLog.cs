// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities.Extended;
using System;

namespace CoreEx.Entities.Models
{
    /// <summary>
    /// Represents a change log audit model (without inheriting <see cref="EntityBase"/>).
    /// </summary>
    public class ChangeLog
    {
        /// <summary>
        /// Gets or sets the created <see cref="DateTime"/>.
        /// </summary>
        public DateTime? CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the created by (username).
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the updated <see cref="DateTime"/>.
        /// </summary>
        public DateTime? UpdatedDate { get; set; }

        /// <summary>
        /// Gets or sets the updated by (username).
        /// </summary>
        public string? UpdatedBy { get; set; }

        /// <summary>
        /// Prepares the <see cref="ChangeLog"/> by setting the <c>Created</c> properties.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="executionContext">The optional <see cref="ExecutionContext"/>.</param>
        /// <remarks>Creates or updates the <see cref="ChangeLog"/> where <paramref name="value"/> implements <see cref="IChangeLog"/>.</remarks>
        public static void PrepareCreated<T>(T value, ExecutionContext? executionContext = null)
        {
            if (value != null && value is IChangeLog cl)
                cl.ChangeLog = PrepareCreated(cl.ChangeLog, executionContext);
        }

        /// <summary>
        /// Prepares the <paramref name="changeLog"/> by setting the <c>Created</c> properties.
        /// </summary>
        /// <param name="changeLog">The <see cref="ChangeLog"/>.</param>
        /// <param name="executionContext">The optional <see cref="ExecutionContext"/>.</param>
        /// <returns>A new or updated <see cref="ChangeLog"/> with <c>Created</c> properties set.</returns>
        public static ChangeLog PrepareCreated(ChangeLog? changeLog, ExecutionContext? executionContext = null)
        {
            changeLog ??= new ChangeLog();
            changeLog.CreatedBy = CoreEx.Entities.ChangeLog.GetUsername(executionContext);
            changeLog.CreatedDate = CoreEx.Entities.ChangeLog.GetTimestamp(executionContext);
            return changeLog;
        }

        /// <summary>
        /// Prepares the <see cref="ChangeLog"/> by setting the <c>Updated</c> properties.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="executionContext">The optional <see cref="ExecutionContext"/>.</param>
        /// <remarks>Creates or updates the <see cref="ChangeLog"/> where <paramref name="value"/> implements <see cref="IChangeLog"/>.</remarks>
        public static void PrepareUpdated<T>(T value, ExecutionContext? executionContext = null)
        {
            if (value != null && value is IChangeLog cl)
                cl.ChangeLog = PrepareUpdated(cl.ChangeLog, executionContext);
        }

        /// <summary>
        /// Prepares the <paramref name="changeLog"/> by setting the <c>Updated</c> properties.
        /// </summary>
        /// <param name="changeLog">The <see cref="ChangeLog"/>.</param>
        /// <param name="executionContext">The optional <see cref="ExecutionContext"/>.</param>
        /// <returns>A new or updated <see cref="ChangeLog"/> with <c>Updated</c> properties set.</returns>
        public static ChangeLog PrepareUpdated(ChangeLog? changeLog, ExecutionContext? executionContext = null)
        {
            changeLog ??= new ChangeLog();
            changeLog.UpdatedBy = CoreEx.Entities.ChangeLog.GetUsername(executionContext);
            changeLog.UpdatedDate = CoreEx.Entities.ChangeLog.GetTimestamp(executionContext);
            return changeLog;
        }
    }
}