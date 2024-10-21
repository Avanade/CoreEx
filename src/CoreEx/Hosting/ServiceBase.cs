// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Hosting
{
    /// <summary>
    /// Represents the base class for a self-orchestrated service to <see cref="ExecuteAsync(CancellationToken)"/> for a specified <see cref="MaxIterations"/>.
    /// </summary>
    public abstract class ServiceBase
    {
        private string? _name;
        private int? _maxIterations;

        /// <summary>
        /// The configuration settings name for <see cref="MaxIterations"/>.
        /// </summary>
        public const string MaxIterationsName = nameof(MaxIterations);

        /// <summary>
        /// Gets or sets the default used where the specified <see cref="MaxIterations"/> is less than or equal to zero. Defaults to <b>one</b> iteration.
        /// </summary>
        public static int DefaultMaxIterations { get; set; } = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBase"/> class.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <param name="settings">The <see cref="SettingsBase"/>; defaults to instance from the <paramref name="serviceProvider"/> where not specified.</param>
        public ServiceBase(IServiceProvider serviceProvider, ILogger logger, SettingsBase? settings = null)
        {
            ServiceProvider = serviceProvider.ThrowIfNull(nameof(serviceProvider));
            Logger = logger.ThrowIfNull(nameof(logger));
            Settings = settings ?? ServiceProvider.GetService<SettingsBase>() ?? new DefaultSettings(ServiceProvider.GetRequiredService<IConfiguration>());
        }

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/>.
        /// </summary>
        protected IServiceProvider ServiceProvider;

        /// <summary>
        /// Gets the <see cref="SettingsBase"/>.
        /// </summary>
        protected SettingsBase Settings { get; }

        /// <summary>
        /// Gets the <see cref="ILogger"/>.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Gets the service name (used for the likes of configuration and logging).
        /// </summary>
        /// <remarks>Defaults to the <see cref="Type"/> <see cref="MemberInfo.Name"/>.</remarks>
        public virtual string ServiceName => _name ??= GetType().Name;

        /// <summary>
        /// Gets or sets the maximum number of iterations per execution.
        /// </summary>
        public virtual int MaxIterations
        {
            get => _maxIterations ?? DefaultMaxIterations;
            set => _maxIterations = value <= 0 ? DefaultMaxIterations : value;
        }

        /// <summary>
        /// <see cref="ExecuteAsync(IServiceProvider, CancellationToken)"/> up to the specified number of <see cref="MaxIterations"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>Each invocation of the <see cref="ExecuteAsync(IServiceProvider, CancellationToken)"/> will be managed within the context of a new Dependency Injection (DI) <see cref="ServiceProviderServiceExtensions.CreateScope">scope</see> that is passed for direct usage.</remarks>
        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            for (int i = 0; i < MaxIterations; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                await ServiceInvoker.Current.InvokeAsync(this, async (_, cancellationToken) =>
                {
                    // Create a scope in which to perform the execution.
                    using var scope = ServiceProvider.CreateScope();
                    ExecutionContext.Reset();

                    try
                    {
                        if (!await ExecuteAsync(scope.ServiceProvider, cancellationToken).ConfigureAwait(false))
                            return;
                    }
                    catch (Exception ex)
                    {
                        if (ex is TaskCanceledException || (ex is AggregateException aex && aex.InnerException is TaskCanceledException))
                            return;

                        Logger.LogCritical(ex, "{ServiceName} failure as a result of an unexpected exception: {Error}", ServiceName, ex.Message);
                        throw;
                    }
                }, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Invoked to perform the per-iteration work.
        /// </summary>
        /// <param name="scopedServiceProvider">The scoped <see cref="IServiceProvider"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates to execute the next iteration (i.e. continue); otherwise, <c>false</c> to stop.</returns>
        /// <remarks>Each invocation of the <see cref="ExecuteAsync(IServiceProvider, CancellationToken)"/> will be managed within the context of a new Dependency Injection (DI) <see cref="ServiceProviderServiceExtensions.CreateScope">scope</see> that is passed for direct usage.</remarks>
        protected abstract Task<bool> ExecuteAsync(IServiceProvider scopedServiceProvider, CancellationToken cancellationToken);
    }
}