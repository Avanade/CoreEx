// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.AspNetCore.Http;
using CoreEx.Http;
using Microsoft.AspNetCore.Http;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Mime;
using UnitTestEx.Azure.Functions;
using Ceh = CoreEx.Http;

namespace UnitTestEx
{
    /// <summary>
    /// Provides extension methods to the core <see href="https://github.com/Avanade/unittestex"/>.
    /// </summary>
    public static class UnitTestExExtensions
    {
        #region FunctionTesterBase

        /// <summary>
        /// Creates a new <see cref="HttpRequest"/> with no body.
        /// </summary>
        /// <param name="tester">The tester.</param>
        /// <param name="httpMethod">The <see cref="HttpMethod"/>.</param>
        /// <param name="requestUri">The requuest uri.</param>
        /// <param name="requestOptions">The optional <see cref="Ceh.HttpRequestOptions"/>.</param>
        /// <returns>The <see cref="HttpRequest"/>.</returns>
#if NET7_0_OR_GREATER
        public static HttpRequest CreateHttpRequest<TEntryPoint, TSelf>(this FunctionTesterBase<TEntryPoint, TSelf> tester, HttpMethod httpMethod, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, Ceh.HttpRequestOptions? requestOptions = null)
#else
        public static HttpRequest CreateHttpRequest<TEntryPoint, TSelf>(this FunctionTesterBase<TEntryPoint, TSelf> tester, HttpMethod httpMethod, string? requestUri, Ceh.HttpRequestOptions? requestOptions = null)
#endif
            where TEntryPoint : class, new() where TSelf : FunctionTesterBase<TEntryPoint, TSelf>
            => tester.CreateHttpRequest(httpMethod, requestUri).ApplyRequestOptions(requestOptions);

        /// <summary>
        /// Creates a new <see cref="HttpRequest"/> with no body.
        /// </summary>
        /// <param name="tester">The tester.</param>
        /// <param name="httpMethod">The <see cref="HttpMethod"/>.</param>
        /// <param name="requestUri">The requuest uri.</param>
        /// <param name="requestOptions">The optional <see cref="Ceh.HttpRequestOptions"/>.</param>
        /// <param name="requestModifier">The optional <see cref="HttpRequest"/> modifier.</param>
        /// <returns>The <see cref="HttpRequest"/>.</returns>
#if NET7_0_OR_GREATER
        public static HttpRequest CreateHttpRequest<TEntryPoint, TSelf>(this FunctionTesterBase<TEntryPoint, TSelf> tester, HttpMethod httpMethod, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, Ceh.HttpRequestOptions? requestOptions = null, Action<HttpRequest>? requestModifier = null)
#else
        public static HttpRequest CreateHttpRequest<TEntryPoint, TSelf>(this FunctionTesterBase<TEntryPoint, TSelf> tester, HttpMethod httpMethod, string? requestUri, Ceh.HttpRequestOptions? requestOptions = null, Action<HttpRequest>? requestModifier = null)
#endif
            where TEntryPoint : class, new() where TSelf : FunctionTesterBase<TEntryPoint, TSelf>
            => tester.CreateHttpRequest(httpMethod, requestUri, requestModifier).ApplyRequestOptions(requestOptions);

        /// <summary>
        /// Creates a new <see cref="HttpRequest"/> with <paramref name="body"/> (defaults <see cref="HttpRequest.ContentType"/> to <see cref="MediaTypeNames.Text.Plain"/>).
        /// </summary>
        /// <param name="tester">The tester.</param>
        /// <param name="httpMethod">The <see cref="HttpMethod"/>.</param>
        /// <param name="requestUri">The requuest uri.</param>
        /// <param name="body">The optional body content.</param>
        /// <param name="requestOptions">The optional <see cref="Ceh.HttpRequestOptions"/>.</param>
        /// <returns>The <see cref="HttpRequest"/>.</returns>
#if NET7_0_OR_GREATER
        public static HttpRequest CreateHttpRequest<TEntryPoint, TSelf>(this FunctionTesterBase<TEntryPoint, TSelf> tester, HttpMethod httpMethod, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, string? body, Ceh.HttpRequestOptions? requestOptions = null)
#else
        public static HttpRequest CreateHttpRequest<TEntryPoint, TSelf>(this FunctionTesterBase<TEntryPoint, TSelf> tester, HttpMethod httpMethod, string? requestUri, string? body, Ceh.HttpRequestOptions? requestOptions = null)
#endif
            where TEntryPoint : class, new() where TSelf : FunctionTesterBase<TEntryPoint, TSelf>
            => tester.CreateHttpRequest(httpMethod, requestUri, body, null, null).ApplyRequestOptions(requestOptions);

