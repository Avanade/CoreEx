namespace CoreEx.Database;

public static partial class DatabaseExtensions
{
    /// <summary>
    /// Add one or more parameters by invoking a delegate.
    /// </summary>
    /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
    /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
    /// <param name="action">The delegate to enable parameter addition.</param>
    /// <returns>The <paramref name="parameters"/> to support fluent-style method-chaining.</returns>
    public static TSelf Params<TSelf>(this IDatabaseParameters<TSelf> parameters, Action<DatabaseParameterCollection> action)
    {
        action.ThrowIfNull()(parameters.ThrowIfNull().Parameters);
        return (TSelf)parameters;
    }

    /// <summary>
    /// Adds the <see cref="DbParameter"/> <paramref name="list"/>.
    /// </summary>
    /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
    /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
    /// <param name="list">The <see cref="DbParameter"/> list.</param>
    /// <returns>The <paramref name="parameters"/> to support fluent-style method-chaining.</returns>
    public static TSelf Params<TSelf>(this IDatabaseParameters<TSelf> parameters, params IEnumerable<DbParameter> list)
    {
        if (list is not null && list != parameters.Parameters)
            parameters.Parameters.AddRange(list);

        return (TSelf)parameters;
    }

    #region Param

    /// <summary>
    /// Adds the named parameter and value, using the specified <paramref name="direction"/>, to the <see cref="DbCommand.Parameters"/>.
    /// </summary>
    /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
    /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The parameter value.</param>
    /// <param name="dbType">The parameter <see cref="DbType"/>.</param>
    /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
    /// <returns>The <paramref name="parameters"/> to support fluent-style method-chaining.</returns>
    public static TSelf Param<TSelf>(this IDatabaseParameters<TSelf> parameters, string name, object? value = null, DbType? dbType = null, ParameterDirection direction = ParameterDirection.Input)
    {
        parameters.ThrowIfNull().Parameters.AddParameter(name, value, dbType, direction);
        return (TSelf)parameters;
    }

    /// <summary>
    /// Adds the named parameter and value, using the specified <paramref name="direction"/>, to the <see cref="DbCommand.Parameters"/>.
    /// </summary>
    /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
    /// <typeparam name="T">The parameter <see cref="Type"/>.</typeparam>
    /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The parameter value.</param>
    /// <param name="dbType">The parameter <see cref="DbType"/>.</param>
    /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
    /// <returns>The <paramref name="parameters"/> to support fluent-style method-chaining.</returns>
    public static TSelf Param<TSelf, T>(this IDatabaseParameters<TSelf> parameters, string name, T? value, DbType? dbType = null, ParameterDirection direction = ParameterDirection.Input)
    {
        parameters.ThrowIfNull().Parameters.AddParameter(name, value, dbType, direction);
        return (TSelf)parameters;
    }

    /// <summary>
    /// Adds the named parameter using the specified <paramref name="dbType"/>, <paramref name="size"/> and <paramref name="direction"/>, to the <see cref="DbCommand.Parameters"/>.
    /// </summary>
    /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
    /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="dbType">The parameter <see cref="DbType"/>.</param>
    /// <param name="size">The maximum size (in bytes).</param>
    /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
    /// <returns>The <paramref name="parameters"/> to support fluent-style method-chaining.</returns>
    public static TSelf Param<TSelf>(this IDatabaseParameters<TSelf> parameters, string name, DbType dbType, int size, ParameterDirection direction = ParameterDirection.Input)
    {
        parameters.ThrowIfNull().Parameters.AddParameter(name, dbType, size, direction);
        return (TSelf)parameters;
    }

    /// <summary>
    /// Adds the named parameter and value serialized as a JSON <see cref="string"/> to the <see cref="DbCommand.Parameters"/>.
    /// </summary>
    /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
    /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The parameter value.</param>
    /// <returns>The <paramref name="parameters"/> to support fluent-style method-chaining.</returns>
    public static TSelf JsonParam<TSelf>(this IDatabaseParameters<TSelf> parameters, string name, object? value)
    {
        parameters.ThrowIfNull().Parameters.AddJsonParameter(name, value);
        return (TSelf)parameters;
    }

    /// <summary>
    /// Adds the named <paramref name="wildcard"/> parameter to the <see cref="DbCommand.Parameters"/>.
    /// </summary>
    /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="wildcard">The wildcard value.</param>
    /// <returns>The <see cref="DatabaseParameterCollection"/> to support fluent-style method-chaining.</returns>
    public static TSelf WildCardParam<TSelf>(this IDatabaseParameters<TSelf> parameters, string name, string? wildcard)
    {
        parameters.ThrowIfNull().Parameters.AddWildcardParameter(name, wildcard);
        return (TSelf)parameters;
    }

