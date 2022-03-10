// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using CoreEx.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreEx
{
    /// <summary>
    /// Provides the core execution capabilities.
    /// </summary>
    public class Executor : IExecutor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Executor"/> class.
        /// </summary>
        /// <param name="executionContext">The <see cref="ExecutionContext"/>.</param>
        /// <param name="settings">The <see cref="SettingsBase"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        protected Executor(ExecutionContext executionContext, SettingsBase settings, ILogger<Executor> logger)
        {
            ExecutionContext = executionContext ?? throw new ArgumentNullException(nameof(executionContext));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the <see cref="CoreEx.ExecutionContext"/>.
        /// </summary>
        public ExecutionContext ExecutionContext { get; }

        /// <summary>
        /// Gets the <see cref="SettingsBase"/>.
        /// </summary>
        public SettingsBase Settings { get; }

        /// <summary>
        /// Gets the <see cref="ILogger"/>.
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// Gets or sets the list of secondary correlation identifier names.
        /// </summary>
        protected virtual IEnumerable<string> SecondaryCorrelationIdNames { get; set; } = new string[] { "x-ms-client-tracking-id" };

        /// <summary>
        /// Gets the list of correlation identifier names, being <see cref="HttpConsts.CorrelationIdHeaderName"/> and <see cref="SecondaryCorrelationIdNames"/> (inclusive).
        /// </summary>
        /// <returns>The list of correlation identifier names.</returns>
        protected virtual IEnumerable<string> GetCorrelationIdNames()
        {
            var list = new List<string>(new string[] { HttpConsts.CorrelationIdHeaderName });
            list.AddRange(SecondaryCorrelationIdNames);
            return list;
        }

        /// <inheritdoc/>
        public async Task RunAsync(Func<Task> function)
        {
            if (function == null)
                throw new ArgumentNullException(nameof(function));

            var scope = Logger.BeginScope(new Dictionary<string, object>() { { HttpConsts.CorrelationIdHeaderName, ExecutionContext.CorrelationId } });

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
            }
        }
    }
}