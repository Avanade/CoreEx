// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.EntityFrameworkCore;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides <see cref="IServiceCollection"/> extenstion methods.
    /// </summary>
    public static class EfDbServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the <see cref="IEfDb"/> as a scoped service.
        /// </summary>
        /// <typeparam name="TEfDb">The corresponding entity framework <see cref="IEfDb"/> <see cref="Type"/>.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> to support fluent-style method-chaining.</returns>
        public static IServiceCollection AddEfDb<TEfDb>(this IServiceCollection services) where TEfDb : class, IEfDb => services.AddScoped<IEfDb, TEfDb>();
    }
}