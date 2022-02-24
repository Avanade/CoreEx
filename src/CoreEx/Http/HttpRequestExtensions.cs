// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Json;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CoreEx.Http
{
    /// <summary>
    /// Extension methods for <see cref="HttpRequest"/>.
    /// </summary>
    public static class HttpRequestExtensions
    {
        private const string _errorText = "Invalid request: content was not provided, contained invalid JSON, or was incorrectly formatted:";

        /// <summary>
        /// Deserialize the HTTP JSON <see cref="HttpRequest.Body"/> to a specified .NET object <see cref="Type"/> via a <see cref="HttpRequestJsonValue{T}"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="httpRequest">The <see cref="HttpRequest"/>.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the value is required; will consider invalid where null.</param>
        /// <returns>The <see cref="HttpRequestJsonValue{T}"/>.</returns>
        public static async Task<HttpRequestJsonValue<T>> ReadAsJsonValueAsync<T>(this HttpRequest httpRequest, IJsonSerializer jsonSerializer, bool valueIsRequired = true)
        {
            // Do not close/dispose StreamReader as that will close underlying stream which may cause a further downstream exception.
            var sr = new StreamReader((httpRequest ?? throw new ArgumentNullException(nameof(httpRequest))).Body);
            var json = await sr.ReadToEndAsync();
            var jv = new HttpRequestJsonValue<T>();

            // Deserialize the JSON into the selected type.
            try
            {
                if (!string.IsNullOrEmpty(json))
                    jv.Value = (jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer))).Deserialize<T>(json)!;

                if (valueIsRequired && jv.Value == null)
                    jv.ValidationException = new ValidationException($"{_errorText} Value is mandatory.");
            }
            catch (Exception ex)
            {
                jv.ValidationException = new ValidationException($"{_errorText} {ex.Message}", ex);
            }

            return jv;
        }

        /// <summary>
        /// Deserialize the HTTP JSON <see cref="HttpRequest.Body"/> to a specified .NET object <see cref="Type"/> via a <see cref="HttpRequestJsonValue"/>.
        /// </summary>
        /// <param name="httpRequest">The <see cref="HttpRequest"/>.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the value is required; will consider invalid where null.</param>
        /// <returns>The <see cref="HttpRequestJsonValue"/>.</returns>
        public static async Task<HttpRequestJsonValue> ReadAsJsonValueAsync(this HttpRequest httpRequest, IJsonSerializer jsonSerializer, bool valueIsRequired = true)
        {
            // Do not close/dispose StreamReader as that will close underlying stream which may cause a further exception.
            var sr = new StreamReader((httpRequest ?? throw new ArgumentNullException(nameof(httpRequest))).Body);
            var json = await sr.ReadToEndAsync();
            var jv = new HttpRequestJsonValue();

            // Deserialize the JSON into the selected type.
            try
            {
                jv.Value = (jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer))).Deserialize(json)!;
                if (valueIsRequired && jv.Value == null)
                    jv.ValidationException = new ValidationException($"{_errorText} Value is mandatory.");
            }
            catch (Exception ex)
            {
                jv.ValidationException = new ValidationException($"{_errorText} {ex.Message}", ex);
            }

            return jv;
        }
    }
}