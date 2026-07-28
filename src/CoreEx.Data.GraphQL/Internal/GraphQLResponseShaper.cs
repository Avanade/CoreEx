namespace CoreEx.Data.GraphQL.Internal;

/// <summary>
/// Reshapes a <see cref="JsonFilter"/>-projected result (keyed by real JSON property names) into the exact shape the client requested, honoring field aliases at every depth and injecting
/// <c>__typename</c> values where requested.
/// </summary>
/// <remarks><see cref="JsonFilter"/> only removes non-selected properties; it has no concept of GraphQL aliases or the synthetic <c>__typename</c> meta-field, so this second pass reconciles the
/// filtered JSON against the original <see cref="GraphQLSelectionSet"/> to produce a spec-correct response shape.</remarks>
internal static class GraphQLResponseShaper
{
    /// <summary>
    /// Reshapes <paramref name="node"/> (an object or array already filtered to the requested real property names) per <paramref name="selectionSet"/>.
    /// </summary>
    /// <param name="node">The <see cref="JsonFilter"/>-projected <see cref="JsonNode"/> (object or array).</param>
    /// <param name="selectionSet">The original GraphQL selection set requested for this field.</param>
    /// <param name="fieldMap">The reflected field map for the object's <see cref="Type"/>.</param>
    /// <param name="typeName">The object's <see cref="Type"/> name, used to populate any requested <c>__typename</c> field.</param>
    /// <returns>The reshaped <see cref="JsonNode"/> (aliased, with <c>__typename</c> populated where requested).</returns>
    public static JsonNode? Shape(JsonNode? node, GraphQLSelectionSet? selectionSet, IReadOnlyDictionary<string, GraphQLFieldNode> fieldMap, string typeName)
    {
        if (node is JsonArray array)
        {
            var shapedArray = new JsonArray();
            foreach (var item in array)
                shapedArray.Add(Shape(item?.DeepClone(), selectionSet, fieldMap, typeName));

            return shapedArray;
        }

        if (node is not JsonObject obj || selectionSet?.Selections is null)
            return node;

        var result = new JsonObject();
        foreach (var selection in selectionSet.Selections)
        {
            // Fragments/unknown fields are already reported as errors during GraphQLSelectionResolver.Resolve; execution is short-circuited before reaching here, so any non-field selection is unreachable.
            if (selection is not GraphQLField field)
                continue;

            var name = field.Name.StringValue;
            var alias = field.Alias?.Name.StringValue ?? name;

            if (name == GraphQLSelectionResolver.TypeNameField)
            {
                result[alias] = typeName;
                continue;
            }

            if (!fieldMap.TryGetValue(name, out var fieldNode) || !obj.TryGetPropertyValue(fieldNode.JsonName, out var value))
                continue;

            result[alias] = fieldNode.IsComplex && fieldNode.Children is not null
                ? Shape(value?.DeepClone(), field.SelectionSet, fieldNode.Children.Value, (Nullable.GetUnderlyingType(fieldNode.ElementType ?? fieldNode.PropertyType) ?? (fieldNode.ElementType ?? fieldNode.PropertyType)).Name)
                : value?.DeepClone();
        }

        return result;
    }
}
