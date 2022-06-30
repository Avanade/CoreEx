// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities.Extended;
using System;
using System.Collections.Generic;

namespace CoreEx.Entities
{
    /// <summary>
    /// Represents a change log audit.
    /// </summary>
    public class ChangeLog : EntityBase<ChangeLog>
    {
        private DateTime? _createdDate;
        private string? _createdBy;
        private DateTime? _updatedDate;
        private string? _updatedBy;

        /// <summary>
        /// Gets or sets the created <see cref="DateTime"/>.
        /// </summary>
        public DateTime? CreatedDate { get => _createdDate; set => SetValue(ref _createdDate, value); }

        /// <summary>
        /// Gets or sets the created by (username).
        /// </summary>
        public string? CreatedBy { get => _createdBy; set => SetValue(ref _createdBy, value); }

        /// <summary>
        /// Gets or sets the updated <see cref="DateTime"/>.
        /// </summary>
        public DateTime? UpdatedDate { get => _updatedDate; set => SetValue(ref _updatedDate, value); }

        /// <summary>
        /// Gets or sets the updated by (username).
        /// </summary>
        public string? UpdatedBy { get => _updatedBy; set => SetValue(ref _updatedBy, value); }

        /// <inheritdoc/>
        protected override IEnumerable<IPropertyValue> GetPropertyValues()
        {
            yield return CreateProperty(CreatedDate, v => CreatedDate = v);
            yield return CreateProperty(CreatedBy, v => CreatedBy = v);
            yield return CreateProperty(UpdatedDate, v => UpdatedDate = v);
            yield return CreateProperty(UpdatedBy, v => UpdatedBy = v);
        }

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
            changeLog.UpdatedBy = GetUsername(executionContext);
            changeLog.UpdatedDate = GetTimestamp(executionContext);
            return changeLog;
        }

        /// <summary>
        /// Gets the username.
        /// </summary>
        private static string GetUsername(ExecutionContext? ec) => ec != null ? ec.Username : (ExecutionContext.HasCurrent ? ExecutionContext.Current.Username : ExecutionContext.EnvironmentUsername);

        /// <summary>
        /// Gets the timestamp.
        /// </summary>
        private static DateTime GetTimestamp(ExecutionContext? ec) => ec != null ? ec.Timestamp : (ExecutionContext.HasCurrent ? ExecutionContext.Current.Timestamp : DateTime.UtcNow);
    }
}