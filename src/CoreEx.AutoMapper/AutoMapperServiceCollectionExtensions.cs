// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Mapping;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides <see cref="IServiceCollection"/> extenstion methods.
    /// </summary>
    public static class AutoMapperServiceCollectionExtensions
    {
        /// <summary>
        /// Adds an <see cref="AutoMapperWrapper"/> to wrap the <see cref="AutoMapper.IMapper"/> as a singleton services.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
        public static IServiceCollection AddAutoMapperWrapper(this IServiceCollection services)
            => (services ?? throw new ArgumentNullException(nameof(services))).AddSingleton<IMapper>(sp => new AutoMapperWrapper(sp.GetRequiredService<AutoMapper.IMapper>()));
    }
}