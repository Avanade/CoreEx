// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Diagnostics;
using System;
using Microsoft.Extensions.Logging;

namespace CoreEx.Invokers
{
    /// <summary>
    /// Enables the standardized invoker capabilities.
    /// </summary>
    public interface IInvoker
    {
        /// <summary>
        /// Gets the start of an <see cref="Activity"/> action.
        /// </summary>
        Action<InvokeArgs>? OnActivityStart { get; }

        /// <summary>
        /// Gets the <see cref="Activity"/>  <see cref="Exception"/> action.
        /// </summary>
        Action<InvokeArgs, Exception>? OnActivityException { get; }

        /// <summary>
        /// Gets the completion of an <see cref="Activity"/> action.
        /// </summary>
        Action<InvokeArgs>? OnActivityComplete { get; }

        /// <summary>
        /// Get the caller information <see cref="ILogger"/> formatter.
        /// </summary>
        Func<InvokeArgs, string> CallerLoggerFormatter { get; }
    }
}