// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System.Collections;

namespace CoreEx.Json.Merge
{
    /// <summary>
    /// Defines the approach for a collection (see <see cref="ICollection"/>) that implements <see cref="IPrimaryKeyCollection{T}"/>; being either <see cref="Replace"/> or <see cref="Merge"/>.
    /// </summary>
    /// <remarks>The formal specification <see href="https://tools.ietf.org/html/rfc7396"/> explictlys state that an <see cref="System.Text.Json.JsonValueKind.Array"/> is to be a replacement operation. However, a <see cref="Merge"/> approach
    /// can be advantageous as it simplifies the manipulation of an <see cref="System.Text.Json.JsonValueKind.Array"/> without having to provide all content within.</remarks>
    public enum PrimaryKeyCollectionMergeApproach
    {
        /// <summary>
        /// Indicates that the collection (see <see cref="IPrimaryKeyCollection{T}"/>) merge will be treated the same as any <see cref="System.Text.Json.JsonValueKind.Array"/> where the result is a replacement (overwrite) operation.
        /// </summary>
        Replace,

        /// <summary>
        /// Indicates that the collection (see <see cref="IPrimaryKeyCollection{T}"/>) merge will be managed by the checking of the <see cref="IPrimaryKey.PrimaryKey"/> for all items. Where the merge JSON does not provide a matching
        /// <see cref="IPrimaryKey.PrimaryKey"/> it will be removed from the resulting collection. Where the merging <see cref="IPrimaryKey.PrimaryKey"/> already exists then a standard value merge will result (not a replacement); otherwise,
        /// where the key does not exist, a resulting add will result.
        /// </summary>
        /// <remarks>Note: this is unique to <i>CoreEx</i> and not part of the formal specification <see href="https://tools.ietf.org/html/rfc7396"/>. However, this approach can be advantageous as it simplifies the manipulation of an
        /// <see cref="System.Text.Json.JsonValueKind.Array"/> without having to provide all content within.</remarks>
        Merge
    }
}