namespace CoreEx.Events;

public partial class EventData
{
    #region CreateEvent

    /// <summary>
    /// Creates a new event-oriented <see cref="EventData"/> instance with the specified <paramref name="entity"/> (subject) and <paramref name="action"/> (typically a verb describing an <i>event</i> in past tense).
    /// </summary>
    /// <param name="entity">The entity (subject) name.</param>
    /// <param name="action">The action.</param>
    /// <returns>The new <see cref="EventData"/>.</returns>
    public static EventData CreateEvent(string entity, string? action) => new EventData { Action = action }.WithEntity(entity);

    /// <summary>
    /// Creates a new event-oriented <see cref="EventData"/> instance with the specified <paramref name="entity"/> (subject) and the <paramref name="action"/> as the <see cref="Action"/> (typically a verb describing an <i>event</i> in past tense).
    /// </summary>
    /// <param name="entity">The entity (subject) name.</param>
    /// <param name="action">The <see cref="Enum"/> value that represents the action.</param>
    /// <returns>The new <see cref="EventData"/>.</returns>
    public static EventData CreateEvent(string entity, Enum action) => new EventData().WithEntity(entity).WithAction(action);

    /// <summary>
    /// Creates a new event-oriented <see cref="EventData"/> instance with the specified <typeparamref name="TEntity"/> (subject) and <paramref name="action"/> (typically a verb describing an <i>event</i> in past tense).
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="action">The action.</param>
    /// <returns>The new <see cref="EventData"/>.</returns>
    public static EventData CreateEvent<TEntity>(string? action) => new EventData { Action = action }.WithEntity<TEntity>();

    /// <summary>
    /// Creates a new event-oriented <see cref="EventData"/> instance with the specified <typeparamref name="TEntity"/> (subject) and the <paramref name="action"/> as the <see cref="Action"/> (typically a verb describing an <i>event</i> in past tense).
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="action">The <see cref="Enum"/> value that represents the action.</param>
    /// <returns>The new <see cref="EventData"/>.</returns>
    public static EventData CreateEvent<TEntity>(Enum action) => new EventData().WithEntity<TEntity>().WithAction(action);

    /// <summary>
    /// Creates a new event-oriented <see cref="EventData"/> instance <see cref="WithValue{T}(T, IEnumerable{string})">with</see> the specified <paramref name="value"/> and <paramref name="action"/> (typically a verb describing an <i>event</i> in past tense).
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="action">The action.</param>
    /// <returns>The new <see cref="EventData"/>.</returns>
    /// <remarks>The <see cref="Entity"/> (subject) automatically defaults from the value's <see cref="SchemaAttribute.Name"/> or <see cref="Type"/> name.</remarks>
    public static EventData CreateEventWith<T>(T? value, string? action)
    {
        var ed = new EventData().WithValue(value);
        return action is null ? ed : ed.WithAction(action);
    }

    /// <summary>
    /// Creates a new event-oriented <see cref="EventData"/> instance <see cref="WithValue{T}(T, IEnumerable{string})">with</see> the specified <paramref name="value"/> and the <paramref name="action"/> as the <see cref="Action"/> (typically a verb describing an <i>event</i> in past tense).
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="action">The <see cref="Enum"/> value that represents the action.</param>
    /// <returns>The new <see cref="EventData"/>.</returns>
    /// <remarks>The <see cref="Entity"/> (subject) automatically defaults from the value's <see cref="SchemaAttribute.Name"/> or <see cref="Type"/> name.</remarks>
    public static EventData CreateEventWith<T>(T? value, Enum action) => new EventData().WithValue(value).WithAction(action);

