namespace CoreEx.Data;

/// <summary>
/// Provides utility capabilities for models.
/// </summary>
public static class Model
{
    /// <summary>
    /// Prepares the model for <i>Create</i> by setting (overriding) the <see cref="ITenantId.TenantId"/>, <see cref="IChangeLogEx.CreatedBy"/>, and <see cref="IChangeLogEx.CreatedOn"/>.
    /// </summary>
    /// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
    /// <param name="model">The model.</param>
    /// <param name="executionContext">The optional <see cref="ExecutionContext"/>.</param>
    /// <returns>The <paramref name="model"/>.</returns>
    /// <remarks>Invokes the following:
    ///   <list type="bullet">
    ///     <item><see cref="PrepareTenantId"/>.</item>
    ///     <item><see cref="PrepareTypeDiscriminator"/>.</item>
    ///     <item><see cref="PrepareCreateChangeLog"/>.</item>
    ///   </list>
    /// </remarks>
    [return: NotNullIfNotNull(nameof(model))]
    public static TModel? PrepareCreate<TModel>(TModel? model, ExecutionContext? executionContext = null)
    {
        if (executionContext is null)
            ExecutionContext.TryGetCurrent(out executionContext);

        return PrepareCreateChangeLog(PrepareTypeDiscriminator(PrepareTenantId(model, executionContext)), executionContext);
    }

    /// <summary>
    /// Prepares the model for <i>Update</i> by setting (overriding) the <see cref="ITenantId.TenantId"/>, <see cref="IChangeLogEx.UpdatedBy"/>, and <see cref="IChangeLogEx.UpdatedOn"/>.
    /// </summary>
    /// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
    /// <param name="model">The model.</param>
    /// <param name="executionContext">The optional <see cref="ExecutionContext"/>.</param>
    /// <returns>The <paramref name="model"/>.</returns>
    /// <remarks>Invokes the following:
    ///   <list type="bullet">
    ///     <item><see cref="PrepareTenantId"/>.</item>
    ///     <item><see cref="PrepareTypeDiscriminator"/>.</item>
    ///     <item><see cref="PrepareUpdateChangeLog"/>.</item>
    ///   </list>
    /// </remarks>
    [return: NotNullIfNotNull(nameof(model))]
    public static TModel? PrepareUpdate<TModel>(TModel? model, ExecutionContext? executionContext = null)
    {
        if (executionContext is null)
            ExecutionContext.TryGetCurrent(out executionContext);

        return PrepareUpdateChangeLog(PrepareTypeDiscriminator(PrepareTenantId(model, executionContext)), executionContext);
    }

    /// <summary>
    /// Prepares the <see cref="ITenantId.TenantId"/> by setting (overriding) with the <see cref="ExecutionContext.Current"/> <see cref="ExecutionContext.TenantId"/>.
    /// </summary>
    /// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
    /// <param name="model">The model.</param>
    /// <param name="executionContext">The optional <see cref="ExecutionContext"/>.</param>
    /// <returns>The <paramref name="model"/>.</returns>
    [return: NotNullIfNotNull(nameof(model))]
    public static TModel? PrepareTenantId<TModel>(TModel? model, ExecutionContext? executionContext = null)
    {
        if (model is null || model is not ITenantId ti)
            return model;

        if (executionContext is null)
            ExecutionContext.TryGetCurrent(out executionContext);

        ti.TenantId = executionContext?.TenantId;
        return model;
    }

    /// <summary>
    /// Prepares the <see cref="ITypeDiscriminator.TypeDiscriminator"/> by setting (overriding) with the specified <paramref name="typeDiscriminator"/> or, where not specified, the <see cref="SchemaAttribute.Name"/>.
    /// </summary>
    /// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
    /// <param name="model">The model.</param>
    /// <param name="typeDiscriminator">The optional type discriminator override.</param>
    /// <returns>The <paramref name="model"/>.</returns>
    [return: NotNullIfNotNull(nameof(model))]
    public static TModel? PrepareTypeDiscriminator<TModel>(TModel? model, string? typeDiscriminator = null)
    {
        if (model is null || model is not ITypeDiscriminator td)
            return model;

        if (string.IsNullOrEmpty(typeDiscriminator) && Schema.TryGetMetadata<TModel>(out var metadata))
            typeDiscriminator = metadata.Name;

        td.TypeDiscriminator = typeDiscriminator;
        return model;
    }

    /// <summary>
    /// Prepares the <see cref="IChangeLog"/> or <see cref="IChangeLogEx"/> for <i>Create</i> by setting (overriding) the <see cref="IChangeLogEx.CreatedBy"/> and <see cref="IChangeLogEx.CreatedOn"/>.
    /// </summary>
    /// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
    /// <param name="model">The model.</param>
    /// <param name="executionContext">The optional <see cref="ExecutionContext"/>.</param>
    /// <returns>The <paramref name="model"/>.</returns>
    [return: NotNullIfNotNull(nameof(model))]
    public static TModel? PrepareCreateChangeLog<TModel>(TModel? model, ExecutionContext? executionContext = null)
    {
        if (model is null)
            return model;

        if (model is IChangeLog changeLog)
            changeLog.ChangeLog = ChangeLog.CreateCreated(executionContext);
        else if (model is IChangeLogEx changeLogEx)
        {
            var (UserName, Timestamp) = ChangeLog.GetChangeLogInfo(executionContext);
            changeLogEx.CreatedBy = UserName;
            changeLogEx.CreatedOn = Timestamp;
        }

        return model;
    }

    /// <summary>
    /// Prepares the <see cref="IChangeLog"/> or <see cref="IChangeLogEx"/> for <i>Update</i> by setting (overriding) the <see cref="IChangeLogEx.UpdatedBy"/> and <see cref="IChangeLogEx.UpdatedOn"/>.
    /// </summary>
    /// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
    /// <param name="model">The model.</param>
    /// <param name="executionContext">The optional <see cref="ExecutionContext"/>.</param>
    /// <returns>The <paramref name="model"/>.</returns>
    [return: NotNullIfNotNull(nameof(model))]
    public static TModel? PrepareUpdateChangeLog<TModel>(TModel? model, ExecutionContext? executionContext = null)
    {
        if (model is null)
            return model;

        if (model is IChangeLog changeLog)
            changeLog.ChangeLog = ChangeLog.CreateChanged(changeLog.ChangeLog, executionContext);
        else if (model is IChangeLogEx changeLogEx)
        {
            var (UserName, Timestamp) = ChangeLog.GetChangeLogInfo(executionContext);
            changeLogEx.UpdatedBy = UserName;
            changeLogEx.UpdatedOn = Timestamp;
        }

        return model;
    }
}