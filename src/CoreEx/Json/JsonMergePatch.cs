namespace CoreEx.Json;

/// <summary>
/// Provides a JSON Merge Patch ('<c>application/merge-patch+json</c>') whereby the contents of a JSON document are merged into an existing JSON document resulting in a new merged JSON document as per <see href="https://tools.ietf.org/html/rfc7396"/>.
/// </summary>
/// <param name="options">The optional <see cref="JsonMergePatchOptions"/>.</param>
/// <remarks><i>Note:</i> The formal specification <see href="https://tools.ietf.org/html/rfc7396"/> explicitly states that an <see cref="JsonValueKind.Array"/> is to be a replacement operation. Additionally, <see langword="null"/>
/// values in the merge patch are given special meaning to indicate the removal of existing values in the target.</remarks>
public sealed class JsonMergePatch(JsonMergePatchOptions? options = null)
{
    /// <summary>
    /// Gets the <see cref="JsonMergePatchOptions"/>.
    /// </summary>
    public JsonMergePatchOptions Options { get; } = options ?? new JsonMergePatchOptions();

    /// <summary>
    /// Merges the <paramref name="patch"/> content into the <paramref name="target"/>.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="patch">The JSON to merge patch.</param>
    /// <param name="target">The target to merge into.</param>
    /// <returns>The <see cref="JsonMergePatchResult{T}"/> <see cref="Result{T}"/>.</returns>
    public Result<JsonMergePatchResult<T>> Merge<T>([StringSyntax(StringSyntaxAttribute.Json)] string patch, T target) => Merge(new BinaryData(patch.ThrowIfNull()), target);

    /// <summary>
    /// Merges the <paramref name="patch"/> content into the <paramref name="target"/>.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="patch">The JSON to merge patch.</param>
    /// <param name="target">The target to merge into.</param>
    /// <returns>The <see cref="JsonMergePatchResult{T}"/> <see cref="Result{T}"/>.</returns>
    public Result<JsonMergePatchResult<T>> Merge<T>(BinaryData patch, T target)
    {
        // Parse ensuring the JSON is valid for the type and can be navigated before attempting merge.
        if (!TryParseJson<T>(patch.ThrowIfNull(), out var r))
            return r;

        // No merge will occur when the target is null.
        if (target is null)
            return new JsonMergePatchResult<T>();

        // Perform the merge patch.
        return MergePatch(patch, target);
    }

    /// <summary>
    /// Merges the <paramref name="patch"/> content into the value returned by the <paramref name="getTarget"/> function.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="patch">The JSON to merge patch.</param>
    /// <param name="getTarget">The function to get the target to merge into.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="JsonMergePatchResult{T}"/> <see cref="Result{T}"/>.</returns>
    /// <remarks>Provides the opportunity to validate the JSON before getting the value where this execution order is important; i.e. get operation is expensive (latency). A <paramref name="getTarget"/> that returns <see langword="null"/>
    /// will immediately exit without performing any merge.</remarks>
    public Task<Result<JsonMergePatchResult<T>>> MergeAsync<T>(BinaryData patch, Func<CancellationToken, Task<T?>> getTarget, CancellationToken cancellationToken = default)
        => MergeWithResultAsync(patch, async ct => Result.Ok(await getTarget.ThrowIfNull()(ct).ConfigureAwait(false)), cancellationToken);

    /// <summary>
    /// Merges the <paramref name="patch"/> content into the value returned by the <paramref name="getTarget"/> function.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="patch">The JSON to merge patch.</param>
    /// <param name="getTarget">The function to get the target to merge into.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="JsonMergePatchResult{T}"/> <see cref="Result{T}"/>.</returns>
    /// <remarks>Provides the opportunity to validate the JSON before getting the value where this execution order is important; i.e. get operation is expensive (latency). A <paramref name="getTarget"/> that returns <see langword="null"/>
    /// will immediately exit without performing any merge.</remarks>
    public async Task<Result<JsonMergePatchResult<T>>> MergeWithResultAsync<T>(BinaryData patch, Func<CancellationToken, Task<Result<T?>>> getTarget, CancellationToken cancellationToken = default)
    {
        // Parse ensuring the JSON is valid for the type and can be navigated.
        if (!TryParseJson<T>(patch.ThrowIfNull(), out var r))
            return r;

        // Get the value and exit where nothing to merge into.
        var target = await getTarget.ThrowIfNull().Invoke(cancellationToken).ConfigureAwait(false);
        if (target.IsFailure)
            return target.Error;

        if (target.Value is null)
            return new JsonMergePatchResult<T>();

        // Perform the merge patch.
        return MergePatch(patch, target.Value);
    }

