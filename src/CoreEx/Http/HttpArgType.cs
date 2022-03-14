// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Http
{
    /// <summary>
    /// Defines the <see cref="HttpArg{T}.ArgType"/>.
    /// </summary>
    public enum HttpArgType
    {
        /// <summary>
        /// Indicates the argument should be passed in the body.
        /// </summary>
        FromBody,

        /// <summary>
        /// Indicates the argument should be passed as part of the URI query string.
        /// </summary>
        FromUri,

        /// <summary>
        /// Indicates the properties of the argument should be passed as part of the URI query string with no prefix.
        /// </summary>
        FromUriUseProperties,

        /// <summary>
        /// Indicates the properties of the argument should be passed as part of the URI query string with a prefix.
        /// </summary>
        FromUriUsePropertiesAndPrefix
    }
}