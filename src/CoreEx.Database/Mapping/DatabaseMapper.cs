namespace CoreEx.Database.Mapping;

/// <summary>
/// Provides utility capabilities for database mapping.
/// </summary>
public static class DatabaseMapper
{
    /// <summary>
    /// Maps the database record values to the standard properties of the specified item.
    /// </summary>
    /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
    /// <param name="item">The item to map into.</param>
    /// <param name="record">The <see cref="DatabaseRecord"/>.</param>
    /// <param name="operationType">The <see cref="OperationType"/> value being performed to enable conditional execution where appropriate.</param>
    /// <param name="strict">Indicates whether the underlying <paramref name="record"/> column value is strictly required or not.</param>
    /// <returns>The <paramref name="item"/> to support fluent-style method-chaining.</returns>
    /// <remarks>Standard properties are mapped based on whether the <typeparamref name="TItem"/> implements the following respectively:
    ///   <list type="bullet">
    ///     <item><see cref="DatabaseColumns.RowVersionName"/> -> <see cref="IReadOnlyETag"/></item>
    ///     <item><see cref="DatabaseColumns.TenantIdName"/> -> <see cref="IReadOnlyTenantId"/></item>
    ///     <item><see cref="DatabaseColumns.IsDeletedName"/> -> <see cref="IReadOnlyLogicallyDeleted"/></item>
    ///     <item>* -> <see cref="IReadOnlyChangeLog"/></item>
    ///     <item>* -> <see cref="IReadOnlyChangeLogEx"/></item>
    ///   </list>
    /// </remarks>    
    public static TItem MapStandardFromDb<TItem>(this TItem item, DatabaseRecord record, OperationType operationType = OperationType.Unspecified, bool strict = true)
    {
        MapStandardFromDb(record, item.ThrowIfNull(), operationType, strict);
        return item;
    }

    /// <summary>
    /// Maps the database record values to the standard properties of the specified item.
    /// </summary>
    /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
    /// <param name="record">The <see cref="DatabaseRecord"/>.</param>
    /// <param name="item">The item to map into.</param>
    /// <param name="operationType">The <see cref="OperationType"/> value being performed to enable conditional execution where appropriate.</param>
    /// <param name="strict">Indicates whether the underlying <paramref name="record"/> column value is strictly required or not.</param>
    /// <remarks>Standard properties are mapped based on whether the <typeparamref name="TItem"/> implements the following respectively:
    ///   <list type="bullet">
    ///     <item><see cref="DatabaseColumns.RowVersionName"/> -> <see cref="IReadOnlyETag"/></item>
    ///     <item><see cref="DatabaseColumns.TenantIdName"/> -> <see cref="IReadOnlyTenantId"/></item>
    ///     <item><see cref="DatabaseColumns.IsDeletedName"/> -> <see cref="IReadOnlyLogicallyDeleted"/></item>
    ///     <item>* -> <see cref="IReadOnlyChangeLog"/></item>
    ///     <item>* -> <see cref="IReadOnlyChangeLogEx"/></item>
    ///   </list>
    /// </remarks>    
    public static void MapStandardFromDb<TItem>(DatabaseRecord record, TItem? item, OperationType operationType = OperationType.Unspecified, bool strict = true)
    {
        if (record is null || item is null)
            return;

        if (item is IETag et)
            et.ETag = strict || record.TryGetOrdinal(record.Database.NamedColumns.RowVersionName, out _) ? record.GetRowVersion() : default;

        if (item is ITenantId ti)
            ti.TenantId = strict ? record.GetValue<string?>(record.Database.NamedColumns.TenantIdName) : record.GetValueOrDefault<string?>(record.Database.NamedColumns.TenantIdName);

        if (item is ILogicallyDeleted ld)
            ld.IsDeleted = (strict ? record.GetValue<bool?>(record.Database.NamedColumns.IsDeletedName) : record.GetValueOrDefault<bool?>(record.Database.NamedColumns.IsDeletedName)) ?? false;

        MapChangeLogFromDb(record, item, operationType, strict);
    }

    /// <summary>
    /// Maps the database record values to the standard change log properties of the specified item.
    /// </summary>
    /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
    /// <param name="record">The <see cref="DatabaseRecord"/>.</param>
    /// <param name="item">The item to map into.</param>
    /// <param name="operationType">The <see cref="OperationType"/> value being performed to enable conditional execution where appropriate.</param>
    /// <param name="strict">Indicates whether the underlying <paramref name="record"/> column value is strictly required or not.</param>
    /// <returns>The <paramref name="item"/> to support fluent-style method-chaining.</returns>
    public static TItem MapChangeLogFromDb<TItem>(this TItem item, DatabaseRecord record, OperationType operationType = OperationType.Unspecified, bool strict = true)
    {
        MapChangeLogFromDb(record, item.ThrowIfNull(), operationType, strict);
        return item;
    }

