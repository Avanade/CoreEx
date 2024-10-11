// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CoreEx.AspNetCore.WebApis
{
    /// <summary>
    /// Extends an <see cref="IActionResult"/> to enable customization of the resulting <see cref="HttpResponse"/> using the <see cref="BeforeExtension"/> and <see cref="AfterExtension"/> functions.
    /// </summary>
    public interface IExtendedActionResult : IActionResult
    {
        /// <summary>
        /// Gets or sets the function to perform the extended <see cref="HttpResponse"/> customization.
        /// </summary>
        [JsonIgnore]
        Func<HttpResponse, Task>? BeforeExtension { get; set; }

        /// <summary>
        /// Gets or sets the function to perform the extended <see cref="HttpResponse"/> customization.
        /// </summary>
        [JsonIgnore]
        Func<HttpResponse, Task>? AfterExtension { get; set; }
    }
}