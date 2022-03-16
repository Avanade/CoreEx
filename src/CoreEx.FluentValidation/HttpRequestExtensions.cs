// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Http;
using CoreEx.Json;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreEx.FluentValidation
{
    /// <summary>
    /// <c>FluentValidation</c> extension methods for <see cref="HttpRequest"/>.
    /// </summary>
    public static class HttpRequestExtensions
    {
        /// <summary>
        /// Deserialize the HTTP JSON <see cref="HttpRequest.Body"/> to a specified .NET object <see cref="Type"/> via a <see cref="HttpRequestJsonValue{T}"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValidator">The <typeparamref name="T"/> validator <see cref="Type"/>.</typeparam>
        /// <param name="httpRequest">The <see cref="HttpRequest"/>.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the value is required; will consider invalid where null.</param>
        /// <returns>The <see cref="HttpRequestJsonValue{T}"/>.</returns>
        public static Task<HttpRequestJsonValue<T>> ReadAsJsonValidatedValueAsync<T, TValidator>(this HttpRequest httpRequest, IJsonSerializer jsonSerializer, bool valueIsRequired = true)
            where TValidator : AbstractValidator<T>, new()
            => ReadAsJsonValidatedValueAsync<T, TValidator>(httpRequest, jsonSerializer, new TValidator(), valueIsRequired);

        /// <summary>
        /// Deserialize the HTTP JSON <see cref="HttpRequest.Body"/> to a specified .NET object <see cref="Type"/> via a <see cref="HttpRequestJsonValue{T}"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValidator">The <typeparamref name="T"/> validator <see cref="Type"/>.</typeparam>
        /// <param name="httpRequest">The <see cref="HttpRequest"/>.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
        /// <param name="validator">The <see cref="AbstractValidator{T}"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the value is required; will consider invalid where null.</param>
        /// <returns>The <see cref="HttpRequestJsonValue{T}"/>.</returns>
        public static async Task<HttpRequestJsonValue<T>> ReadAsJsonValidatedValueAsync<T, TValidator>(this HttpRequest httpRequest, IJsonSerializer jsonSerializer, TValidator validator, bool valueIsRequired = true)
            where TValidator : AbstractValidator<T>
        {
            var jv = await httpRequest.ReadAsJsonValueAsync<T>(jsonSerializer, valueIsRequired).ConfigureAwait(false);
            if (jv.IsInvalid)
                return jv;

            if (!valueIsRequired && Comparer<T>.Default.Compare(jv.Value, default!) == 0)
                return jv;

            var fvr = (validator ?? throw new ArgumentNullException(nameof(validator))).Validate(jv.Value);
            jv.ValidationException = fvr.CreateValidationException();
            return jv;
        }
    }
}