// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CoreEx
{
    /// <summary>
    /// Defines the core execution capabilities.
    /// </summary>
    public interface IExecutor
    {
        /// <summary>
        /// Gets the <see cref="CoreEx.ExecutionContext"/>.
        /// </summary>
        ExecutionContext ExecutionContext { get; }

        /// <summary>
        /// Gets the <see cref="SettingsBase"/>.
        /// </summary>
        SettingsBase Settings { get; }

        /// <summary>
        /// Gets the <see cref="ILogger"/>.
        /// </summary>
        ILogger Logger { get; }

        /// <summary>
        /// Encapsulates the execution of a <paramref name="function"/>.
        /// </summary>
        /// <param name="function">The function logic to invoke.</param>
        Task RunAsync(Func<Task> function);
    }
}