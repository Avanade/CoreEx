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
        private const string PKCollectionName = "PKColl";
        private readonly TypeReflectorArgs _trArgs;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonMergePatch"/> class.
        /// </summary>
        /// <param name="options">The <see cref="JsonMergePatchOptions"/>.</param>
        /// <remarks>This object should be reused where possible as it caches the JSON serialization semantics internally to improve performance. It is also thread-safe.</remarks>
        public JsonMergePatch(JsonMergePatchOptions? options = null)
        {
            Options = options ?? new JsonMergePatchOptions();

            _trArgs = new TypeReflectorArgs(Options.JsonSerializer)
            {
                AutoPopulateProperties = true,
                NameComparer = Options.NameComparer,
                TypeBuilder = tr =>
                {
                    // Determine if type implements IPrimaryKeyCollection.
                    if (tr.Type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPrimaryKeyCollection<>)))
                        tr.Data.Add(PKCollectionName, true);
                },
                PropertyBuilder = pr => pr.PropertyExpression.IsJsonSerializable // Only interested in properties that are considered serializable.
            };
        }

        /// <summary>
        /// Gets the <see cref="JsonMergePatchOptions"/>.
        /// </summary>
        public JsonMergePatchOptions Options { get; set; }

        /// <inheritdoc/>
        public bool Merge<T>(string json, ref T? value)
        {
            if (json == null)
                throw new ArgumentNullException(nameof(json));

            // Parse the JSON.
            var j = ParseJson<T>(json);

            // Perform the root merge patch.
            return MergeRoot(j.JsonElement, j.Value, ref value);
        }

        /// <inheritdoc/>
        public async Task<(bool HasChanges, T? Value)> MergeAsync<T>(string json, Func<T?, Task<T?>> getValue)
        {
            if (json == null)
                throw new ArgumentNullException(nameof(json));

            if (getValue == null)
                throw new ArgumentNullException(nameof(getValue));

            // Parse the JSON.
            var j = ParseJson<T>(json); 

            // Get the value.
            T? value = await getValue(j.Value).ConfigureAwait(false);
            if (value == null)
                return (false, default!);

            // Perform the root merge patch.
            return (MergeRoot(j.JsonElement, j.Value, ref value), value);
        }

        /// <summary>
        /// Performs the merge patch.
        /// </summary>
        private bool MergeRoot<T>(JsonElement json, T? srce, ref T? dest)
        {
            bool hasChanged = false;

            var tr = TypeReflector.GetReflector<T>(_trArgs);

            switch (json.ValueKind)
            {
                case JsonValueKind.Null:
                case JsonValueKind.True:
                case JsonValueKind.False:
                case JsonValueKind.String:
                case JsonValueKind.Number:
                    hasChanged = !tr.Compare(srce, dest);
                    dest = srce;
                    break;

                case JsonValueKind.Object:
                    if (tr.TypeCode == TypeReflectorTypeCode.IDictionary)
                    {
                        // Where merging into a dictionary this can be a replace or per item merge.
                        if (Options.DictionaryMergeApproach == DictionaryMergeApproach.Replace)
                        {
                            hasChanged = !tr.Compare(dest, srce);
                            if (hasChanged)
                                dest = srce;
                        }
                        else
                            dest = (T)MergeDictionary(tr, "$", json, (IDictionary)srce!, (IDictionary)dest!, ref hasChanged)!;
                    }
                    else
                    {
                        if (srce == null || dest == null)
                        {
                            hasChanged = !tr.Compare(dest, srce);
                            if (hasChanged)
                                dest = srce;
                        }
                        else
                            MergeObject(tr, "$", json, srce, dest!, ref hasChanged);
                    }

                    break;

                case JsonValueKind.Array:
                    // Unless explicitly requested an array is a full replacement only (source copy); otherwise, perform key collection item merge.
                    if (Options.PrimaryKeyCollectionMergeApproach != PrimaryKeyCollectionMergeApproach.Replace && tr.Data.ContainsKey(PKCollectionName))
                        dest = (T)MergeKeyedCollection(tr, "$", json, (IKeyedCollection)srce!, (IKeyedCollection)dest!, ref hasChanged);
                    else
                    {
                        hasChanged = !tr.Compare(dest, srce);
                        if (hasChanged)
                            dest = srce;
                    }

                    break;

                default: 
                    throw new InvalidOperationException($"A JSON element of '{json.ValueKind}' is invalid where merging the root.");
            }

            return hasChanged;
        }

        /// <summary>
        /// Parses the JSON.
        /// </summary>
        private (JsonElement JsonElement, T? Value) ParseJson<T>(string json)
        {
            try
            {
                // Deserialize into a temporary value which will be used as the merge source.
                var value = Options.JsonSerializer.Deserialize<T>(json);

                // Parse the JSON into a JsonElement which will be used to navigate the merge.
                var jr = new Utf8JsonReader(new BinaryData(json));
                var je = JsonElement.ParseValue(ref jr);
                return (je, value);
            }
            catch (JsonException jex)
            {
                throw new JsonMergePatchException(jex.Message, jex);
            }
        }

        /// <summary>
        /// Merge the object.
        /// </summary>
        private void MergeObject(ITypeReflector tr, string root, JsonElement json, object? srce, object dest, ref bool hasChanged)
        {
            foreach (var jp in json.EnumerateObject())
            {
                // Find the named property; skip when not found.
                var pr = tr.GetJsonProperty(jp.Name);
                if (pr == null)
                    continue;

                MergeProperty(pr, $"{root}.{jp.Name}", jp, srce, dest, ref hasChanged);
            }
        }

        /// <summary>
        /// Merge the property.
        /// </summary>
        private void MergeProperty(IPropertyReflector pr, string path, JsonProperty json, object? srce, object dest, ref bool hasChanged)
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
                            // Where the merging into a dictionary this can be a replace or per item merge.
                            if (Options.DictionaryMergeApproach == DictionaryMergeApproach.Replace)
                                SetPropertyValue(pr, pr.PropertyExpression.GetValue(srce)!, dest, ref hasChanged);
                            else
                            {
                                var dict = MergeDictionary(pr.GetTypeReflector()!, path, json.Value, (IDictionary)pr.PropertyExpression.GetValue(srce)!, (IDictionary)pr.PropertyExpression.GetValue(dest)!, ref hasChanged);
                                SetPropertyValue(pr, dict, dest, ref hasChanged);
                            }
                        }
                        else
                            MergeObject(pr.GetTypeReflector()!, path, json.Value, pr.PropertyExpression.GetValue(srce), current, ref hasChanged);
                    }

                    break;

                case JsonValueKind.Array:
                    // Unless explicitly requested an array is a full replacement only (source copy); otherwise, perform key collection item merge.
                    var tr = pr.GetTypeReflector()!;
                    if (Options.PrimaryKeyCollectionMergeApproach != PrimaryKeyCollectionMergeApproach.Replace && tr.Data.ContainsKey(PKCollectionName))
                    {
                        var coll = MergeKeyedCollection(tr, path, json.Value, (IKeyedCollection)pr.PropertyExpression.GetValue(srce)!, (IKeyedCollection)pr.PropertyExpression.GetValue(dest)!, ref hasChanged);
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
            if (pr.Compare(curr, srce))
                return;

            pr.PropertyExpression.SetValue(dest, srce);
            hasChanged = true;
        }

        /// <summary>
        /// Merge an <see cref="IDictionary"/>.
        /// </summary>
        private IDictionary? MergeDictionary(ITypeReflector tr, string root, JsonElement json, IDictionary srce, IDictionary? dest, ref bool hasChanged)
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
                dict ??= (IDictionary)tr.CreateInstance();

                // Find the existing and merge; otherwise, add as-is.
                if (dict.Contains(jp.Name))
                {
                    var destitem = dict[jp.Name];
                    switch (tr.ItemTypeCode)
                    {
                        case TypeReflectorTypeCode.Simple:
                            if (!hasChanged && !tr.GetItemTypeReflector()!.Compare(dict[jp.Name], destitem))
                                hasChanged = true;

                            dict[jp.Name] = srceitem;
                            continue;

                        case TypeReflectorTypeCode.Complex:
                            MergeObject(tr.GetItemTypeReflector()!, path, jp.Value, srceitem, destitem, ref hasChanged);
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
        private IKeyedCollection MergeKeyedCollection(ITypeReflector tr, string root, JsonElement json, IKeyedCollection srce, IKeyedCollection? dest, ref bool hasChanged)
        {
            if (srce!.IsAnyDuplicates())
                throw new JsonMergePatchException($"The JSON array must not contain items with duplicate '{nameof(IPrimaryKey)}' keys. Path: {root}");

            if (dest != null && dest.IsAnyDuplicates())
                throw new JsonMergePatchException($"The JSON array destination collection must not contain items with duplicate '{nameof(IPrimaryKey)}' keys prior to merge. Path: {root}");

            // Create new destination collection; add each to maintain sent order as this may be important to the consuming application.
            var coll = (IKeyedCollection)tr.CreateInstance();

            // Iterate through the items and add to the new collection.
            var i = 0;
            ITypeReflector ier = tr.GetItemTypeReflector()!;

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
                    MergeObject(ier, path, ji, srceitem, destitem, ref hasChanged);
                    coll.Add(destitem);
                }
                else
                    coll.Add(srceitem!);
            }

            return coll;
        }
    }
}