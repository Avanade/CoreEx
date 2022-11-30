// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Entities
{
    /// <summary>
    /// Provides a <see cref="IChangeLogAudit"/>.
    /// </summary>
    public class ChangeLog : IChangeLogAudit
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
            if (value != null && value is IChangeLogAuditLog cl)
                cl.ChangeLogAudit = PrepareCreated(cl.ChangeLogAudit ?? new ChangeLog(), executionContext);
        }

        /// <summary>
        /// Prepares the <paramref name="changeLog"/> by setting the <c>Created</c> properties.
        /// </summary>
        /// <param name="changeLog">The <see cref="IChangeLogAudit"/>.</param>
        /// <param name="executionContext">The optional <see cref="ExecutionContext"/>.</param>
        /// <returns>A new or updated <see cref="ChangeLog"/> with <c>Created</c> properties set.</returns>
        public static IChangeLogAudit PrepareCreated(IChangeLogAudit changeLog, ExecutionContext? executionContext = null)
        {
            if (changeLog is null)
                throw new ArgumentNullException(nameof(changeLog));

            changeLog.CreatedBy = GetUsername(executionContext);
            changeLog.CreatedDate = GetTimestamp(executionContext);
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
            if (value != null && value is IChangeLogAuditLog cl)
                cl.ChangeLogAudit = PrepareUpdated(cl.ChangeLogAudit ?? new ChangeLog(), executionContext);
        }

        /// <summary>
        /// Prepares the <paramref name="changeLog"/> by setting the <c>Updated</c> properties.
        /// </summary>
        /// <param name="changeLog">The <see cref="ChangeLog"/>.</param>
        /// <param name="executionContext">The optional <see cref="ExecutionContext"/>.</param>
        /// <returns>A new or updated <see cref="ChangeLog"/> with <c>Updated</c> properties set.</returns>
        public static IChangeLogAudit PrepareUpdated(IChangeLogAudit changeLog, ExecutionContext? executionContext = null)
        {
            if (changeLog is null)
                throw new ArgumentNullException(nameof(changeLog));

            changeLog.UpdatedBy = GetUsername(executionContext);
            changeLog.UpdatedDate = GetTimestamp(executionContext);
            return changeLog;
        }

        /// <summary>
        /// Gets the username.
        /// </summary>
        private static string GetUsername(ExecutionContext? ec) => ec != null ? ec.UserName : (ExecutionContext.HasCurrent ? ExecutionContext.Current.UserName : ExecutionContext.EnvironmentUserName);

        /// <summary>
        /// Gets the timestamp.
        /// </summary>
        private static DateTime GetTimestamp(ExecutionContext? ec) => ec != null ? ec.Timestamp : (ExecutionContext.HasCurrent ? ExecutionContext.Current.Timestamp : DateTime.UtcNow);
    }
}