    /// <summary>
    /// Maps the database record values to the standard change log properties of the specified item.
    /// </summary>
    /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
    /// <param name="record">The <see cref="DatabaseRecord"/>.</param>
    /// <param name="item">The item to map into.</param>
    /// <param name="operationType">The <see cref="OperationType"/> value being performed to enable conditional execution where appropriate.</param>
    /// <param name="strict">Indicates whether the underlying <paramref name="record"/> column value is strictly required or not.</param>
    public static void MapChangeLogFromDb<TItem>(DatabaseRecord record, TItem? item, OperationType operationType = OperationType.Unspecified, bool strict = true)
    {
        if (record is null || item is null)
            return;

        if (item is IChangeLog cl)
        {
            if (cl.ChangeLog is null)
            {
                // Only update where there is a legit value.
                var changeLog = new ChangeLog();
                MapChangeLogFromDb(record, changeLog, operationType, strict);
                if (!changeLog.IsDefault())
                    cl.ChangeLog = changeLog;
            }
            else
                MapChangeLogFromDb(record, cl.ChangeLog, operationType, strict);
        }

        if (item is not IChangeLogEx cle)
            return;

        cle.CreatedBy = strict ? record.GetValue<string?>(record.Database.NamedColumns.CreatedByName) : record.GetValueOrDefault<string?>(record.Database.NamedColumns.CreatedByName);
        cle.CreatedOn = strict ? record.GetValue<DateTimeOffset?>(record.Database.NamedColumns.CreatedOnName) : record.GetValueOrDefault<DateTimeOffset?>(record.Database.NamedColumns.CreatedOnName);
        cle.UpdatedBy = strict ? record.GetValue<string?>(record.Database.NamedColumns.UpdatedByName) : record.GetValueOrDefault<string?>(record.Database.NamedColumns.UpdatedByName);
        cle.UpdatedOn = strict ? record.GetValue<DateTimeOffset?>(record.Database.NamedColumns.UpdatedOnName) : record.GetValueOrDefault<DateTimeOffset?>(record.Database.NamedColumns.UpdatedOnName);
    }

    /// <summary>
    /// Maps the standard properties of the specified item to the database parameters.
    /// </summary>
    /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
    /// <param name="item">The item value.</param>
    /// <param name="parameters">The <see cref="DatabaseParameterCollection"/> to update from the <paramref name="item"/>.</param>
    /// <param name="operationType">The <see cref="OperationType"/> value being performed to enable conditional execution where appropriate.</param>
    /// <remarks>Standard properties are mapped based on whether the <typeparamref name="TItem"/> implements the following respectively:
    ///   <list type="bullet">
    ///     <item><see cref="IReadOnlyETag"/> -> <see cref="DatabaseColumns.RowVersionName"/></item>
    ///     <item><see cref="IReadOnlyTenantId"/> -> <see cref="DatabaseColumns.TenantIdName"/></item>
    ///     <item><see cref="IReadOnlyLogicallyDeleted"/> -> <see cref="DatabaseColumns.IsDeletedName"/></item>
    ///     <item><see cref="IReadOnlyChangeLog"/> -> *</item>
    ///     <item><see cref="IReadOnlyChangeLogEx"/> -> *</item>
    ///   </list>
    /// </remarks>
    public static void MapStandardToDb<TItem>(TItem item, DatabaseParameterCollection parameters, OperationType operationType = OperationType.Unspecified)
    {
        if (item is null)
            return;

        if (item is IReadOnlyETag et)
            parameters.AddParameter(parameters.Database.NamedColumns.RowVersionName, et.ETag);

        if (item is IReadOnlyTenantId ti)
            parameters.AddParameter(parameters.Database.NamedColumns.TenantIdName, ti.TenantId);

        if (item is IReadOnlyLogicallyDeleted ld)
            parameters.AddParameter(parameters.Database.NamedColumns.IsDeletedName, ld.IsDeleted);

        MapChangeLogToDb(item, parameters, operationType);
    }

    /// <summary>
    /// Maps the change log properties of the specified item to the database parameters.
    /// </summary>
    /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
    /// <param name="item">The item value.</param>
    /// <param name="parameters">The <see cref="DatabaseParameterCollection"/> to update from the <paramref name="item"/>.</param>
    /// <param name="operationType">The <see cref="OperationType"/> value being performed to enable conditional execution where appropriate.</param>
    /// <remarks>This maps both <see cref="IChangeLog"/> and <see cref="IChangeLogEx"/> using the corresponding the <see cref="IDatabase.NamedColumns"/>.</remarks>
    public static void MapChangeLogToDb<TItem>(TItem item, DatabaseParameterCollection parameters, OperationType operationType = OperationType.Unspecified)
    {
        if (item is null)
            return;

        if (item is IChangeLog cl && cl.ChangeLog is not null)
        {
            MapChangeLogToDb(cl.ChangeLog, parameters, operationType);
            return;
        }

        if (item is IChangeLogEx cle)
        {
            if (operationType == OperationType.Unspecified || operationType == OperationType.Create)
            {
                parameters.AddParameter(parameters.Database.NamedColumns.CreatedByName, cle.CreatedBy);
                parameters.AddParameter(parameters.Database.NamedColumns.CreatedOnName, cle.CreatedOn);
            }

            if (operationType == OperationType.Unspecified || operationType == OperationType.Update)
            {
                parameters.AddParameter(parameters.Database.NamedColumns.UpdatedByName, cle.UpdatedBy);
                parameters.AddParameter(parameters.Database.NamedColumns.UpdatedOnName, cle.UpdatedOn);
            }
        }
    }
}