    #endregion

    #region ParamWhen

    /// <summary>
    /// Adds a named parameter and value <paramref name="when"/> <see langword="true"/>.
    /// </summary>
    /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
    /// <typeparam name="T">The parameter <see cref="Type"/>.</typeparam>
    /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
    /// <param name="when">Adds the parameter when <see langword="true"/>.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The parameter value.</param>
    /// <param name="dbType">The parameter <see cref="DbType"/>.</param>
    /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
    /// <returns>The <paramref name="parameters"/> to support fluent-style method-chaining.</returns>
    public static TSelf ParamWhen<TSelf, T>(this IDatabaseParameters<TSelf> parameters, bool? when, string name, Func<T> value, DbType? dbType = null, ParameterDirection direction = ParameterDirection.Input)
    {
        value.ThrowIfNull();

        if (when is true)
            parameters.ThrowIfNull().Parameters.AddParameter(name, value(), dbType, direction);

        return (TSelf)parameters;
    }

    /// <summary>
    /// Adds a named parameter and value serialized as a JSON <see cref="string"/> <paramref name="when"/> <see langword="true"/>.
    /// </summary>
    /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
    /// <typeparam name="T">The parameter <see cref="Type"/>.</typeparam>
    /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
    /// <param name="when">Adds the parameter when <see langword="true"/>.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The parameter value.</param>
    /// <returns>The <paramref name="parameters"/> to support fluent-style method-chaining.</returns>
    public static TSelf JsonParamWhen<TSelf, T>(this IDatabaseParameters<TSelf> parameters, bool? when, string name, Func<T?> value)
    {
        value.ThrowIfNull();

        if (when is true)
            parameters.ThrowIfNull().Parameters.AddJsonParameter(name, value());

        return (TSelf)parameters;
    }

    /// <summary>
    /// Adds a named <paramref name="wildcard"/> parameter <paramref name="when"/> <see langword="true"/>.
    /// </summary>
    /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
    /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
    /// <param name="when">Adds the parameter when <see langword="true"/>.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="wildcard">The wildcard parameter value.</param>
    /// <returns>The <paramref name="parameters"/> to support fluent-style method-chaining.</returns>
    public static TSelf WildcardParamWhen<TSelf>(this IDatabaseParameters<TSelf> parameters, bool? when, string name, Func<string?> wildcard)
    {
        wildcard.ThrowIfNull();
        if (when is true)
            parameters.ThrowIfNull().Parameters.AddWildcardParameter(name, wildcard());

        return (TSelf)parameters;
    }

    #endregion

    #region ParamWith

    /// <summary>
    /// Adds a named parameter when invoked <paramref name="with"/> a non-default value.
    /// </summary>
    /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
    /// <typeparam name="T">The parameter <see cref="Type"/>.</typeparam>
    /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
    /// <param name="with">The value <b>with</b> which to verify is non-default that is also used as the parameter value.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="dbType">The parameter <see cref="DbType"/>.</param>
    /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
    /// <returns>The <paramref name="parameters"/> to support fluent-style method-chaining.</returns>
    public static TSelf ParamWith<TSelf, T>(this IDatabaseParameters<TSelf> parameters, T? with, string name, DbType? dbType = null, ParameterDirection direction = ParameterDirection.Input)
        => ParamWhen(parameters, with is not null && Comparer<T>.Default.Compare(with, default!) != 0, name, () => with!, dbType, direction);

    /// <summary>
    /// Adds a named parameter when invoked <paramref name="with"/> a non-default value.
    /// </summary>
    /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
    /// <typeparam name="TWith">The with <see cref="Type"/>.</typeparam>
    /// <typeparam name="TValue">The parameter <see cref="Type"/>.</typeparam>
    /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
    /// <param name="with">The value <b>with</b> which to verify is non-default.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The parameter value.</param>
    /// <param name="dbType">The parameter <see cref="DbType"/>.</param>
    /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
    /// <returns>The <paramref name="parameters"/> to support fluent-style method-chaining.</returns>
    public static TSelf ParamWith<TSelf, TWith, TValue>(this IDatabaseParameters<TSelf> parameters, TWith? with, string name, Func<TValue?> value, DbType? dbType = null, ParameterDirection direction = ParameterDirection.Input)
        => ParamWhen(parameters, with is not null && Comparer<TWith>.Default.Compare(with, default!) != 0, name, value, dbType, direction);

