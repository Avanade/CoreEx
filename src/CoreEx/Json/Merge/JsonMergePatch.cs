// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions.Reflection;
using CoreEx.Entities;
using System;
using System.Collections;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace CoreEx.Json.Merge
{
    /// <summary>
    /// Provides a JSON Merge Patch (<c>application/merge-patch+json</c>) whereby the contents of a JSON document are merged into an existing object value as per <see href="https://tools.ietf.org/html/rfc7396"/>.
    /// </summary>
    /// <remarks><para>This object should be reused where possible as it caches the JSON serialization semantics internally to improve performance. It is also thread-safe.</para>
    /// <para>Additional logic has been added to the merge patch enabled by <see cref="JsonMergePatchOptions.DictionaryMergeApproach"/> and <see cref="JsonMergePatchOptions.PrimaryKeyCollectionMergeApproach"/>. Note: these capabilities are
    /// unique to <i>CoreEx</i> and not part of the formal specification <see href="https://tools.ietf.org/html/rfc7396"/>.</para></remarks>
    public class JsonMergePatch : IJsonMergePatch
    {
        private const string PKCollectionName = "PKCollection";
        private readonly EntityReflectorArgs _erArgs;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonMergePatch"/> class.
        /// </summary>
        /// <param name="args">The <see cref="JsonMergePatchOptions"/>.</param>
        /// <remarks>This object should be reused where possible as it caches the JSON serialization semantics internally to improve performance. It is also thread-safe.</remarks>
        public JsonMergePatch(JsonMergePatchOptions? args = null)
        {
            Options = args ?? new JsonMergePatchOptions();

            _erArgs = new EntityReflectorArgs(Options.JsonSerializer)
            {
                AutoPopulateProperties = true,
                NameComparer = Options.NameComparer,
                PropertyBuilder = pr =>
                {
                    // Only interested in properties that are considered serializable
                    if (!pr.PropertyExpression.IsJsonSerializable)
                        return false;

                    // Determine if the property implements IPrimaryKeyCollection.
                    if (pr.Type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPrimaryKeyCollection<>)))
                        pr.Data.Add(PKCollectionName, true);

                    return true;
                }
            };
        }

        /// <summary>
        /// Gets the <see cref="JsonMergePatchOptions"/>.
        /// </summary>
        public JsonMergePatchOptions Options { get; set; }

        /// <inheritdoc/>
        public bool Merge<T>(string json, T value) where T : class
        {
            if (json == null)
                throw new ArgumentNullException(nameof(json));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            // Parse the JSON.
            var j = ParseJson<T>(json);

            // Perform the object merge patch.
            bool hasChanged = false;
            MergeObject(Options, EntityReflector.GetReflector<T>(_erArgs), "$", j.JsonElement, j.Value, value, ref hasChanged);
            return hasChanged;
        }

        /// <inheritdoc/>
        public async Task<bool> MergeAsync<T>(string json, Func<T, Task<T>> getValue) where T : class
        {
            if (json == null)
                throw new ArgumentNullException(nameof(json));

            if (getValue == null)
                throw new ArgumentNullException(nameof(getValue));

            // Parse the JSON.
            var j = ParseJson<T>(json); 

            // Get the value.
            var value = await getValue(j.Value).ConfigureAwait(false);
            if (value == null)
                return false;

            // Perform the object merge patch.
            bool hasChanged = false;
            MergeObject(Options, EntityReflector.GetReflector<T>(_erArgs), "$", j.JsonElement, j.Value, value, ref hasChanged);
            return hasChanged;
        }

        /// <summary>
        /// Parses the JSON.
        /// </summary>
        private (JsonElement JsonElement, T Value) ParseJson<T>(string json)
        {
            JsonElement je;
            T? value;

            try
            {
                // Deserialize into a temporary value which will be used as the merge source.
                value = Options.JsonSerializer.Deserialize<T>(json);
                if (value == null)
                    throw new JsonMergePatchException($"The JSON is malformed and could not be parsed.");

                // Parse the JSON into a JsonElement which will be used to navigate the merge.
                var jr = new Utf8JsonReader(new BinaryData(json));
                je = JsonElement.ParseValue(ref jr);
            }
            catch (JsonException jex)
            {
                throw new JsonMergePatchException(jex.Message, jex);
            }

            return (je, value!);
        }

        /// <summary>
        /// Merge the object.
        /// </summary>
        private static void MergeObject(JsonMergePatchOptions args, IEntityReflector er, string root, JsonElement json, object? srce, object dest, ref bool hasChanged)
        {
            foreach (var jp in json.EnumerateObject())
            {
                // Find the named property; skip when not found.
                var pr = er.GetJsonProperty(jp.Name);
                if (pr == null)
                    continue;

                MergeProperty(args, pr, $"{root}.{jp.Name}", jp, srce, dest, ref hasChanged);
            }
        }

        /// <summary>
        /// Merge the property.
        /// </summary>
        private static void MergeProperty(JsonMergePatchOptions args, IPropertyReflector pr, string path, JsonProperty json, object? srce, object dest, ref bool hasChanged)
        {
            // Update according to the value kind.
            switch (json.Value.ValueKind)
            {
                case JsonValueKind.Null:
                case JsonValueKind.True:
                case JsonValueKind.False:
                case JsonValueKind.String:
                case JsonValueKind.Number:
                    // Update the value directly from the source.
                    SetPropertyValue(pr, pr.PropertyExpression.GetValue(srce)!, dest, ref hasChanged);
                    break;

                case JsonValueKind.Object:
                    // Where existing is null, copy source as-is; otherwise, merge object property-by-property.
                    var current = pr.PropertyExpression.GetValue(dest);
                    if (current == null)
                        SetPropertyValue(pr, pr.PropertyExpression.GetValue(srce)!, dest, ref hasChanged);
                    else
                    {
                        if (pr.TypeCode == TypeReflectorTypeCode.IDictionary)
                        {
                            if (args.DictionaryMergeApproach == DictionaryMergeApproach.Replace)
                                SetPropertyValue(pr, pr.PropertyExpression.GetValue(srce)!, dest, ref hasChanged);
                            else
                            {
                                var dict = MergeDictionary(args, pr, path, json.Value, (IDictionary)pr.PropertyExpression.GetValue(srce)!, (IDictionary)pr.PropertyExpression.GetValue(dest)!, ref hasChanged);
                                SetPropertyValue(pr, dict, dest, ref hasChanged);
                            }
                        }
                        else
                            MergeObject(args, pr.GetEntityReflector()!, path, json.Value, pr.PropertyExpression.GetValue(srce), current, ref hasChanged);
                    }

                    break;

                case JsonValueKind.Array:
                    // Unless explicitly requested an array is a full replacement only (source copy); otherwise, perform key collection item merge.
                    if (args.PrimaryKeyCollectionMergeApproach != PrimaryKeyCollectionMergeApproach.Replace && pr.Data.ContainsKey(PKCollectionName))
                    {
                        var coll = MergeKeyedCollection(args, pr, path, json.Value, (IKeyedCollection)pr.PropertyExpression.GetValue(srce)!, (IKeyedCollection)pr.PropertyExpression.GetValue(dest)!, ref hasChanged);
                        SetPropertyValue(pr, coll, dest, ref hasChanged);
                    }
                    else
                        SetPropertyValue(pr, pr.PropertyExpression.GetValue(srce)!, dest, ref hasChanged);

                    break;
            }
        }

        /// <summary>
        /// Sets the property value.
        /// </summary>
        private static void SetPropertyValue(IPropertyReflector pr, object? srce, object dest, ref bool hasChanged)
        {
            var curr = pr.PropertyExpression.GetValue(dest);
            if (pr.PropertyExpression.Compare(curr, srce))
                return;

            pr.PropertyExpression.SetValue(dest, srce);
            hasChanged = true;
        }

        /// <summary>
        /// Merge an <see cref="IDictionary"/>.
        /// </summary>
        private static IDictionary? MergeDictionary(JsonMergePatchOptions args, IPropertyReflector pr, string root, JsonElement json, IDictionary srce, IDictionary? dest, ref bool hasChanged)
        {
            var dict = dest;

            // Iterate through the properties, each is an item that will be added to the new dictionary.
            foreach (var jp in json.EnumerateObject())
            {
                var path = $"{root}.{jp.Name}";
                var srceitem = srce[jp.Name];

                if (srceitem == null)
                {
                    // A null value results in a remove operation.
                    if (dict != null && dict.Contains(jp.Name))
                    {
                        dict.Remove(jp.Name);
                        hasChanged = true;
                    }

                    continue;
                }

                // Create new destination dictionary where it does not exist already.
                dict ??= (IDictionary)pr.GetEntityReflector()!.CreateInstance();

                // Find the existing and merge; otherwise, add as-is.
                if (dict.Contains(jp.Name))
                {
                    var destitem = dict[jp.Name];
                    switch (pr.ItemTypeCode)
                    {
                        case TypeReflectorTypeCode.Simple:
                            if (!hasChanged && dict[jp.Name] != destitem)
                                hasChanged = true;

                            dict[jp.Name] = srceitem;
                            continue;

                        case TypeReflectorTypeCode.Complex:
                            MergeObject(args, pr.GetItemEntityReflector()!, path, jp.Value, srceitem, destitem, ref hasChanged);
                            dict[jp.Name] = destitem;
                            continue;

                        default:
                            throw new NotSupportedException("A merge where a dictionary value is an array or other collection type is not supported.");
                    }
                }
                else
                {
                    // Represents an add.
                    hasChanged = true;
                    dict[jp.Name] = srceitem;
                }
            }

            return dict;
        }

        /// <summary>
        /// Merge a <see cref="IKeyedCollection"/>.
        /// </summary>
        private static IKeyedCollection MergeKeyedCollection(JsonMergePatchOptions args, IPropertyReflector pr, string root, JsonElement json, IKeyedCollection srce, IKeyedCollection? dest, ref bool hasChanged)
        {
            if (srce!.IsAnyDuplicates())
                throw new JsonMergePatchException($"The JSON array must not contain items with duplicate '{nameof(IPrimaryKey)}' keys. Path: {root}");

            if (dest != null && dest.IsAnyDuplicates())
                throw new JsonMergePatchException($"The JSON array destination collection must not contain items with duplicate '{nameof(IPrimaryKey)}' keys prior to merge. Path: {root}");

            // Create new destination collection; add each to maintain sent order as this may be important to the consuming application.
            var coll = (IKeyedCollection)pr.GetEntityReflector()!.CreateInstance();

            // Iterate through the items and add to the new collection.
            var i = 0;
            IEntityReflector er = pr.GetItemEntityReflector()!;

            foreach (var ji in json.EnumerateArray())
            {
                var path = $"{root}[{i}]";
                if (ji.ValueKind != JsonValueKind.Object && ji.ValueKind != JsonValueKind.Null)
                    throw new JsonMergePatchException($"The JSON array item must be an Object where the destination collection supports keys. Path: {path}");

                var srceitem = (IPrimaryKey)srce[i++];

                // Find the existing and merge; otherwise, add as-is.
                var destitem = srceitem == null ? null : dest?.GetByKey(srceitem.PrimaryKey);
                if (destitem != null)
                {
                    MergeObject(args, er, path, ji, srceitem, destitem, ref hasChanged);
                    coll.Add(destitem);
                }
                else
                    coll.Add(srceitem!);
            }

            return coll;
        }
    }
}