    /// <summary>
    /// Merges the <paramref name="patch"/> content into the <paramref name="target"/>.
    /// </summary>
    private Result<JsonMergePatchResult<T>> MergePatch<T>(BinaryData patch, T target)
    {
        // Serialize the merge into value as will be using JsonElements to perform.
        var vje = JsonSerializer.SerializeToElement(target, Options.JsonSerializerOptions);

        // Perform the root merge patch and return the corresponding result.
        var jd = JsonDocument.Parse(patch.ToMemory());
        if (!TryMerge(jd.RootElement, vje, out ArrayBufferWriter<byte>? merged))
            return Result.Ok(new JsonMergePatchResult<T> { HasChanges = false, Merged = target });

        var reader = new Utf8JsonReader(merged.WrittenSpan);
        return Result.Ok(new JsonMergePatchResult<T> { HasChanges = true, Merged = JsonSerializer.Deserialize<T>(ref reader, Options.JsonSerializerOptions) });
    }

    /// <summary>
    /// Tries to parse the JSON outputting a failed result where unsuccessful.
    /// </summary>
    private bool TryParseJson<T>(BinaryData patch, out Result<JsonMergePatchResult<T>> result)
    {
        try
        {
            patch.ToObjectFromJson<T>(Options.JsonSerializerOptions);
            result = new();
            return true;
        }
        catch (JsonException jex)
        {
            result = new Result<JsonMergePatchResult<T>>(jex);
            return false;
        }
    }

    /// <summary>
    /// Merges the <paramref name="patch"/> with the <paramref name="target"/> with a merged result.
    /// </summary>
    /// <param name="patch">The JSON to merge patch.</param>
    /// <param name="target">The JSON target to merge with (into).</param>
    /// <returns>The resulting merged JSON.</returns>
    public string Merge([StringSyntax(StringSyntaxAttribute.Json)] string patch, [StringSyntax(StringSyntaxAttribute.Json)] string target)
        => Merge(JsonDocument.Parse(patch.ThrowIfNullOrEmpty()).RootElement, JsonDocument.Parse(target.ThrowIfNullOrEmpty()).RootElement).GetRawText();

    /// <summary>
    /// Merges the <paramref name="json"/> with the <paramref name="target"/>.
    /// </summary>
    /// <param name="json">The JSON to merge patch.</param>
    /// <param name="target">The JSON target to merge with (into).</param>
    /// <returns>The resulting merged <see cref="JsonElement"/>.</returns>
    public JsonElement Merge(JsonElement json, JsonElement target)
    {
        TryMerge(json, target, out JsonElement merged);
        return merged;
    }

    /// <summary>
    /// Merges the <paramref name="patch"/> with the <paramref name="target"/> resulting in merged JSON where changes were made.
    /// </summary>
    /// <param name="patch">The JSON to merge patch.</param>
    /// <param name="target">The JSON to merge with (into).</param>
    /// <param name="merged">The resulting merged JSON.</param>
    /// <returns><see langword="true"/> indicates that changes were made as a result of the merge (see resulting <paramref name="merged"/>); otherwise, <see langword="false"/> for no changes.</returns>
    public bool TryMerge([StringSyntax(StringSyntaxAttribute.Json)] string patch, [StringSyntax(StringSyntaxAttribute.Json)] string target, [NotNullWhen(true)] out string? merged)
    {
        if (!TryMerge(JsonDocument.Parse(patch.ThrowIfNullOrEmpty()).RootElement, JsonDocument.Parse(target.ThrowIfNullOrEmpty()).RootElement, out ArrayBufferWriter<byte>? buffer))
        {
            merged = null;
            return false;
        }

        // Read the merged writer and parse.
        var reader = new Utf8JsonReader(buffer.WrittenSpan);
        merged = JsonElement.ParseValue(ref reader).Clone().GetRawText();
        return true;
    }

    /// <summary>
    /// Merges the <paramref name="patch"/> with the <paramref name="target"/> resulting in the <paramref name="merged"/> where changes were made.
    /// </summary>
    /// <param name="patch">The JSON to merge patch.</param>
    /// <param name="target">The JSON to merge with (into).</param>
    /// <param name="merged">The resulting merged JSON.</param>
    /// <returns><see langword="true"/> indicates that changes were made as a result of the merge (see resulting <paramref name="merged"/>); otherwise, <see langword="false"/> for no changes.</returns>
    public bool TryMerge(JsonElement patch, JsonElement target, out JsonElement merged)
    {
        // Merge and where no changes then return the target as-is.
        if (!TryMerge(patch, target, out ArrayBufferWriter<byte>? buffer))
        {
            merged = target;
            return false;
        }

        // Read the merged writer and parse.
        var reader = new Utf8JsonReader(buffer.WrittenSpan);
        merged = JsonElement.ParseValue(ref reader).Clone();
        return true;
    }

    /// <summary>
    /// Merge the JSON elements (root).
    /// </summary>
    private bool TryMerge(JsonElement patch, JsonElement target, [NotNullWhen(true)] out ArrayBufferWriter<byte>? merged)
    {
        // Create writer for the merged output.
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);

