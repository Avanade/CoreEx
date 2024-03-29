// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.IO;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CoreEx.AspNetCore.HealthChecks
{
    /// <summary>
    /// Provides additional <see cref="HealthReport"/> <c>HealthCheckOptions.ResponseWriter</c> capabilities.
    /// </summary>
    public static class HealthReportStatusWriter
    {
        /// <summary>
        /// Writes the <paramref name="healthReport"/> as a JSON summary.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/>.</param>
        /// <param name="healthReport">The <see cref="HealthReport"/>.</param>
        public static Task WriteJsonSummary(HttpContext context, HealthReport healthReport) => WriteJson(context, healthReport, false, false, null);

        /// <summary>
        /// Writes the <paramref name="healthReport"/> as JSON including the <see cref="HealthReport.Entries"/> results.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/>.</param>
        /// <param name="healthReport">The <see cref="HealthReport"/>.</param>
        public static Task WriteJsonResults(HttpContext context, HealthReport healthReport) => WriteJson(context, healthReport, true, false, null);

        /// <summary>
        /// Writes the <paramref name="healthReport"/> as JSON including the <see cref="SettingsBase.Deployment"/> and <see cref="HealthReport.Entries"/> results.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/>.</param>
        /// <param name="healthReport">The <see cref="HealthReport"/>.</param>
        public static async Task WriteJsonDeploymentResults(HttpContext context, HealthReport healthReport) => await WriteJson(context, healthReport, true, true, null).ConfigureAwait(false);

        /// <summary>
        /// Writes the <paramref name="healthReport"/> as JSON to the <paramref name="context"/>.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/>.</param>
        /// <param name="healthReport">The <see cref="HealthReport"/>.</param>
        /// <param name="includeResults">Indicates whether to include <see cref="HealthReport.Entries"/> results (where applicable).</param>
        /// <param name="includeDeployment">Indicates whether to include <see cref="SettingsBase.Deployment"/> information (where applicable).</param>
        /// <param name="extension">An action to enable extensions to the underlying JSON being written.</param>
        public static async Task WriteJson(HttpContext context, HealthReport healthReport, bool includeResults = true, bool includeDeployment = true, Action<HealthReport, Utf8JsonWriter>? extension = null)
        {
            using var memoryStream = new MemoryStream();
            using (var jsonWriter = new Utf8JsonWriter(memoryStream))
            {
                jsonWriter.WriteStartObject();
                jsonWriter.WriteString("status", healthReport.Status.ToString());
                jsonWriter.WriteString("duration", healthReport.TotalDuration.ToString());

                if (ExecutionContext.HasCurrent)
                    jsonWriter.WriteString("correlationId", ExecutionContext.Current.CorrelationId);

                jsonWriter.WriteStartObject("results");

                foreach (var e in healthReport.Entries)
                {
                    jsonWriter.WriteStartObject(e.Key.Replace(' ', '-'));
                    jsonWriter.WriteString("status", e.Value.Status.ToString());
                    jsonWriter.WriteString("description", e.Value.Description);
                    jsonWriter.WriteString("duration", e.Value.Duration.ToString());

                    if (e.Value.Exception is not null)
                    {
                        var settings = ExecutionContext.GetService<SettingsBase>();
                        if (settings is not null && settings.IncludeExceptionInResult)
                            jsonWriter.WriteString("exception", e.Value.Exception?.ToString());
                        else
                            jsonWriter.WriteString("exception", e.Value.Exception?.Message);
                    }

                    if (includeDeployment)
                    {
                        var settings = ExecutionContext.GetService<SettingsBase>();
                        if (settings is not null)
                        {
                            jsonWriter.WritePropertyName("deployment");
                            JsonSerializer.Serialize(jsonWriter, settings, Text.Json.JsonSerializer.DefaultOptions);
                        }
                    }

                    if (includeResults && e.Value.Data.Count > 0)
                    {
                        jsonWriter.WriteStartObject("data");

                        foreach (var d in e.Value.Data)
                        {
                            jsonWriter.WritePropertyName(d.Key);
                            JsonSerializer.Serialize(jsonWriter, d.Value, d.Value?.GetType() ?? typeof(object), Text.Json.JsonSerializer.DefaultOptions);
                        }

                        jsonWriter.WriteEndObject();
                    }

                    jsonWriter.WriteEndObject();
                }

                extension?.Invoke(healthReport, jsonWriter);

                jsonWriter.WriteEndObject();
                jsonWriter.WriteEndObject();
            }

            context.Response.ContentType = MediaTypeNames.Application.Json;
            await context.Response.WriteAsync(Encoding.UTF8.GetString(memoryStream.ToArray())).ConfigureAwait(false);
        }
    }
}