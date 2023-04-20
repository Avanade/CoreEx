// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Net.Mime;

namespace CoreEx.WebApis
{
    /// <summary>
    /// An attribute that specifies the expected request <b>body</b> <see cref="Type"/> that the action/operation accepts and the supported request content types.
    /// </summary>
    /// <remarks>The is used to enable <i>Swagger/Swashbuckle</i> generated documentation where the operation does not explicitly define the body as a method parameter; i.e. via <see cref="Microsoft.AspNetCore.Mvc.FromBodyAttribute"/>.</remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class AcceptsBodyAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AcceptsBodyAttribute"/> class.
        /// </summary>
        /// <param name="type">The <b>body</b> <see cref="Type"/>.</param>
        /// <param name="contentTypes">The <b>body</b> content type(s). Defaults to <see cref="MediaTypeNames.Application.Json"/>.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public AcceptsBodyAttribute(Type type, params string[] contentTypes)
        {
            BodyType = type ?? throw new ArgumentNullException(nameof(type));
            ContentTypes = contentTypes.Length == 0 ? new string[] { MediaTypeNames.Application.Json } : contentTypes;
        }

        /// <summary>
        /// Gets the <b>body</b> <see cref="Type"/>.
        /// </summary>
        public Type BodyType { get; }

        /// <summary>
        /// Gets the <b>body</b> content type(s).
        /// </summary>
        public string[] ContentTypes { get; }
    }
}