    /// <summary>
    /// Creates new event-oriented <see cref="EventData"/> instances <see cref="WithValue{T}(T, IEnumerable{string})">with</see> the specified <paramref name="values"/> and <paramref name="action"/> (typically a verb describing an <i>event</i> in past tense).
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="action">The action.</param>
    /// <param name="configure">An optional action to configure each <see cref="EventData"/> instance.</param>
    /// <returns>The new <see cref="EventData"/>.</returns>
    /// <remarks>The <see cref="Entity"/> (subject) automatically defaults from the value's <see cref="SchemaAttribute.Name"/> or <see cref="Type"/> name.</remarks>
    public static EventData[] CreateEventsWith<T>(IEnumerable<T?> values, string? action, Action<T, EventData>? configure = null)
    {
        var list = new List<EventData>();
        foreach (var value in values)
        {
            if (value is not null)
            {
                var ed = new EventData().WithValue(value);
                configure?.Invoke(value, ed);
                list.Add(action is null ? ed : ed.WithAction(action));
            }
        }

        return [.. list];
    }

    /// <summary>
    /// Creates new event-oriented <see cref="EventData"/> instances <see cref="WithValue{T}(T, IEnumerable{string})">with</see> the specified <paramref name="values"/> and the <paramref name="action"/> as the <see cref="Action"/> (typically a verb describing an <i>event</i> in past tense).
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="action">The <see cref="Enum"/> value that represents the action.</param>
    /// <param name="configure">An optional action to configure each <see cref="EventData"/> instance.</param>
    /// <returns>The new <see cref="EventData"/>.</returns>
    /// <remarks>The <see cref="Entity"/> (subject) automatically defaults from the value's <see cref="SchemaAttribute.Name"/> or <see cref="Type"/> name.</remarks>
    public static EventData[] CreateEventsWith<T>(IEnumerable<T?> values, Enum action, Action<T, EventData>? configure = null)
    {
        var list = new List<EventData>();
        foreach (var value in values)
        {
            if (value is not null)
            {
                var ed = new EventData().WithValue(value);
                configure?.Invoke(value, ed);
                list.Add(action is null ? ed : ed.WithAction(action));
            }
        }

        return [.. list];
    }

    #endregion

    #region CreateCommand

    /// <summary>
    /// Creates a new command-oriented <see cref="EventData"/> instance with the specified <paramref name="entity"/> (subject) and <paramref name="command"/> <see cref="Action"/> being requested.
    /// </summary>
    /// <param name="targetDomainName">The target domain name.</param>
    /// <param name="entity">The entity (subject) name.</param>
    /// <param name="command">The command action.</param>
    /// <returns>The new <see cref="EventData"/>.</returns>
    /// <remarks>The <paramref name="targetDomainName"/> represents the name of the domain that is the intended target of the command.</remarks>
    public static EventData CreateCommand(string targetDomainName, string entity, string command) => new EventData { MessageType = MessageType.Command }.WithDomain(targetDomainName).WithEntity(entity).WithAction(command);

    /// <summary>
    /// Creates a new command-oriented <see cref="EventData"/> instance with the specified <paramref name="entity"/> (subject) and the <paramref name="command"/> <see cref="Action"/> being requested.
    /// </summary>
    /// <param name="targetDomainName">The target domain name.</param>
    /// <param name="entity">The entity (subject) name.</param>
    /// <param name="command">The <see cref="Enum"/> value that represents the command action.</param>
    /// <returns>The new <see cref="EventData"/>.</returns>
    public static EventData CreateCommand(string targetDomainName, string entity, Enum command) => new EventData { MessageType = MessageType.Command }.WithDomain(targetDomainName).WithEntity(entity).WithAction(command);

    /// <summary>
    /// Creates a new command-oriented <see cref="EventData"/> instance with the specified <typeparamref name="TEntity"/> (subject) and <paramref name="command"/> <see cref="Action"/> being requested.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="targetDomainName">The target domain name.</param>
    /// <param name="command">The command action.</param>
    /// <returns>The new <see cref="EventData"/>.</returns>
    /// <remarks>The <paramref name="targetDomainName"/> represents the name of the domain that is the intended target of the command.</remarks>
    public static EventData CreateCommand<TEntity>(string targetDomainName, string command) => new EventData { MessageType = MessageType.Command }.WithDomain(targetDomainName).WithEntity<TEntity>().WithAction(command);

