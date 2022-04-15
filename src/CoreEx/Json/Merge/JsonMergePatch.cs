// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions.Reflection;
using CoreEx.Entities;
using System;
using System.Collections;
using System.Linq;
using System.Text.Json;

namespace CoreEx.Json.Merge
{
    /// <summary>
    /// Enables a JSON Merge Patch (<c>application/merge-patch+json</c>) whereby the contents of a JSON document are merged into an existing object value as per <see href="https://tools.ietf.org/html/rfc7396"/>.
    /// </summary>
    /// <remarks><para>This object should be reused where possible as it caches the JSON serialization semantics internally to improve performance. It is also thread-safe.</para>
    /// <para>Additional logic has been added to the merge patch enable by <see cref="JsonMergePatchOptions.UseKeyMergeForDictionaries"/> and <see cref="JsonMergePatchOptions.UseKeyMergeForPrimaryKeyCollections"/>. Note: these capabilities are
    /// unique to <i>CoreEx</i> and not part of the formal specification <see href="https://tools.ietf.org/html/rfc7396"/>.</para></remarks>
    public class JsonMergePatch
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
            Args = args ?? new JsonMergePatchOptions();

            _erArgs = new EntityReflectorArgs(Args.JsonSerializer)
            {
                AutoPopulateProperties = true,
                NameComparer = Args.NameComparer,
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
        public JsonMergePatchOptions Args { get; set; }

        /// <summary>
        /// Merges the JSON <see cref="string"/> content into the <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <param name="json">The <see cref="JsonElement"/> to merge.</param>
        /// <param name="value">The entity value to merge into.</param>
        /// <returns><c>true</c> indicates that changes were made to the <paramref name="value"/> as a result of the merge; otherwise, <c>false</c> for no changes.</returns>
        public bool Merge<TEntity>(string json, TEntity value) where TEntity : class, new()
        {
            if (json == null)
                throw new ArgumentNullException(nameof(json));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            TEntity? srce;
            JsonElement je;

            try
            {
                // Deserialize into a temporary value which will be used as the merge source.
                srce = Args.JsonSerializer.Deserialize<TEntity>(json);
                if (srce == null)
                    throw new JsonMergePatchException($"The JSON is malformed and could not be parsed.");

                // Parse the JSON into a JsonElement which will be used to navigate the merge.
                var jr = new Utf8JsonReader(new BinaryData(json));
                je = JsonElement.ParseValue(ref jr);
            }
            catch (JsonException jex)
            {
                throw new JsonMergePatchException(jex.Message, jex);
            }

            // Perform the object merge itself.
            bool hasChanged = false;
            MergeObject(Args, EntityReflector.GetReflector<TEntity>(_erArgs), "$", je, srce, value, ref hasChanged);
            return hasChanged;
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

                var path = $"{root}.{jp.Name}";

                // Update according to the value kind.
                switch (jp.Value.ValueKind)
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
                        var current = pr.PropertyInfo.GetValue(dest);
                        if (current == null)
                            SetPropertyValue(pr, pr.PropertyExpression.GetValue(srce)!, dest, ref hasChanged);
                        else
                        {
                            if (pr.TypeCode == TypeReflectorTypeCode.IDictionary)
                            {
                                if (pr.ItemTypeCode == TypeReflectorTypeCode.Complex && args.UseKeyMergeForDictionaries)
                                {
                                    var dict = MergeDictionary(args, pr, path, jp.Value, (IDictionary)pr.PropertyExpression.GetValue(srce)!, (IDictionary)pr.PropertyExpression.GetValue(dest)!, ref hasChanged);
                                    SetPropertyValue(pr, dict, dest, ref hasChanged);
                                }
                                else
                                    SetPropertyValue(pr, pr.PropertyInfo.GetValue(srce)!, dest, ref hasChanged);
                            }
                            else
                                MergeObject(args, pr.GetEntityReflector()!, path, jp.Value, pr.PropertyExpression.GetValue(srce), current, ref hasChanged);
                        }

                        break;

                    case JsonValueKind.Array:
                        // Unless explicitly requested an array is a full replacement only (source copy); otherwise, perform key collection item merge.
                        if (args.UseKeyMergeForPrimaryKeyCollections && pr.Data.ContainsKey(PKCollectionName))
                        {
                            var coll = MergeKeyedCollection(args, pr, path, jp.Value, (IKeyedCollection)pr.PropertyExpression.GetValue(srce)!, (IKeyedCollection)pr.PropertyExpression.GetValue(dest)!, ref hasChanged);
                            SetPropertyValue(pr, coll, dest, ref hasChanged);
                        }
                        else
                            SetPropertyValue(pr, pr.PropertyInfo.GetValue(srce)!, dest, ref hasChanged);

                        break;
                }
            }
        }

        /// <summary>
        /// Sets the property value.
        /// </summary>
        private static void SetPropertyValue(IPropertyReflector pr, object srce, object dest, ref bool hasChanged)
        {
            var curr = pr.PropertyExpression.GetValue(dest);
            if (pr.PropertyExpression.Compare(curr, srce))
                return;

            pr.PropertyExpression.SetValue(dest, srce);
            hasChanged = true;
        }

        /// <summary>
        /// Advanced item merge for <see cref="IKeyedCollection"/>.
        /// </summary>
        private static IKeyedCollection MergeKeyedCollection(JsonMergePatchOptions args, IPropertyReflector pr, string root, JsonElement json, IKeyedCollection srce, IKeyedCollection? dest, ref bool hasChanged)
        {
            if (srce!.IsAnyDuplicates())
                throw new JsonMergePatchException($"The JSON array must not contain items with duplicate keys. Path: {root}");

            if (dest != null && dest.IsAnyDuplicates())
                throw new JsonMergePatchException($"The JSON array destination collection must not contain items with duplicate keys prior to merge. Path: {root}");

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

        /// <summary>
        /// Advanced item merge for <see cref="IDictionary"/>.
        /// </summary>
        private static IDictionary MergeDictionary(JsonMergePatchOptions args, IPropertyReflector pr, string root, JsonElement json, IDictionary srce, IDictionary? dest, ref bool hasChanged)
        {
            // Create new destination dictionary; add each to which will simulate add/update/delete outcome.
            var dict = (IDictionary)pr.GetEntityReflector()!.CreateInstance();

            // Iterate through the properties, each is an item that will be added to the new dictionary.
            foreach (var jp in json.EnumerateObject())
            {
                var path = $"{root}.{jp.Name}";
                var srceitem = srce[jp.Name];

                // Find the existing and merge; otherwise, add as-is.
                if (dest != null && dest.Contains(jp.Name))
                {
                    var destitem = dest[jp.Name];
                    MergeObject(args, pr.GetItemEntityReflector()!, path, jp.Value, srceitem, destitem, ref hasChanged);
                    dict[jp.Name] = destitem;
                }
                else
                    dict[jp.Name] = srceitem;
            }

            return dict;
        }
    }
}