    /// <summary>
    /// Adds a named parameter when invoked <paramref name="with"/> a non-default value serialized as a JSON <see cref="string"/>.
    /// </summary>
    /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
    /// <typeparam name="T">The parameter value <see cref="Type"/>.</typeparam>
    /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
    /// <param name="with">The value <b>with</b> which to verify is non-default that is also used as the parameter value.</param>
    /// <param name="name">The parameter name.</param>
    /// <returns>The <paramref name="parameters"/> to support fluent-style method-chaining.</returns>
    public static TSelf JsonParamWith<TSelf, T>(this IDatabaseParameters<TSelf> parameters, T? with, string name)
        => JsonParamWhen(parameters, with is not null && Comparer<T>.Default.Compare(with, default!) != 0, name, () => with);

    /// <summary>
    /// Adds a named parameter when invoked <paramref name="with"/> a non-default value serialized as a JSON <see cref="string"/>.
    /// </summary>
    /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
    /// <typeparam name="TWith">The with value <see cref="Type"/>.</typeparam>
    /// <typeparam name="TValue">The parameter value <see cref="Type"/>.</typeparam>
    /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
    /// <param name="with">The value <b>with</b> which to verify is non-default.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The parameter value.</param>
    /// <returns>The <paramref name="parameters"/> to support fluent-style method-chaining.</returns>
    public static TSelf JsonParamWith<TSelf, TWith, TValue>(this IDatabaseParameters<TSelf> parameters, TWith? with, string name, Func<TValue?> value)
        => JsonParamWhen(parameters, with is not null && Comparer<TWith>.Default.Compare(with, default!) != 0, name, value);

    /// <summary>
    /// Adds a named parameter when invoked with a non-default <paramref name="wildcard"/> (converted for the database).
    /// </summary>
    /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
    /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
    /// <param name="wildcard">The wildcard <b>with</b> which to verify is non-default that is also used as the parameter value.</param>
    /// <param name="name">The parameter name.</param>
    /// <returns>The <paramref name="parameters"/> to support fluent-style method-chaining.</returns>
    public static TSelf WildcardParamWith<TSelf>(this IDatabaseParameters<TSelf> parameters, string? wildcard, string name)
        => WildcardParamWhen(parameters, !string.IsNullOrEmpty(wildcard), name, () => wildcard);

    /// <summary>
    /// Adds a named parameter when invoked with a non-default <paramref name="wildcard"/> (converted for the database).
    /// </summary>
    /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
    /// <typeparam name="TWith">The with value <see cref="Type"/>.</typeparam>
    /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
    /// <param name="with">The value <b>with</b> which to verify is non-default.</param>
    /// <param name="wildcard">The wildcard parameter value.</param>
    /// <param name="name">The parameter name.</param>
    /// <returns>The <paramref name="parameters"/> to support fluent-style method-chaining.</returns>
    public static TSelf WildcardParamWith<TSelf, TWith>(this IDatabaseParameters<TSelf> parameters, TWith? with, string name, Func<string?> wildcard)
        => WildcardParamWhen(parameters, with is not null && Comparer<TWith>.Default.Compare(with, default!) != 0, name, wildcard);

    #endregion

    #region RowVersionParam

    /// <summary>
    /// Adds a named (<see cref="DatabaseColumns.RowVersionName"/>) parameter using the <see cref="IReadOnlyETag.ETag"/> <see cref="IDatabase.RowVersionConverter"/>.
    /// </summary>
    /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
    /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
    /// <param name="value">The <see langword="string"/>-based <see cref="IReadOnlyETag.ETag"/> representation of the row version.</param>
    /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
    /// <returns>The <paramref name="parameters"/> to support fluent-style method-chaining.</returns>
    public static TSelf RowVersionParam<TSelf>(this IDatabaseParameters<TSelf> parameters, string? value, ParameterDirection direction = ParameterDirection.Input)
    {
        parameters.ThrowIfNull().Parameters.AddRowVersionParam(value, direction);
        return (TSelf)parameters;
    }

    /// <summary>
    /// Adds a named (<see cref="DatabaseColumns.RowVersionName"/>) parameter using the <see cref="IReadOnlyETag.ETag"/> <see cref="IDatabase.RowVersionConverter"/>.
    /// </summary>
    /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
    /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
    /// <param name="when">Adds the parameter when <see langword="true"/>.</param>
    /// <param name="value">The <see langword="string"/>-based <see cref="IReadOnlyETag.ETag"/> representation of the row version.</param>
    /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
    /// <returns>The <paramref name="parameters"/> to support fluent-style method-chaining.</returns>
    public static TSelf RowVersionParamWhen<TSelf>(this IDatabaseParameters<TSelf> parameters, bool? when, string? value, ParameterDirection direction = ParameterDirection.Input)
    {
        if (when is true)
            parameters.ThrowIfNull().Parameters.AddRowVersionParam(value, direction);

        return (TSelf)parameters;
    }