        // Merge for real!
        var changed = TryMerge(patch, target, writer, false);

        // Where no changes then return the target as-is.
        if (!changed)
        {
            merged = null;
            return false;
        }

        // Read the merged writer and parse.
        writer.Flush();
        merged = buffer;
        return true;
    }

    /// <summary>
    /// Merge the JSON elements (recursive).
    /// </summary>
    private bool TryMerge(JsonElement patch, JsonElement target, Utf8JsonWriter writer, bool changed)
    {
        // Where the kinds are different then simply accept the merge and acknowledge as changed.
        if (patch.ValueKind != target.ValueKind)
        {
            patch.WriteTo(writer);
            return true;
        }

        // Where the kinds are the same then process accordingly.
        switch (patch.ValueKind)
        {
            case JsonValueKind.Object:
                // An object is a property-by-property merge.
                return TryObjectMerge(patch, target, writer, changed);

            case JsonValueKind.Array:
                // An array is always a replacement.
                patch.WriteTo(writer);
#if NET8_0
                if (patch.GetArrayLength() != target.GetArrayLength() || !DeepEquals(patch, target))
#else
                if (patch.GetArrayLength() != target.GetArrayLength() || !JsonElement.DeepEquals(patch, target))
#endif
                    changed = true;

                break;

            default:
                // Accept merge as-is.
                patch.WriteTo(writer);
#if NET8_0
                if (!DeepEquals(patch, target))
#else
                if (!JsonElement.DeepEquals(patch, target))
#endif
                    changed = true;

                break;
        }

        return changed;
    }

    /// <summary>
    /// Merge the JSON object and properties and record changed.
    /// </summary>
    private bool TryObjectMerge(JsonElement patch, JsonElement target, Utf8JsonWriter writer, bool changed)
    {
        writer.WriteStartObject();

        // Apply merge add/override.
        foreach (var j in patch.EnumerateObject())
        {
            // Where the property is new then add.
            if (!TryGetProperty(target, j.Name, out var t))
            {
                if (j.Value.ValueKind != JsonValueKind.Null)
                    j.WriteTo(writer);

                changed = true;
                continue;
            }

            // Null is a remove; otherwise, override.
            if (j.Value.ValueKind != JsonValueKind.Null)
            {
                writer.WritePropertyName(j.Name);
                changed = TryMerge(j.Value, t, writer, changed);
            }
            else
                changed = true;
        }

        // Add existing target properties not being merged.
        foreach (var t in target.EnumerateObject())
        {
            // Where found then consider as handled above and ignore!
            if (TryGetProperty(patch, t.Name, out _))
                continue;

            t.WriteTo(writer);
        }

        writer.WriteEndObject();
        return changed;
    }

    /// <summary>
    /// Performs the TryGetProperty using the configured comparer.
    /// </summary>
    private bool TryGetProperty(JsonElement json, string propertyName, out JsonElement value)
        => Options.PropertyNameComparer is null ? json.TryGetProperty(propertyName, out value) : TryGetPropertyWithComparer(json, propertyName, Options.PropertyNameComparer, out value);

    /// <summary>
    /// Performs the TryGetProperty using the specified comparer (this will impact performance).
    /// </summary>
    private static bool TryGetPropertyWithComparer(JsonElement json, string propertyName, StringComparer comparer, out JsonElement value)
    {
        foreach (var j in json.EnumerateObject())
        {
            if (comparer.Equals(j.Name, propertyName))
            {
                value = j.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

#if NET8_0
    /// <summary>
    /// Provides a deep equals for two <see cref="JsonElement"/> instances.
    /// </summary>
    /// <param name="left">The left <see cref="JsonElement"/>.</param>
    /// <param name="right">The right <see cref="JsonElement"/>.</param>
    internal static bool DeepEquals(JsonElement left, JsonElement right)
    {
        if (left.ValueKind != right.ValueKind)
        {
            return false;
        }

        switch (left.ValueKind)
        {
            case JsonValueKind.Null:
            case JsonValueKind.False:
            case JsonValueKind.True:
                // These are the same by kind, so carry on!
                return true;

            case JsonValueKind.Number:
            case JsonValueKind.String:
                return left.GetRawText() == right.GetRawText();

            case JsonValueKind.Array:
                if (left.GetArrayLength() != right.GetArrayLength())
                    return false;

                var rja = right.EnumerateArray();
                foreach (var lje in left.EnumerateArray())
                {
                    rja.MoveNext();
                    if (!DeepEquals(lje, rja.Current))
                        return false;
                }

                return true;

            default:
                foreach (var l in left.EnumerateObject())
                {
                    if (!right.TryGetProperty(l.Name, out var r))
                    {
                        if (!DeepEquals(l.Value, r))
                            return false;
                    }
                }

                foreach (var r in right.EnumerateObject())
                {
                    if (!left.TryGetProperty(r.Name, out var _))
                        return false;
                }

                return true;
        }
    }
#endif
}