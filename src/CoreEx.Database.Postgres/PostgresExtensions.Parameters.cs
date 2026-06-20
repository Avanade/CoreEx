namespace CoreEx.Database.Postgres;

public static partial class PostgresExtensions
{
    /// <summary>
    /// Adds the named parameter and value, using the specified <see cref="NpgsqlDbType"/> and <see cref="ParameterDirection"/>, to the <see cref="DbCommand.Parameters"/>.
    /// </summary>
    /// <param name="parameters">The <see cref="DatabaseParameterCollection"/>.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The parameter value.</param>
    /// <param name="npgsqlDbType">The parameter <see cref="NpgsqlDbType"/>.</param>
    /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
    /// <returns>A <see cref="DbParameter"/>.</returns>
    public static NpgsqlParameter AddParameter<T>(this DatabaseParameterCollection parameters, string name, T? value, NpgsqlDbType? npgsqlDbType = null, ParameterDirection direction = ParameterDirection.Input)
    {
        var p = (NpgsqlParameter)parameters.ThrowIfNull().Database.CreateParameter();
        p.ParameterName = DatabaseParameterCollection.ParameterizeName(name);
        if (npgsqlDbType.HasValue)
            p.NpgsqlDbType = npgsqlDbType.Value;

        p.Value = DatabaseParameterCollection.ConvertToDbValue(value, parameters.Database);
        p.Direction = direction;

        parameters.Add(p);
        return p;
    }

    /// <summary>
    /// Adds the named parameter and value, using the specified <see cref="NpgsqlDbType"/> and <see cref="ParameterDirection"/>.
    /// </summary>
    /// <param name="parameters">The <see cref="DatabaseParameterCollection"/>.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The parameter value.</param>
    /// <param name="npgsqlDbType">The parameter <see cref="NpgsqlDbType"/>.</param>
    /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
    /// <returns></returns>
    public static TSelf Param<TSelf, T>(this IDatabaseParameters<TSelf> parameters, string name, T? value, NpgsqlDbType? npgsqlDbType = null, ParameterDirection direction = ParameterDirection.Input)
    {
        parameters.ThrowIfNull().Parameters.AddParameter(name, value, npgsqlDbType, direction);
        return (TSelf)parameters;
    }

    /// <summary>
    /// Adds a named parameter and value <paramref name="when"/> <see langword="true"/>.
    /// </summary>
    /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
    /// <typeparam name="T">The parameter <see cref="Type"/>.</typeparam>
    /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
    /// <param name="when">Adds the parameter when <see langword="true"/>.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The parameter value.</param>
    /// <param name="npgsqlDbType">The parameter <see cref="NpgsqlDbType"/>.</param>
    /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
    /// <returns>The <see cref="DatabaseParameterCollection"/> to support fluent-style method-chaining.</returns>
    public static TSelf ParamWhen<TSelf, T>(this IDatabaseParameters<TSelf> parameters, bool? when, string name, Func<T> value, NpgsqlDbType npgsqlDbType, ParameterDirection direction = ParameterDirection.Input)
    {
        parameters.ThrowIfNull();
        value.ThrowIfNull();

        if (when == true)
            parameters.Parameters.AddParameter(name, value(), npgsqlDbType, direction);

        return (TSelf)parameters;
    }

    /// <summary>
    /// Adds a named parameter when invoked <paramref name="with"/> a non-default value.
    /// </summary>
    /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
    /// <typeparam name="T">The parameter <see cref="Type"/>.</typeparam>
    /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
    /// <param name="with">The value <b>with</b> which to verify is non-default.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The parameter value.</param>
    /// <param name="npgsqlDbType">The parameter <see cref="NpgsqlDbType"/>.</param>
    /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
    /// <returns>The current <see cref="DatabaseParameterCollection"/> instance to support chaining (fluent interface).</returns>
    public static TSelf ParamWith<TSelf, T>(this IDatabaseParameters<TSelf> parameters, object? with, string name, Func<T> value, NpgsqlDbType npgsqlDbType, ParameterDirection direction = ParameterDirection.Input)
        => ParamWhen(parameters, with is not null && Comparer<T>.Default.Compare((T)with, default!) != 0, name, value, npgsqlDbType, direction);

    /// <summary>
    /// Adds a named parameter when invoked <paramref name="with"/> a non-default value.
    /// </summary>
    /// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
    /// <typeparam name="T">The parameter <see cref="Type"/>.</typeparam>
    /// <param name="parameters">The <see cref="IDatabaseParameters{TSelf}"/>.</param>
    /// <param name="with">The value <b>with</b> which to verify is non-default.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The parameter value; where not specified the <paramref name="with"/> value will be used.</param>
    /// <param name="npgsqlDbType">The parameter <see cref="NpgsqlDbType"/>.</param>
    /// <param name="direction">The <see cref="ParameterDirection"/> (default to <see cref="ParameterDirection.Input"/>).</param>
    /// <returns>The current <see cref="DatabaseParameterCollection"/> instance to support chaining (fluent interface).</returns>
    public static TSelf ParamWith<TSelf, T>(this IDatabaseParameters<TSelf> parameters, T? with, string name, Func<T>? value, NpgsqlDbType npgsqlDbType, ParameterDirection direction = ParameterDirection.Input)
        => ParamWhen(parameters, with is not null && Comparer<T>.Default.Compare(with, default!) != 0, name, value ?? (() => with!), npgsqlDbType, direction);
}