    /// <summary>
    /// Creates a new command-oriented <see cref="EventData"/> instance with the specified <typeparamref name="TEntity"/> (subject) and the <paramref name="command"/> <see cref="Action"/> being requested.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="targetDomainName">The target domain name.</param>
    /// <param name="command">The <see cref="Enum"/> value that represents the command action.</param>
    /// <returns>The new <see cref="EventData"/>.</returns>
    public static EventData CreateCommand<TEntity>(string targetDomainName, Enum command) => new EventData { MessageType = MessageType.Command }.WithDomain(targetDomainName).WithEntity<TEntity>().WithAction(command);

    /// <summary>
    /// Creates a new command-oriented <see cref="EventData"/> instance <see cref="WithValue{T}(T, IEnumerable{string})">with</see> the specified <paramref name="value"/> and <paramref name="command"/> <see cref="Action"/> being requested.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="targetDomainName">The target domain name.</param>
    /// <param name="value">The value.</param>
    /// <param name="command">The command action.</param>
    /// <returns>The new <see cref="EventData"/>.</returns>
    /// <remarks>The <see cref="Entity"/> (subject) automatically defaults from the value's <see cref="SchemaAttribute.Name"/> or <see cref="Type"/> name.</remarks>
    public static EventData CreateCommandWith<T>(string targetDomainName, T value, string command) => new EventData { MessageType = MessageType.Command }.WithDomain(targetDomainName).WithValue(value).WithAction(command);

    /// <summary>
    /// Creates a new command-oriented <see cref="EventData"/> instance <see cref="WithValue{T}(T, IEnumerable{string})">with</see> the specified <paramref name="value"/> and the <paramref name="command"/> <see cref="Action"/> being requested.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="targetDomainName">The target domain name.</param>
    /// <param name="value">The value.</param>
    /// <param name="command">The <see cref="Enum"/> value that represents the command action.</param>
    /// <returns>The new <see cref="EventData"/>.</returns>
    /// <remarks>The <see cref="Entity"/> (subject) automatically defaults from the value's <see cref="SchemaAttribute.Name"/> or <see cref="Type"/> name.</remarks>
    public static EventData CreateCommandWith<T>(string targetDomainName, T value, Enum command) => new EventData { MessageType = MessageType.Command }.WithDomain(targetDomainName).WithValue(value).WithAction(command);

    #endregion

    #region ToObjectFromJson

    /// <summary>
    /// Converts (deserializes) the JSON-based <see cref="Data"/> to type of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The resulting <see cref="Type"/>.</typeparam>
    /// <param name="jsonSerializerOptions">The optional <see cref="JsonSerializerOptions"/>.</param>
    /// <returns>The resulting JSON deserialized value.</returns>
    public T? ToObjectFromJson<T>(JsonSerializerOptions? jsonSerializerOptions = null)
    {
        if (Data is null || Data.IsEmpty)
            return default;

        return Data.ToObjectFromJson<T>(jsonSerializerOptions ?? JsonDefaults.SerializerOptions);
    }

    /// <summary>
    /// Converts (deserializes) the JSON-based <see cref="Data"/> to type of <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The resulting <see cref="Type"/>.</param>
    /// <param name="jsonSerializerOptions">The optional <see cref="JsonSerializerOptions"/>.</param>
    /// <returns>The resulting JSON deserialized value.</returns>
    public object? ToObjectFromJson(Type type, JsonSerializerOptions? jsonSerializerOptions = null)
    {
        type.ThrowIfNull();
        if (Data is null || Data.IsEmpty)
            return default;

        return JsonSerializer.Deserialize(Data, type, jsonSerializerOptions ?? JsonDefaults.SerializerOptions);
    }

    #endregion
}