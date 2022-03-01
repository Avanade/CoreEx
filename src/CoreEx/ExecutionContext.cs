// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading;

namespace CoreEx
{
    /// <summary>
    /// Represents a thread-bound (request) execution context using <see cref="AsyncLocal{ExecutionContext}"/>.
    /// </summary>
    /// <remarks>Used to house/pass context parameters and capabilities that are outside of the general operation arguments.</remarks>
    public class ExecutionContext
    {
        private static readonly AsyncLocal<ExecutionContext?> _asyncLocal = new();

        /// <summary>
        /// Gets or sets the function to create a default <see cref="ExecutionContext"/> instance.
        /// </summary>
        public static Func<ExecutionContext>? Create { get; set; } = () => new ExecutionContext();

        /// <summary>
        /// Indicates whether the <see cref="ExecutionContext"/> <see cref="Current"/> has a value.
        /// </summary>
        public static bool HasCurrent => _asyncLocal.Value != null;

        /// <summary>
        /// Gets the current <see cref="ExecutionContext"/> for the executing thread graph (see <see cref="AsyncLocal{T}"/>).
        /// </summary>
        /// <remarks>Where not previously set (see <see cref="SetCurrent(ExecutionContext?)"/> then the <see cref="Create"/> will be invoked as a backup to create an instance.</remarks>
        public static ExecutionContext Current => _asyncLocal.Value ??= Create?.Invoke() ?? 
            throw new InvalidOperationException("There is currently no ExecutionContext.Current instance; this must be set (SetCurrent) prior to access. Use ExecutionContext.HasCurrent to verify value and avoid this exception.");

        /// <summary>
        /// Resets (clears) the <see cref="Current"/> <see cref="ExecutionContext"/>.
        /// </summary>
        public static void Reset() => _asyncLocal.Value = null;

        /// <summary>
        /// Sets the <see cref="Current"/> instance (only allowed where <see cref="HasCurrent"/> is <c>false</c>).
        /// </summary>
        /// <param name="executionContext">The <see cref="ExecutionContext"/> instance.</param>
        public static void SetCurrent(ExecutionContext? executionContext)
        {
            if (HasCurrent)
                throw new InvalidOperationException("The SetCurrent method can only be used where there is no Current instance.");

            _asyncLocal.Value = executionContext ?? throw new ArgumentNullException(nameof(executionContext));
        }

        /// <summary>
        /// Gets the <see cref="ServiceProvider"/>.
        /// </summary>
        /// <remarks>This is automaticall set via the <see cref="DependencyInjection.ServiceCollectionExtensions.AddExecutionContext(Microsoft.Extensions.DependencyInjection.IServiceCollection, Func{IServiceProvider, ExecutionContext}?)"/>.</remarks>
        public IServiceProvider? ServiceProvider { get; set; }

        /// <summary>
        /// Gets or sets the correlation identifier.
        /// </summary>
        /// <remarks>Defaults to <see cref="Guid.NewGuid"/>.</remarks>
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString().ToLowerInvariant();
    }
}