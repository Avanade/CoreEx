// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;
using CoreEx.Configuration;
using CoreEx.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CoreEx.AspNetCore.HealthChecks
{
    /// <summary>
    /// Provides the Health Check service.
    /// </summary>
    public class HealthService(SettingsBase settings, HealthCheckService healthCheckService, IJsonSerializer jsonSerializer)
    {
        private readonly SettingsBase _settings = settings.ThrowIfNull(nameof(settings));
        private readonly HealthCheckService _healthCheckService = healthCheckService.ThrowIfNull(nameof(healthCheckService));
        private readonly IJsonSerializer _jsonSerializer = jsonSerializer.ThrowIfNull(nameof(jsonSerializer));

        /// <summary>
        /// Runs the health check and returns JSON result.
        /// </summary>
        public async Task<IActionResult> RunAsync()
        {
            try
            {
                var report = await _healthCheckService.CheckHealthAsync();
                return BuildResponse(report);
            }
            catch (Exception ex)
            {
                var reportEntry = new HealthReportEntry(HealthStatus.Unhealthy, "Failed during health checks", TimeSpan.Zero, ex, null);
                var report = new HealthReport(new Dictionary<string, HealthReportEntry> { { "Running health checks", reportEntry } }, TimeSpan.Zero);
                return BuildResponse(report);
            }
        }

        /// <summary>
        /// Builds the health report response.
        /// </summary>
        private IActionResult BuildResponse(HealthReport healthReport)
        {
            var code = healthReport.Status == HealthStatus.Healthy ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable;

            var json = _jsonSerializer.Serialize(new
            {
                healthReport = new
                {
                    Status = healthReport.Status.ToString(),
                    TotalDuration = healthReport.TotalDuration.ToString(),
                    healthReport.Entries
                },
                _settings.Deployment
            }, JsonWriteFormat.Indented);

            return new ContentResult
            {
                Content = json,
                ContentType = MediaTypeNames.Application.Json,
                StatusCode = (int)code
            };
        }
    }
}