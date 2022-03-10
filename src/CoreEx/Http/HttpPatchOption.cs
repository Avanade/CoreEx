// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Net.Http;

namespace CoreEx.Http
{
    /// <summary>
    /// Specifies the <see cref="HttpMethod.Patch"/> option.
    /// </summary>
    public enum HttpPatchOption
    {
        /// <summary>
        /// Indicates that no valid patch option has been specified.
        /// </summary>
        NotSpecified,

        /// <summary>
        /// Indicates a <b>json-patch</b>. Requires a Content-Type of 'application/json-patch+json'. See https://tools.ietf.org/html/rfc6902 for more details.
        /// </summary>
        JsonPatch,

        /// <summary>
        /// Indicates a <b>merge-patch</b>. Requires a Content-Type of 'application/merge-patch+json'. See https://tools.ietf.org/html/rfc7396 for more details.
        /// </summary>
        MergePatch
    }
}