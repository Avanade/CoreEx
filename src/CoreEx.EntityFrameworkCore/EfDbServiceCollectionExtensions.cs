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
        /// Adds the <typeparamref name="TEfDb"/> as a scoped <see cref="IEfDb"/> service.
        /// </summary>
        /// <typeparam name="TEfDb">The corresponding entity framework <see cref="IEfDb"/> <see cref="Type"/>.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> to support fluent-style method-chaining.</returns>
        public static IServiceCollection AddEfDb<TEfDb>(this IServiceCollection services) where TEfDb : class, IEfDb => services.AddScoped<IEfDb, TEfDb>();

        /// <summary>
        /// Adds the <typeparamref name="TEfDb"/> as a scoped <typeparamref name="TIEfDb"/> service.
        /// </summary>
        /// <typeparam name="TIEfDb">The corresponding entity framework <see cref="IEfDb"/> service <see cref="Type"/>.</typeparam>
        /// <typeparam name="TEfDb">The corresponding entity framework <typeparamref name="TIEfDb"/> implementation <see cref="Type"/>.</typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddEfDb<TIEfDb, TEfDb>(this IServiceCollection services) where TIEfDb : class, IEfDb where TEfDb : class, TIEfDb => services.AddScoped<TIEfDb, TEfDb>();
    }
}