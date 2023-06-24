// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.Extensions.Hosting;

namespace CoreEx.Hosting
{
    /// <summary>
    /// Provides <see cref="IHostBuilder"/> extensions.
    /// </summary>
    public static class HostStartupExtensions
    {
        /// <summary>
        /// Configures the <see cref="IHostBuilder"/> with the <see cref="IHostStartup"/> capabilities.
        /// </summary>
        /// <typeparam name="TStartup">The <see cref="IHostedService"/>.</typeparam>
        /// <param name="hostBuilder">The <see cref="IHostBuilder"/>.</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder ConfigureHostStartup<TStartup>(this IHostBuilder hostBuilder) where TStartup : class, IHostStartup, new()
        {
            var startup = new TStartup();
            return hostBuilder.ConfigureAppConfiguration(startup.ConfigureAppConfiguration)
                .ConfigureHostConfiguration(startup.ConfigureHostConfiguration)
                .ConfigureServices((hbc, sc) => startup.ConfigureServices(sc));
        }
    }
}