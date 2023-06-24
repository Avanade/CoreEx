// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CoreEx.Hosting
{
    /// <summary>
    /// Enables standardized host startup capabilities
    /// </summary>
    public interface IHostStartup
    {
        /// <summary>
        /// Sets up the configuration for the remainder of the build process and application.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="config"></param>
        /// <remarks>This is intended to be invoked by the <see cref="IHostBuilder.ConfigureAppConfiguration(System.Action{HostBuilderContext, IConfigurationBuilder})"/>.</remarks>
        void ConfigureAppConfiguration(HostBuilderContext context, IConfigurationBuilder config);

        /// <summary>
        /// Sets up the configuration for the builder itself to initialize the <see cref="IHostEnvironment"/>.
        /// </summary>
        /// <param name="config">The <see cref="IConfigurationBuilder"/>.</param>
        /// <remarks>This is intended to be invoked by the <see cref="IHostBuilder.ConfigureHostConfiguration(System.Action{IConfigurationBuilder})"/>.</remarks>
        void ConfigureHostConfiguration(IConfigurationBuilder config);

        /// <summary>
        /// Adds services to the container.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <remarks>This is intended to be invoked by the <see cref="IHostBuilder.ConfigureServices(System.Action{HostBuilderContext, IServiceCollection})"/>.</remarks>
        void ConfigureServices(IServiceCollection services);
    }
}