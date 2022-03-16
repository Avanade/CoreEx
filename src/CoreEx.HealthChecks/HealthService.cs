using CoreEx.Configuration;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using CoreEx.Json;

namespace CoreEx.Healthchecks
{
    /// <summary>Health Check service.</summary>
    public class HealthService
    {
        private readonly SettingsBase _settings;
        private readonly HealthCheckService _healthCheckService;
        private readonly IJsonSerializer _jsonSerializer;

        /// <summary>Initializes a new instance of the <see cref="HealthService"/> class.</summary>
        public HealthService(SettingsBase settings, HealthCheckService healthCheckService, IJsonSerializer jsonSerializer)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
             _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
            _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
        }

        /// <summary>Runs the health check and returns JSON result.</summary>
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

        private IActionResult BuildResponse(HealthReport healthReport)
        {
            var code = healthReport.Status == HealthStatus.Healthy ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable;

            var json = _jsonSerializer.Serialize(new
            {
                healthReport = new {
                    Status = healthReport.Status.ToString(),
                    TotalDuration = healthReport.TotalDuration.ToString(),
                    Entries = healthReport.Entries
                },
                _settings.Deployment
            }, JsonWriteFormat.Indented);

            return new ContentResult
            {
                Content = json,
                ContentType = "application/json",
                StatusCode = (int)code
            };
        }
    }
}