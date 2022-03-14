// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
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
        public async Task LogRequestAsync(HttpRequestMessage request)
        {
            try
            {
                if (_settings.HttpLogContent)
                {
                    _logger.LogInformation("Sending HTTP request {HttpRequestMethod} {HttpRequestUri} {HttpRequestContent} ({HttpRequestMediaType})",
                        request.Method,
                        request.RequestUri,
                        request.Content == null ? "No content." : await request.Content.ReadAsStringAsync().ConfigureAwait(false),
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
        public async Task LogResponseAsync(HttpRequestMessage request, HttpResponseMessage response)
        {
            // Logging should never throw an exception.
            try
            {
                if (IsConsideredSuccessful(response))
                {
                    if (_settings.HttpLogContent)
                    {
                        _logger.LogInformation("Received HTTP Response {HttpRequestHost} {HttpStatusCodeText} ({HttpStatusCode}) {HttpResponseContent} ({HttpResponseMediaType})",
                            request.RequestUri?.Host,
                            response.StatusCode,
                            (int)response.StatusCode,
                            response.Content == null ? "No content." : await response.Content.ReadAsStringAsync().ConfigureAwait(false),
                            response.Content?.Headers?.ContentType?.MediaType ?? "None");
                    }
                }
                else
                {
                    if (_settings.HttpLogContent)
                    {
                        _logger.LogError("Unexpected HTTP Response {HttpRequestHost} {HttpStatusCodeText} ({HttpStatusCode}) {HttpResponseContent} ({HttpResponseMediaType})",
                            request.RequestUri?.Host,
                            response.StatusCode,
                            (int)response.StatusCode,
                            response.Content == null ? "No content." : await response.Content.ReadAsStringAsync().ConfigureAwait(false),
                            response.Content?.Headers?.ContentType?.MediaType ?? "None");
                    }
                    else
                    {
                        _logger.LogError("Unexpected HTTP Response {HttpRequestHost} {HttpStatusCodeText} ({HttpStatusCode})",
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