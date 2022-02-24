// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx
{
    /// <summary>
    /// Provides the core execution capabilities.
    /// </summary>
    public class Executor : IExecutor
    {
        private static readonly AsyncLocal<string?> _correlationId = new();

        /// <summary>
        /// Gets the correlation identifier for the current execution.
        /// </summary>
        public static string GetCorrelationId() => _correlationId.Value ??= Guid.NewGuid().ToString().ToLowerInvariant();

        /// <summary>
        /// Sets the correlation identifier for the current execution.
        /// </summary>
        /// <param name="value">The correlation identifier.</param>
        public static void SetCorrelationId(string? value) => _correlationId.Value = value;

        /// <summary>
        /// Initializes a new instance of the <see cref="Executor"/> class.
        /// </summary>
        /// <param name="settings">The <see cref="SettingsBase"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        protected Executor(SettingsBase settings, ILogger<Executor> logger)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the <see cref="SettingsBase"/>.
        /// </summary>
        public SettingsBase Settings { get; }

        /// <summary>
        /// Gets the <see cref="ILogger"/>.
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// Gets the primary correlation identifier name.
        /// </summary>
        protected string CorrelationIdName { get; set; } = "x-correlation-id";

        /// <summary>
        /// Gets or sets the list of secondary correlation identifier names.
        /// </summary>
        protected virtual IEnumerable<string> SecondaryCorrelationIdNames { get; set; } = new string[] { "x-ms-client-tracking-id" };

        /// <summary>
        /// Gets the list of correlation identifier names, being <see cref="CorrelationIdName"/> and <see cref="SecondaryCorrelationIdNames"/> (inclusive).
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<string> GetCorrelationIdNames()
        {
            var list = new List<string>(new string[] { CorrelationIdName });
            list.AddRange(SecondaryCorrelationIdNames);
            return list;
        }

        /// <inheritdoc/>
        public async Task RunAsync(Func<Task> function)
        {
            if (function == null)
                throw new ArgumentNullException(nameof(function));

            var scope = Logger.BeginScope(new Dictionary<string, object>() { { CorrelationIdName, GetCorrelationId() } });

            try
            {
                await function().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "Executor encountered an Unhandled Exception: {Error}", ex.Message);
                throw;
            }
            finally
            {
                scope.Dispose();
                SetCorrelationId(null);
            }
        }
    }
}