    /// <summary>
    /// Adds a named (<see cref="DatabaseColumns.RowVersionName"/>) parameter when invoked with a non-default value using the <see cref="IReadOnlyETag.ETag"/> <see cref="IDatabase.RowVersionConverter"/>.
    /// </summary>
    /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
    /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
    /// <param name="value">The <see langword="string"/>-based <see cref="IReadOnlyETag.ETag"/> representation of the row version which to verify is non-default that is also used as the parameter value.</param>
    /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
    /// <returns>The <paramref name="parameters"/> to support fluent-style method-chaining.</returns>
    public static TSelf RowVersionParamWith<TSelf>(this IDatabaseParameters<TSelf> parameters, string? value, ParameterDirection direction = ParameterDirection.Input)
    {
        if (!string.IsNullOrEmpty(value))
            parameters.ThrowIfNull().Parameters.AddRowVersionParam(value, direction);

        return (TSelf)parameters;
    }

    #endregion

    #region ReselectRecordParam

    /// <summary>
    /// Adds a named parameter (<see cref="Extended.DatabaseColumns.ReselectRecordName"/>) to <paramref name="reselect"/> the data.
    /// </summary>
    /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
    /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
    /// <param name="reselect">Indicates whether to reselect after the operation.</param>
    /// <returns>The <paramref name="parameters"/> to support fluent-style method-chaining.</returns>
    public static TSelf ReselectRecordParam<TSelf>(this IDatabaseParameters<TSelf> parameters, bool reselect = true)
    {
        parameters.ThrowIfNull().Parameters.AddReselectRecordParam(reselect);
        return (TSelf)parameters;
    }

    #endregion

    #region PagingParams

    /// <summary>
    /// Adds the <see cref="PagingArgs"/> as parameters.
    /// </summary>
    /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
    /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
    /// <param name="paging">The <see cref="PagingArgs"/>.</param>
    /// <returns>The <paramref name="parameters"/> to support fluent-style method-chaining.</returns>
    public static TSelf PagingParams<TSelf>(this IDatabaseParameters<TSelf> parameters, PagingArgs? paging)
    {
        if (paging is not null)
        {
            parameters.Param(parameters.Database.NamedColumns.PagingSkipName, paging.Skip);
            parameters.Param(parameters.Database.NamedColumns.PagingTakeName, paging.Take);
            parameters.ParamWhen(paging.IsCountRequested, parameters.Database.NamedColumns.PagingCountName, () => paging.IsCountRequested);
        }

        return (TSelf)parameters;
    }

    #endregion

    #region ReturnValue

    /// <summary>
    /// Adds a <see cref="ParameterDirection.ReturnValue"/> parameter to the <see cref="IDatabaseParameters{TSelf}"/>.
    /// </summary>
    /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
    /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
    /// <param name="returnValueParameter">The resulting <see cref="DbParameter"/>.</param>
    /// <param name="dbType">The parameter <see cref="DbType"/>; defaults to <see cref="DbType.Int32"/>.</param>
    /// <returns>The <paramref name="parameters"/> to support fluent-style method-chaining.</returns>
    public static TSelf ReturnValue<TSelf>(this IDatabaseParameters<TSelf> parameters, out DbParameter returnValueParameter, DbType dbType = DbType.Int32)
    {
        returnValueParameter = parameters.ThrowIfNull().Parameters.AddParameter(parameters.Database.NamedColumns.ReturnValueName, dbType, direction: ParameterDirection.ReturnValue);
        return (TSelf)parameters;
    }

    #endregion

    /// <summary>
    /// Sets the <see cref="DbParameter.Direction"/> to the <paramref name="direction"/> when the <paramref name="operationType"/> is as expressed by the <paramref name="when"/>.
    /// </summary>
    /// <param name="parameter">The <see cref="DbParameter"/>.</param>
    /// <param name="operationType">The single <see cref="OperationType"/> value being performed to enable conditional execution where appropriate.</param>
    /// <param name="when">The single or multi <see cref="OperationType"/> expression.</param>
    /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
    /// <returns>The <paramref name="parameter"/> to support fluent-style method-chaining.</returns>
    /// <remarks>When the <paramref name="operationType"/> is <i>not</i> as expressed by the <paramref name="when"/> then the existing <see cref="DbParameter.Direction"/> will remain unchanged.</remarks>
    public static DbParameter SetDirectionWhenOperationType(this DbParameter parameter, OperationType operationType, OperationType when, ParameterDirection direction = ParameterDirection.Output)
    {
        if (when.HasFlag(operationType))
            parameter.Direction = direction;

        return parameter;
    }
}