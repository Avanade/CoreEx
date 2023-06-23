// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CoreEx.Hosting
{
    /// <summary>
    /// Provides standardized host startup capabilities
    /// </summary>
    public class HostStartup : IHostStartup
    {
        /// <inheritdoc/>
        public virtual void ConfigureAppConfiguration(HostBuilderContext context, IConfigurationBuilder config) { }

        /// <inheritdoc/>
        public virtual void ConfigureHostConfiguration(IConfigurationBuilder config) { }

        /// <inheritdoc/>
        public virtual void ConfigureServices(IServiceCollection services) { }
    }
}