        /// <summary>
        /// Creates a new <see cref="HttpRequest"/> with <paramref name="body"/> and <paramref name="contentType"/>.
        /// </summary>
        /// <param name="tester">The tester.</param>
        /// <param name="httpMethod">The <see cref="HttpMethod"/>.</param>
        /// <param name="requestUri">The requuest uri.</param>
        /// <param name="body">The optional body content.</param>
        /// <param name="contentType">The content type. Defaults to <see cref="MediaTypeNames.Text.Plain"/>.</param>
        /// <param name="requestOptions">The optional <see cref="Ceh.HttpRequestOptions"/>.</param>
        /// <returns>The <see cref="HttpRequest"/>.</returns>
#if NET7_0_OR_GREATER
        public static HttpRequest CreateHttpRequest<TEntryPoint, TSelf>(this FunctionTesterBase<TEntryPoint, TSelf> tester, HttpMethod httpMethod, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, string? body, string? contentType, Ceh.HttpRequestOptions? requestOptions = null)
#else
        public static HttpRequest CreateHttpRequest<TEntryPoint, TSelf>(this FunctionTesterBase<TEntryPoint, TSelf> tester, HttpMethod httpMethod, string? requestUri, string? body, string? contentType, Ceh.HttpRequestOptions? requestOptions = null)
#endif
            where TEntryPoint : class, new() where TSelf : FunctionTesterBase<TEntryPoint, TSelf>
            => tester.CreateHttpRequest(httpMethod, requestUri, body, contentType, null).ApplyRequestOptions(requestOptions);

        /// <summary>
        /// Creates a new <see cref="HttpRequest"/> with the <paramref name="value"/> JSON serialized as <see cref="HttpRequest.ContentType"/> of <see cref="MediaTypeNames.Application.Json"/>.
        /// </summary>
        /// <param name="tester">The tester.</param>
        /// <param name="httpMethod">The <see cref="HttpMethod"/>.</param>
        /// <param name="requestUri">The requuest uri.</param>
        /// <param name="value">The value to JSON serialize.</param>
        /// <param name="requestOptions">The optional <see cref="Ceh.HttpRequestOptions"/> modifier.</param>
        /// <returns>The <see cref="HttpRequest"/>.</returns>
#if NET7_0_OR_GREATER
        public static HttpRequest CreateJsonHttpRequest<TEntryPoint, TSelf>(this FunctionTesterBase<TEntryPoint, TSelf> tester, HttpMethod httpMethod, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, object? value, Ceh.HttpRequestOptions? requestOptions)
#else
        public static HttpRequest CreateJsonHttpRequest<TEntryPoint, TSelf>(this FunctionTesterBase<TEntryPoint, TSelf> tester, HttpMethod httpMethod, string? requestUri, object? value, Ceh.HttpRequestOptions? requestOptions)
#endif
            where TEntryPoint : class, new() where TSelf : FunctionTesterBase<TEntryPoint, TSelf>
            => tester.CreateJsonHttpRequest(httpMethod, requestUri, value).ApplyRequestOptions(requestOptions);

        /// <summary>
        /// Creates a new <see cref="HttpRequest"/> with the <paramref name="value"/> JSON serialized as <see cref="HttpRequest.ContentType"/> of <see cref="MediaTypeNames.Application.Json"/>.
        /// </summary>
        /// <param name="tester">The tester.</param>
        /// <param name="httpMethod">The <see cref="HttpMethod"/>.</param>
        /// <param name="requestUri">The requuest uri.</param>
        /// <param name="value">The value to JSON serialize.</param>
        /// <param name="requestOptions">The optional <see cref="Ceh.HttpRequestOptions"/> modifier.</param>
        /// <param name="requestModifier">The optional <see cref="HttpRequest"/> modifier.</param>
        /// <returns>The <see cref="HttpRequest"/>.</returns>
#if NET7_0_OR_GREATER
        public static HttpRequest CreateJsonHttpRequest<TEntryPoint, TSelf>(this FunctionTesterBase<TEntryPoint, TSelf> tester, HttpMethod httpMethod, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, object? value, Ceh.HttpRequestOptions? requestOptions, Action<HttpRequest>? requestModifier = null)
#else
        public static HttpRequest CreateJsonHttpRequest<TEntryPoint, TSelf>(this FunctionTesterBase<TEntryPoint, TSelf> tester, HttpMethod httpMethod, string? requestUri, object? value, Ceh.HttpRequestOptions? requestOptions, Action<HttpRequest>? requestModifier = null)
#endif
            where TEntryPoint : class, new() where TSelf : FunctionTesterBase<TEntryPoint, TSelf>
            => tester.CreateJsonHttpRequest(httpMethod, requestUri, value, requestModifier).ApplyRequestOptions(requestOptions);

        #endregion
    }
}