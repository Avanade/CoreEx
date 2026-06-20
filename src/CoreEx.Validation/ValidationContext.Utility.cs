namespace CoreEx.Validation;

public sealed partial class ValidationContext<TEntity>
{
    /// <summary>
    /// Determines whether the specified property has an error.
    /// </summary>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="propertyExpression">The property expression.</param>
    /// <returns><see langword="true"/> where an error exists for the specified property; otherwise, <see langword="false"/>.</returns>
    public bool HasError<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression) => HasError<TProperty>(RuntimeMetadata.GetForExpression(propertyExpression));

    /// <summary>
    /// Determines whether the specified property has an error.
    /// </summary>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="propertyMetadata">The property metadata.</param>
    /// <returns><see langword="true"/> where an error exists for the specified property; otherwise, <see langword="false"/>.</returns>
    internal bool HasError<TProperty>(IPropertyRuntimeMetadata propertyMetadata) => HasError(CreateFullyQualifiedPropertyName(propertyMetadata.Name));

    /// <summary>
    /// Checks whether a specified property has not had an error, then executes a predicate to determine whether an error has occurred and where <see langword="true"/> adds an error <see cref="MessageItem"/>.
    /// </summary>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="propertyExpression">The property expression.</param>
    /// <param name="predicate">The error checking predicate; a <see langword="true"/> result indicates an error.</param>
    /// <param name="format">The composite format string.</param>
    /// <param name="values">The values that form part of the message text.</param>
    /// <returns><see langword="true"/> indicates that the specified property has had an error, or is now considered in error; otherwise, <see langword="false"/> for no error.</returns>
    /// <remarks>The property friendly text and value are automatically passed as the first two arguments to the underlying string formatter.</remarks>
    public bool Check<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression, Predicate<TProperty> predicate, LText format, params object?[] values)
    {
        var pm = RuntimeMetadata.GetForExpression(propertyExpression);
        predicate.ThrowIfNull();

        if (HasError<TProperty>(pm))
            return true;

        if (!predicate(pm.GetValue<TProperty>(Value)))
            return false;

        AddError<TProperty>(pm, null, null, null, format, values);
        return true;
    }

    /// <summary>
    /// Checks whether a specified property has not had an error, then <paramref name="when"/> <see langword="true"/> adds an error <see cref="MessageItem"/>.
    /// </summary>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="propertyExpression">The property expression.</param>
    /// <param name="when"><see langword="true"/> indicates an error; otherwise, <see langword="false"/>.</param>
    /// <param name="format">The composite format string.</param>
    /// <param name="values">The values that form part of the message text.</param>
    /// <returns><see langword="true"/> indicates that the specified property has had an error, or is now considered in error; otherwise, <see langword="false"/> for no error.</returns>
    /// <remarks>The property friendly text and value are automatically passed as the first two arguments to the underlying string formatter.</remarks>
    public bool Check<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression, bool when, LText format, params object?[] values)
    {
        var pm = RuntimeMetadata.GetForExpression(propertyExpression);

        if (HasError<TProperty>(pm))
            return true;

        if (!when)
            return false;

        AddError<TProperty>(pm, null, null, null, format, values);
        return true;
    }

    /// <summary>
    /// Adds an <see cref="MessageType.Error"/> <see cref="MessageItem"/> to the <see cref="Messages"/> for the specified property, explicit text format and and additional values included in the text. 
    /// </summary>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="propertyExpression">The property expression.</param>
    /// <param name="format">The composite format string.</param>
    /// <param name="values">The values that form part of the message text.</param>
    /// <returns>The <see cref="MessageItem"/>.</returns>
    /// <remarks>The property friendly text and value are automatically prepended to the <paramref name="values"/> as the first two arguments where the <paramref name="format"/> does not have already have <see cref="LText.HasArgs"/>.</remarks>
    public MessageItem AddError<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression, LText format, params object?[] values)
        => AddError<TProperty>(RuntimeMetadata.GetForExpression(propertyExpression), null, null, null, format, values);

    /// <summary>
    /// Adds an <see cref="MessageType.Error"/> <see cref="MessageItem"/> to the <see cref="Messages"/> for the specified property, explicit text format and and additional values included in the text. 
    /// </summary>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="propertyMetadata">The <see cref="IPropertyRuntimeMetadata"/>.</param>
    /// <param name="text">The property <see cref="LText"/> override.</param>
    /// <param name="valueFormatter">The <see cref="ValueFormatter"/>.</param>
    /// <param name="jsonNameOverride">The JSON property name override.</param>
    /// <param name="format">The composite format string.</param>
    /// <param name="values">The values that form part of the message text.</param>
    /// <returns>The <see cref="MessageItem"/>.</returns>
    /// <remarks>The property friendly text and value are automatically prepended to the <paramref name="values"/> as the first two arguments where the <paramref name="format"/> does not have already have <see cref="LText.HasArgs"/>.</remarks>
    internal MessageItem AddError<TProperty>(IPropertyRuntimeMetadata propertyMetadata, LText? text, ValueFormatter? valueFormatter, string? jsonNameOverride, LText format, params object?[] values)
    {
        format = format.EnsureNoArgsWhen(values);

        text ??= propertyMetadata.Text;
        jsonNameOverride ??= propertyMetadata.GetJsonName(JsonSerializerOptions);
        object?[] std = [text, valueFormatter is null ? propertyMetadata.GetValue(Value) : valueFormatter.Value.ToLText(propertyMetadata.GetValue(Value))];

        if (propertyMetadata is ISelfRuntimeMetadata)
            return AddError(format, [.. std, .. values]);
        else
            return AddError(propertyMetadata.Name, jsonNameOverride ?? propertyMetadata.GetJsonName(JsonSerializerOptions), format, [.. std, .. values]);
    }

    /// <summary>
    /// Adds an <see cref="MessageType.Error"/> <see cref="MessageItem"/> to the <see cref="Messages"/> for the specified property, explicit text format and and additional values included in the text. 
    /// </summary>
    /// <param name="propertyName">The property name.</param>
    /// <param name="jsonPropertyName">The JSON property name.</param>
    /// <param name="format">The composite format string.</param>
    /// <param name="values">The values that form part of the message text.</param>
    /// <returns>The <see cref="MessageItem"/>.</returns>
    internal MessageItem AddError(string propertyName, string jsonPropertyName, LText format, params object?[] values)
    {
        propertyName.ThrowIfNullOrEmpty();
        var fqpn = CreateFullyQualifiedPropertyName(propertyName);
        var mi = new ValidationMessageItem()
        {
            Property = UseJsonNames ? CreateFullyQualifiedJsonPropertyName(jsonPropertyName) : fqpn,
            Type = MessageType.Error,
            Text = format.EnsureNoArgs().WithArgs(values),
            FullyQualifiedPropertyName = fqpn
        };

        GetMessages().Add(mi);
        return mi;
    }

    /// <summary>
    /// Adds an <see cref="MessageType.Error"/> <see cref="MessageItem"/> to the <see cref="Messages"/> for the explicit text format and and additional values included in the text. 
    /// </summary>
    /// <param name="format">The composite format string.</param>
    /// <param name="values">The values that form part of the message text.</param>
    /// <returns>The <see cref="MessageItem"/>.</returns>
    internal MessageItem AddError(LText format, params object?[] values)
    {
        var mi = new ValidationMessageItem()
        {
            Property = UseJsonNames ? (FullyQualifiedJsonEntityName ?? string.Empty) : FullyQualifiedEntityName,
            Type = MessageType.Error,
            Text = format.EnsureNoArgs().WithArgs(values),
            FullyQualifiedPropertyName = FullyQualifiedEntityName
        };

        GetMessages().Add(mi);
        return mi;
    }

    /// <summary>
    /// Creates a fully qualified property name appending the <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The property name.</param>
    public string CreateFullyQualifiedPropertyName(string name) => FullyQualifiedEntityName is null ? name : name.StartsWith('[') ? FullyQualifiedEntityName + name : FullyQualifiedEntityName + "." + name;

    /// <summary>
    /// Creates a fully qualified JSON property name appending the <paramref name="jsonName"/>.
    /// </summary>
    /// <param name="jsonName">The JSON property name.</param>
    public string CreateFullyQualifiedJsonPropertyName(string jsonName) => FullyQualifiedJsonEntityName is null ? jsonName : (jsonName.StartsWith('[') ? FullyQualifiedJsonEntityName + jsonName : FullyQualifiedJsonEntityName + "." + jsonName);
}