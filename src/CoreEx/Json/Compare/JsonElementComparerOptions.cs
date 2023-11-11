// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;

namespace CoreEx.Json.Compare
{
    /// <summary>
    /// Provides the options for <see cref="JsonElementComparer"/>.
    /// </summary>
    public sealed class JsonElementComparerOptions
    {
        private static JsonElementComparerOptions? _default;

        /// <summary>
        /// Gets or sets the default <see cref="JsonElementComparer"/> instance.
        /// </summary>
        public static JsonElementComparerOptions Default
        {
            get => _default ??= new();
            set => _default = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets the <see cref="IEqualityComparer{String}"/> to use for comparing JSON paths.
        /// </summary>
        /// <remarks>Defaults to <see cref="StringComparer.OrdinalIgnoreCase"/>.</remarks>
        public IEqualityComparer<string> PathComparer { get; set; } = StringComparer.OrdinalIgnoreCase;

        /// <summary>
        /// Gets or sets the maximum number of differences to detect where performing a comparison.
        /// </summary>
        /// <remarks>Defaults to <see cref="int.MaxValue"/>.</remarks>
        public int MaxDifferences { get; set; } = int.MaxValue;

        /// <summary>
        /// Gets or sets the <see cref="JsonElementComparison"/> used for value comparisons.
        /// </summary>
        /// <remarks>Defauls to <see cref="JsonElementComparison.Semantic"/>.</remarks>
        public JsonElementComparison ValueComparison { get; set; } = JsonElementComparison.Semantic;

        /// <summary>
        /// Gets or sets the <see cref="IJsonSerializer"/>.
        /// </summary>
        /// <remarks>Defaults to <see cref="Json.JsonSerializer.Default"/>.</remarks>
        public IJsonSerializer JsonSerializer { get; set; } = Json.JsonSerializer.Default;

        /// <summary>
        /// Indicates whether to always replace all array items where at least one item has changed when performing a corresponding <see cref="JsonElementComparerResult.ToMergePatch(string[])"/>.
        /// </summary>
        /// <remarks>The formal specification <see href="https://tools.ietf.org/html/rfc7396"/> explictly states that an <see cref="System.Text.Json.JsonValueKind.Array"/> is to be a replacement operation.
        /// <para>Where set to <c>false</c> and there is an array length difference this will always result in a replace (i.e. all); no means to reliably determine what has been added, deleted, modified, resequenced, etc.</para></remarks>
        public bool AlwaysReplaceAllArrayItems { get; set; } = true;
    }
}