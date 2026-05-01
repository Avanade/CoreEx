#pragma warning disable IDE0130 // Namespace does not match folder structure - this is by design.
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides <see href="https://learn.microsoft.com/en-us/ef/core/">Entity Framework Core</see> extensions.
/// </summary>
public static partial class CoreExEfDbExtensions
{
    /// <summary>
    /// Adds a <b>scoped</b> service for the <typeparamref name="TEfDb"/>.
    /// </summary>
    /// <typeparam name="TEfDb">The <see cref="IEfDb"/> <see cref="Type"/>.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    public static IServiceCollection AddEfDb<TEfDb>(this IServiceCollection services) where TEfDb : class, IEfDb => services.ThrowIfNull().AddScoped<TEfDb>();
}