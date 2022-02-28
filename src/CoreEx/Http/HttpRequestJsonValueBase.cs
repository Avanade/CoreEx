// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.AspNetCore.Mvc;
using System;
using System.Text;

namespace CoreEx.Http
{
    /// <summary>
    /// Represents the base for a <see cref="HttpRequestJsonValue"/> and <see cref="HttpRequestJsonValue{T}"/>.
    /// </summary>
    public abstract class HttpRequestJsonValueBase
    {
        /// <summary>
        /// Indicates whether the request value was found to be valid.
        /// </summary>
        public bool IsValid => ValidationException == null;

        /// <summary>
        /// Indicates whether the request value was found to be invalid.
        /// </summary>
        public bool IsInvalid => !IsValid;

        /// <summary>
        /// Gets or sets any corresponding <see cref="CoreEx.ValidationException"/>.
        /// </summary>
        /// <remarks>This is typically set as the result of JSON deserialization.</remarks>
        public ValidationException? ValidationException { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"{nameof(IsValid)}: {IsValid}, Error: {ValidationException?.Message ?? "none"}");

            if (ValidationException is ValidationException vex && vex.ModelStateDictionary != null && vex.ModelStateDictionary.ErrorCount > 0)
            {
                sb.Append($", Errors: {vex.ModelStateDictionary.ErrorCount}");
                foreach (var kvp in vex.ModelStateDictionary)
                {
                    foreach (var e in kvp.Value.Errors)
                    {
                        sb.Append($"{Environment.NewLine}\t{kvp.Key}: {e.ErrorMessage}");
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Converts the <see cref="HttpRequestJsonValueBase"/> into a <see cref="BadRequestObjectResult"/>.
        /// </summary>
        /// <returns>The corresponding <see cref="BadRequestObjectResult"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when <see cref="HttpRequestJsonValueBase.IsValid"/>.</exception>
        public IActionResult ToBadRequestResult()
        {
            if (IsValid)
                throw new InvalidOperationException($"The request {nameof(IsValid)} and therefore can not be converted into a {nameof(BadRequestObjectResult)}.");

            return ValidationException!.ToResult();
        }
    }
}