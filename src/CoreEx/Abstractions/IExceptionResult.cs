// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.AspNetCore.Mvc;
using System;

namespace CoreEx.Abstractions
{
    /// <summary>
    /// Enables the conversion of an <see cref="Exception"/> into a corresponding <see cref="IActionResult"/>.
    /// </summary>
    public interface IExceptionResult
    {
        /// <summary>
        /// Converts the <see cref="Exception"/> into a corresponding <see cref="IActionResult"/>.
        /// </summary>
        /// <returns>The corresponding <see cref="IActionResult"/>.</returns>
        IActionResult ToResult();
    }
}