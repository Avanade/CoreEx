namespace CoreEx.Metadata;

public static partial class RuntimeMetadata
{
    /// <summary>
    /// Gets the dot-separated JSON path for a direct or nested property expression.
    /// </summary>
    /// <typeparam name="TEntity">The root entity type.</typeparam>
    /// <param name="propertyExpression">A property chain rooted at the entity parameter.</param>
    /// <param name="options">The serializer options used for naming; defaults to <see cref="JsonDefaults.SerializerOptions"/>.</param>
    /// <returns>The JSON path with each segment resolved using its owning type's runtime metadata.</returns>
    /// <remarks>Property and collection access is not executed. Supports outermost boxing, one-dimensional array/list indexes, and string dictionary keys.
    /// Indexes and keys must be constants or captured locals. String keys use the effective serializer's dictionary key naming policy.</remarks>
    /// <exception cref="ArgumentException">The expression is not a supported property chain.</exception>
    public static string GetJsonPath<TEntity>(Expression<Func<TEntity, object?>> propertyExpression, JsonSerializerOptions? options = null) where TEntity : class
        => GetJsonPathAndText(propertyExpression, options).Path;

    /// <summary>
    /// Gets the JSON path and friendly text of the last property in an expression.
    /// </summary>
    /// <typeparam name="TEntity">The root entity type.</typeparam>
    /// <param name="propertyExpression">The direct, nested, or indexed property expression.</param>
    /// <param name="options">The JSON naming options, or null to use the defaults.</param>
    /// <returns>The JSON path and localized property text. For a terminal index/key, uses the containing property's text.</returns>
    /// <remarks>Uses the same expression restrictions as <see cref="GetJsonPath{TEntity}"/> without executing entity access.</remarks>
    public static (string Path, LText Text) GetJsonPathAndText<TEntity>(Expression<Func<TEntity, object?>> propertyExpression, JsonSerializerOptions? options = null) where TEntity : class
    {
        Expression? expression = propertyExpression.ThrowIfNull().Body;
        if (expression is UnaryExpression { NodeType: ExpressionType.Convert, Type: var type, Method: null } conversion && type == typeof(object))
            expression = conversion.Operand;

        if (expression == propertyExpression.Parameters[0])
            throw InvalidJsonPath();

        var path = new StringBuilder();
        var text = Validation.Validation.ValueText;
        AppendJsonPath(expression, propertyExpression.Parameters[0], options, path, ref text);
        return (path.ToString(), text);
    }

    /// <summary>
    /// Appends property names and collection locations without executing entity access.
    /// </summary>
    private static void AppendJsonPath(Expression? expression, ParameterExpression root, JsonSerializerOptions? options, StringBuilder path, ref LText text)
    {
        if (expression == root)
            return;

        if (expression is MemberExpression { Member: PropertyInfo property, Expression: not null } member
            && property.GetMethod is { IsPublic: true, IsStatic: false } && property.GetIndexParameters().Length == 0)
        {
            AppendJsonPath(member.Expression, root, options, path, ref text);
            var propertyMetadata = GetForProperty(member.Expression.Type, property);

            if (path.Length > 0)
                path.Append('.');

            path.Append(propertyMetadata.GetJsonName(options));
            text = propertyMetadata.Text;
            return;
        }

        if (expression is BinaryExpression { NodeType: ExpressionType.ArrayIndex } array && array.Left.Type.GetArrayRank() == 1)
        {
            AppendJsonPath(array.Left, root, options, path, ref text);
            AppendJsonIndex(array.Left.Type, array.Right, options, path);
            return;
        }

        if (expression is MethodCallExpression { Object: not null, Arguments.Count: 1 } call
            && call.Object.Type.GetProperties().Any(p => p.GetMethod == call.Method && p.GetIndexParameters().Length == 1))
        {
            AppendJsonPath(call.Object, root, options, path, ref text);
            AppendJsonIndex(call.Object.Type, call.Arguments[0], options, path);
            return;
        }

        throw InvalidJsonPath();
    }

    /// <summary>
    /// Appends an index or dictionary key using the same notation as validation contexts.
    /// </summary>
    private static void AppendJsonIndex(Type collectionType, Expression argument, JsonSerializerOptions? options, StringBuilder path)
    {
        var value = argument switch
        {
            ConstantExpression constant => constant.Value,
            MemberExpression { Member: FieldInfo field, Expression: ConstantExpression closure } when field.DeclaringType?.IsDefined(typeof(CompilerGeneratedAttribute), false) == true => field.GetValue(closure.Value),
            _ => throw InvalidJsonPath()
        };

        var types = collectionType.GetInterfaces().Append(collectionType);
        if (types.Any(t => t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(IDictionary<,>) || t.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>))
            && t.GetGenericArguments()[0] == typeof(string)) && value is string key)
        {
            path.Append('.').Append((options ?? JsonDefaults.SerializerOptions).DictionaryKeyPolicy?.ConvertName(key) ?? key);
            return;
        }

        if ((collectionType.IsArray || types.Any(t => t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(IList<>) || t.GetGenericTypeDefinition() == typeof(IReadOnlyList<>))))
            && value is int index && index >= 0)
        {
            path.Append('[').Append(index).Append(']');
            return;
        }

        throw InvalidJsonPath();
    }

    /// <summary>
    /// Creates the error for unsupported property paths or index arguments.
    /// </summary>
    private static ArgumentException InvalidJsonPath()
        => new("Only public instance property paths with array/list indexes or string dictionary keys (constants or captured locals) are supported.", "propertyExpression");
}
