// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoreEx.Entities;
using CoreEx.Json;
using Microsoft.AspNetCore.Http;

namespace CoreEx.Http
{
    /// <summary>
    /// Represents a typed <see cref="HttpClient"/> foundation wrapper.
    /// </summary>
    public abstract class TypedHttpClientBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TypedHttpClientBase{TBase}"/>.
        /// </summary>
        /// <param name="client">The underlying <see cref="HttpClient"/>.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>. Defaults to <see cref="Json.JsonSerializer.Default"/>..</param>
        public TypedHttpClientBase(HttpClient client, IJsonSerializer? jsonSerializer = null)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
            JsonSerializer = jsonSerializer ?? CoreEx.Json.JsonSerializer.Default;
        }

        /// <summary>
        /// Gets the underlying <see cref="HttpClient"/>.
        /// </summary>
        protected HttpClient Client { get; }

        /// <summary>
        /// Gets the Base Address of the client
        /// </summary>
        public Uri? BaseAddress => Client?.BaseAddress;

        /// <summary>
        /// Gets the <see cref="IJsonSerializer"/>.
        /// </summary>
        protected IJsonSerializer JsonSerializer { get; }

        /// <summary>
        /// Create an <see cref="HttpRequestMessage"/> with no specified content.
        /// </summary>
        /// <param name="method">The <see cref="HttpMethod"/>.</param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="IHttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpRequestMessage"/>.</returns>
        protected Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string requestUri, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
            => CreateRequestInternalAsync(method, requestUri, null, requestOptions, args, cancellationToken);

        /// <summary>
        /// Create an <see cref="HttpRequestMessage"/> with the specified <paramref name="content"/>.
        /// </summary>
        /// <param name="method">The <see cref="HttpMethod"/>.</param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="content">The <see cref="HttpContent"/>.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="IHttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpRequestMessage"/>.</returns>
        protected Task<HttpRequestMessage> CreateContentRequestAsync(HttpMethod method, string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
            => CreateRequestInternalAsync(method, requestUri, content, requestOptions, args, cancellationToken);

        /// <summary>
        /// Create an <see cref="HttpRequestMessage"/> serializing the <paramref name="value"/> as JSON content.
        /// </summary>
        /// <typeparam name="TReq">The request <see cref="Type"/>.</typeparam>
        /// <param name="method">The <see cref="HttpMethod"/>.</param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="value">The request value to be serialized to JSON.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="IHttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpRequestMessage"/>.</returns>
        protected Task<HttpRequestMessage> CreateJsonRequestAsync<TReq>(HttpMethod method, string requestUri, TReq value, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
            => CreateContentRequestAsync(method, requestUri, new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, MediaTypeNames.Application.Json), ApplyValueETagToRequestOptions(value, requestOptions), args, cancellationToken);

        /// <summary>
        /// Applies the <paramref name="value"/> <see cref="IETag"/> to the <see cref="HttpRequestOptions"/> where not already specified.
        /// </summary>
        private static HttpRequestOptions? ApplyValueETagToRequestOptions<TReq>(TReq value, HttpRequestOptions? requestOptions = null)
        {
            if (value == null || (requestOptions != null && requestOptions.ETag != null))
                return requestOptions;

            if (value is IETag et && et.ETag != null)
            {
                if (requestOptions == null)
                    return new HttpRequestOptions { ETag = et.ETag };

                requestOptions.ETag = et.ETag;
            }

            return requestOptions;
        }

        /// <summary>
        /// Create the request applying the specified options and args.
        /// </summary>
        private async Task<HttpRequestMessage> CreateRequestInternalAsync(HttpMethod method, string requestUri, HttpContent? content, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
        {
            // Replace any format placeholders within request uri.
            requestUri = FormatReplacement(requestUri, args);

            // Access the query string.
            var uri = new Uri(requestUri, UriKind.RelativeOrAbsolute);

            var ub = new UriBuilder(uri.IsAbsoluteUri ? uri : new Uri(new Uri("https://coreex"), requestUri));
            var qs = QueryString.FromUriComponent(ub.Query);

            // Extend the query string from the IHttpArgs.
            foreach (var arg in (args ??= Array.Empty<IHttpArg>()).Where(x => x != null))
            {
                qs = arg.AddToQueryString(qs, JsonSerializer);
            }

            // Extend the query string to include additional options.
            if (requestOptions != null)
                qs = requestOptions.AddToQueryString(qs);

            // Create the request and include ETag if any.
            ub.Query = qs.ToUriComponent();
            var request = new HttpRequestMessage(method, uri.IsAbsoluteUri ? ub.Uri.ToString() : ub.Uri.PathAndQuery).ApplyETag(requestOptions?.ETag);
            if (content != null)
                request.Content = content;

            // Apply the body/content IHttpArg.
            foreach (var arg in args.Where(x => x != null))
            {
                await arg.ModifyHttpRequestAsync(request, JsonSerializer, cancellationToken).ConfigureAwait(false);
            }

            return request;
        }

        /// <summary>
        /// Format replacement inspired by: https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Logging.Abstractions/src/LogValuesFormatter.cs
        /// </summary>
        private static string FormatReplacement(string requestUri, IEnumerable<IHttpArg>? args)
        {
            var sb = new StringBuilder();
            var scanIndex = 0;
            var endIndex = requestUri.Length;

            while (scanIndex < endIndex)
            {
                var openBraceIndex = FindBraceIndex(requestUri, '{', scanIndex, endIndex);
                if (scanIndex == 0 && openBraceIndex == endIndex)
                    return requestUri;  // No holes found.

                var closeBraceIndex = FindBraceIndex(requestUri, '}', openBraceIndex, endIndex);
                if (closeBraceIndex == endIndex)
                {
                    sb.Append(requestUri, scanIndex, endIndex - scanIndex);
                    scanIndex = endIndex;
                }
                else
                {
                    sb.Append(requestUri, scanIndex, openBraceIndex - scanIndex);

                    if (args != null)
                    {
                        var arg = args.OfType<IHttpArgTypeArg>().Where(x => x != null && MemoryExtensions.Equals(requestUri.AsSpan(openBraceIndex + 1, closeBraceIndex - openBraceIndex - 1), x.Name, StringComparison.Ordinal)).FirstOrDefault();
                        if (arg != null)
                            sb.Append(arg.ToEscapeDataString());
                    }

                    scanIndex = closeBraceIndex + 1;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Find the brace index within specified range.
        /// </summary>
        private static int FindBraceIndex(string format, char brace, int startIndex, int endIndex)
        {
            // Example: {{prefix{{{Argument}}}suffix}}.
            var braceIndex = endIndex;
            var scanIndex = startIndex;
            var braceOccurenceCount = 0;

            while (scanIndex < endIndex)
            {
                if (braceOccurenceCount > 0 && format[scanIndex] != brace)
                {
                    if (braceOccurenceCount % 2 == 0)
                    {
                        // Even number of '{' or '}' found. Proceed search with next occurence of '{' or '}'.
                        braceOccurenceCount = 0;
                        braceIndex = endIndex;
                    }
                    else
                    {
                        // An unescaped '{' or '}' found.
                        break;
                    }
                }
                else if (format[scanIndex] == brace)
                {
                    if (brace == '}')
                    {
                        if (braceOccurenceCount == 0)
                        {
                            // For '}' pick the first occurence.
                            braceIndex = scanIndex;
                        }
                    }
                    else
                    {
                        // For '{' pick the last occurence.
                        braceIndex = scanIndex;
                    }

                    braceOccurenceCount++;
                }

                scanIndex++;
            }

            return braceIndex;
        }


        /// <summary>
        /// Deserialize the JSON <see cref="HttpResponseMessage.Content"/> into <see cref="Type"/> of <typeparamref name="TResp"/>.
        /// </summary>
        /// <typeparam name="TResp">The response <see cref="Type"/>.</typeparam>
        /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The deserialized response value.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Future proofing.")]
        protected async Task<TResp> ReadAsJsonAsync<TResp>(HttpResponseMessage response, CancellationToken cancellationToken = default)
        {
            response.EnsureSuccessStatusCode();
            if (response.Content == null)
                return default!;

#if NETSTANDARD2_1
            var data = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
#else
            var data = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
#endif
            return JsonSerializer.Deserialize<TResp>(new BinaryData(data))!;
        }

        /// <summary>
        /// Sends the <paramref name="request"/> returning the <see cref="HttpResponseMessage"/>.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResponseMessage"/>.</returns>
        protected abstract Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);

        /// <summary>
        /// Determines whether the <paramref name="response"/> or <paramref name="exception"/> result is transient in nature, and as such is a candidate for a retry.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
        /// <param name="exception">The <see cref="Exception"/>.</param>
        /// <returns><c>true</c> indicates transient; otherwise, <c>false</c>.</returns>
        public static (bool result, string error) IsTransient(HttpResponseMessage? response = null, Exception? exception = null)
        {
            if (exception != null)
            {
                if (exception is HttpRequestException)
                    return (true, $"Http Request Exception occurred: {exception.Message}");

                if (exception is TaskCanceledException)
                    return (true, "Task was cancelled.");
            }

            if (response == null)
                return (false, string.Empty);

            if ((int)response.StatusCode >= 500)
                return (true, $"Response status code was {response.StatusCode} >= 500.");

            if (response.StatusCode == HttpStatusCode.RequestTimeout)
                return (true, $"Response status code was {HttpStatusCode.RequestTimeout} ({(int)HttpStatusCode.RequestTimeout}).");

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                return (true, $"Response status code was {HttpStatusCode.TooManyRequests} ({(int)HttpStatusCode.TooManyRequests}).");

            return (false, string.Empty);
        }
    }
}