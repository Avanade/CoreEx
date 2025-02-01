// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Json.Compare;
using CoreEx.Results;
using CoreEx.Text.Json;
using System;
using System.Buffers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Json.Merge
{
    /// <summary>
    /// Provides a JSON Merge Patch (<c>application/merge-patch+json</c>) whereby the contents of a JSON document are merged into an existing JSON document resulting in a new merged JSON document as per <see href="https://tools.ietf.org/html/rfc7396"/>.
    /// </summary>
    /// <param name="options">The <see cref="JsonMergePatchOptions"/>.</param>
    public class JsonMergePatch(JsonMergePatchOptions? options = null) : IJsonMergePatch
    {
        /// <summary>
        /// Gets the <see cref="JsonMergePatchOptions"/>.
        /// </summary>
        public JsonMergePatchOptions Options { get; } = options ?? new JsonMergePatchOptions();

        /// <inheritdoc/>
        public bool Merge<T>(BinaryData json, ref T? value)
        {
            // Parse the JSON.
            var j = ParseJson<T>(json.ThrowIfNull(nameof(json)));
            var t = SerializeToJsonElement(value);

            // Perform the root merge patch.
            if (!TryMerge(j.JsonElement, t, out var merged))
                return false;

            // Deserialize the merged JSON.
            value = DeserializeFromJsonElement<T>(merged);
            return true;
        }

        /// <inheritdoc/>
        public async Task<(bool HasChanges, T? Value)> MergeAsync<T>(BinaryData json, Func<T?, CancellationToken, Task<T?>> getValue, CancellationToken cancellationToken = default)
        {
            getValue.ThrowIfNull(nameof(getValue));

            // Parse the JSON.
            var j = ParseJson<T>(json.ThrowIfNull(nameof(json))); 

            // Get the value.
            var value = await getValue(j.Value, cancellationToken).ConfigureAwait(false);
            if (value == null)
                return (false, default!);

            // Perform the merge patch.
            var t = SerializeToJsonElement(value);
            if (!TryMerge(j.JsonElement, t, out var merged))
                return (false, value);

            // Deserialize the merged JSON.
            return (true, DeserializeFromJsonElement<T>(merged));
        }

        /// <inheritdoc/>
        public async Task<Result<(bool HasChanges, T? Value)>> MergeWithResultAsync<T>(BinaryData json, Func<T?, CancellationToken, Task<Result<T?>>> getValue, CancellationToken cancellationToken = default)
        {
            getValue.ThrowIfNull(nameof(getValue));

            // Parse the JSON.
            var j = ParseJson<T>(json.ThrowIfNull(nameof(json)));

            // Get the value.
            var result = await getValue(j.Value!, cancellationToken).ConfigureAwait(false);
            return result.ThenAs(value =>
            {
                if (value == null)
                    return (false, default);

                // Perform the merge patch.
                var t = SerializeToJsonElement(value);
                if (!TryMerge(true, j.JsonElement, t, out var merged))
                    return (false, value);

                // Deserialize the merged JSON.
                return (true, DeserializeFromJsonElement<T>(merged));
            });
        }

        /// <summary>
        /// Serialize the <paramref name="value"/> to a <see cref="JsonElement"/>.
        /// </summary>
        private JsonElement SerializeToJsonElement<T>(T value)
        {
            // Fast path where using System.Text.Json.
            if (Options.JsonSerializer is CoreEx.Text.Json.JsonSerializer js)
                return System.Text.Json.JsonSerializer.SerializeToElement(value, js.Options);

            // Otherwise, serialize and then parse as two separate operations (slower path).
            var bd = Options.JsonSerializer.SerializeToBinaryData(value);
            var jr = new Utf8JsonReader(bd);
            return JsonElement.ParseValue(ref jr).Clone();
        }

        /// <summary>
        /// Deserialize the <paramref name="json"/> to a <typeparamref name="T"/>.
        /// </summary>
        private T? DeserializeFromJsonElement<T>(JsonElement json)
        {
            // Fast path where using System.Text.Json.
            if (Options.JsonSerializer is CoreEx.Text.Json.JsonSerializer js)
                return json.Deserialize<T>(js.Options);

            // Otherwise, deserialize using the specified serializer.
            return Options.JsonSerializer.Deserialize<T>(json.GetRawText());
        }

        /// <summary>
        /// Parses the JSON.
        /// </summary>
        private (JsonElement JsonElement, T? Value) ParseJson<T>(BinaryData json)
        {
            try
            {
                // Deserialize into a temporary value which will be used as the merge source.
                var value = Options.JsonSerializer.Deserialize<T>(json);

                // Parse the JSON into a JsonElement which will be used to navigate the merge.
                var jr = new Utf8JsonReader(json);
                var je = JsonElement.ParseValue(ref jr).Clone();
                return (je, value);
            }
            catch (JsonException jex)
            {
                throw new JsonMergePatchException(jex.Message, jex);
            }
        }

        /// <summary>
        /// Merges the <paramref name="json"/> with (into) the <paramref name="target"/>.
        /// </summary>
        /// <param name="json">The JSON to merge.</param>
        /// <param name="target">The JSON target to merge with (into).</param>
        /// <returns>The resulting merged <see cref="JsonElement"/>.</returns>
        public JsonElement Merge(JsonElement json, JsonElement target)
        {
            TryMerge(false, json, target, out var merged);
            return merged;
        }

        /// <summary>
        /// Merges the <paramref name="json"/> with (into) the <paramref name="target"/> resulting in the <paramref name="merged"/> where changes were made.
        /// </summary>
        /// <param name="json">The JSON to merge.</param>
        /// <param name="target">The JSON to merge with (into).</param>
        /// <param name="merged">The resulting JSON where changes were made.</param>
        /// <returns><c>true</c> indicates that changes were made as a result of the merge (see resulting <paramref name="merged"/>); otherwise, <c>false</c> for no changes.</returns>
        public bool TryMerge(JsonElement json, JsonElement target, out JsonElement merged)
        {
            if (TryMerge(true, json, target, out var m))
            {
                merged = m;
                return true;
            }

            merged = target;
            return false;
        }

        /// <summary>
        /// Orchestrates the 'actual' merge processing. The `checkForChanges` is used to determine whether to check for changes after the completed merge only where necessary; therefore, the boolean result is not always guaranteed to be accurate :-)
        /// </summary>
        private bool TryMerge(bool checkForChanges, JsonElement json, JsonElement target, out JsonElement merged)
        {
            // Create writer for the merged output.
            var buffer = new ArrayBufferWriter<byte>();
            using var writer = new Utf8JsonWriter(buffer);

            var changed = TryMerge(json, target, writer, false);

            writer.Flush();

            // Read the merged output and parse.
            var reader = new Utf8JsonReader(buffer.WrittenSpan);
            merged = JsonElement.ParseValue(ref reader).Clone();

            // Where check for changes is enabled then compare the target and merged JSON if not previously identified.
            if (checkForChanges && !changed)
            {
                var comparer = new JsonElementComparer(new JsonElementComparerOptions { JsonSerializer = Options.JsonSerializer, PropertyNameComparer = Options.PropertyNameComparer, MaxDifferences = 1 });
                changed = comparer.Compare(target, merged).HasDifferences;
            }

            return changed;
        }

        /// <summary>
        /// Merge the JSON element (record 'change' where no additional cost to do so).
        /// </summary>
        private bool TryMerge(JsonElement json, JsonElement target, Utf8JsonWriter writer, bool changed)
        {
            // Where the kinds are different then simply accept the merge and acknowledge as changed.
            if (json.ValueKind != target.ValueKind)
            {
                json.WriteTo(writer);
                return true;
            }

            // Where the kinds are the same then process accordingly.
            switch (json.ValueKind)
            {
                case JsonValueKind.Object:
                    // An object is a property-by-property merge.
                    return TryObjectMerge(json, target, writer, changed);

                case JsonValueKind.Array:
                    // An array is always a replacement.
                    json.WriteTo(writer);
                    if (json.GetArrayLength() != target.GetArrayLength())
                        changed = true;

                    break;

                default:
                    // Accept merge as-is.
                    json.WriteTo(writer);
                    break;
            }

            return changed;
        }

        /// <summary>
        /// Merge the JSON object and properties (record 'change' where no cost to do so).
        /// </summary>
        private bool TryObjectMerge(JsonElement json, JsonElement target, Utf8JsonWriter writer, bool changed)
        {
            writer.WriteStartObject();

            // Apply merge add/override.
            foreach (var j in json.EnumerateObject())
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
                // Where found then consider as handled above!
                if (TryGetProperty(json, t.Name, out _))
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
            => Options.PropertyNameComparer is null ? json.TryGetProperty(propertyName, out value) : json.TryGetProperty(propertyName, Options.PropertyNameComparer, out value);
    }
}