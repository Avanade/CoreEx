// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Http
{
    /// <summary>
    /// Provides the HTTP request/response logging.
    /// </summary>
    public class HttpRequestLogger
    {
        private readonly SettingsBase _settings;
        private readonly ILogger _logger;

        /// <summary>
        /// Private constructor.
        /// </summary>
        private HttpRequestLogger(SettingsBase settings, ILogger logger)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a <see cref="HttpRequestLogger"/> instance.
        /// </summary>
        /// <param name="settings">The <see cref="SettingsBase"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <returns>The <see cref="HttpRequestLogger"/>.</returns>
        public static HttpRequestLogger Create(SettingsBase settings, ILogger logger) => new(settings, logger);


        /// <summary>
        /// Logs the <paramref name="request"/>.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
#if NETSTANDARD2_1
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Future proofing.")]
#endif
        public async Task LogRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_settings.HttpLogContent)
                {
                    _logger.LogInformation("Sending HTTP request {HttpRequestMethod} {HttpRequestUri} {HttpRequestContent} ({HttpRequestMediaType})",
                        request.Method,
                        request.RequestUri,
#if NETSTANDARD2_1
                        request.Content == null ? "No content." : await request.Content.ReadAsStringAsync().ConfigureAwait(false),
#else
                        request.Content == null ? "No content." : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false),
#endif
                        request.Content?.Headers?.ContentType?.MediaType ?? "None");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error encountered whilst logging HTTP Request: {Message}", ex.Message);
            }
        }

        /// <summary>
        /// Logs the <paramref name="response"/>.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/>.</param>
        /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
        /// <param name="operationTime">Time in which response was received by the client</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Future proofing.")]
        public async Task LogResponseAsync(HttpRequestMessage request, HttpResponseMessage response, TimeSpan operationTime, CancellationToken cancellationToken = default)
        {
            // Logging should never throw an exception.
            try
            {
                if (IsConsideredSuccessful(response))
                {
                    if (_settings.HttpLogContent)
                    {
                        _logger.LogInformation("Received HTTP Response in {Time} {HttpRequestHost} {HttpStatusCodeText} ({HttpStatusCode}) {HttpResponseContent} ({HttpResponseMediaType})",
                            operationTime,
                            request.RequestUri?.Host,
                            response.StatusCode,
                            (int)response.StatusCode,
#if NETSTANDARD2_1
                            response.Content == null ? "No content." : await response.Content.ReadAsStringAsync().ConfigureAwait(false),
#else
                            response.Content == null ? "No content." : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false),
#endif
                            response.Content?.Headers?.ContentType?.MediaType ?? "None");
                    }
                }
                else
                {
                    if (_settings.HttpLogContent)
                    {
                        _logger.LogError("Unexpected HTTP Response in {Time} {HttpRequestHost} {HttpStatusCodeText} ({HttpStatusCode}) {HttpResponseContent} ({HttpResponseMediaType})",
                            operationTime,
                            request.RequestUri?.Host,
                            response.StatusCode,
                            (int)response.StatusCode,
#if NETSTANDARD2_1
                            response.Content == null ? "No content." : await response.Content.ReadAsStringAsync().ConfigureAwait(false),
#else
                            response.Content == null ? "No content." : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false),
#endif
                            response.Content?.Headers?.ContentType?.MediaType ?? "None");
                    }
                    else
                    {
                        _logger.LogDebug("Unsuccessful HTTP Response in {Time} {HttpRequestHost} {HttpStatusCodeText} ({HttpStatusCode})",
                            operationTime,
                            request.RequestUri?.Host,
                            response.StatusCode,
                            (int)response.StatusCode);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error encountered whilst logging HTTP Response: {Message}", ex.Message);
            }
        }

        /// <summary>
        /// Determine whether the response is considered successful.
        /// </summary>
        private static bool IsConsideredSuccessful(HttpResponseMessage response) => response.IsSuccessStatusCode;
    }
}