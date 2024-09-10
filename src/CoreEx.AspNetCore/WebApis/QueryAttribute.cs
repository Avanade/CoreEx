// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;

namespace CoreEx.AspNetCore.WebApis
{
    /// <summary>
    /// An attribute that specifies that the action/operation supports <see cref="QueryArgs"/> (not explicitly defined as a parameter).
    /// </summary>
    /// <remarks>The is used to enable <i>Swagger/Swashbuckle</i> generated documentation where the operation does not explicitly define the <see cref="QueryArgs"/> as a method parameter.</remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class QueryAttribute : Attribute { }
}