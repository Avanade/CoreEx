namespace CoreEx.Azure.Functions.TestApp.InventoryImports;

/// <summary>Provides focused host registration for the executable import example.</summary>
public static class InventoryImportRegistration
{
    /// <summary>Registers the capability with deployment-owned routing and storage settings.</summary>
    public static IServiceCollection AddInventoryImport(this IServiceCollection services, Func<IServiceProvider, InventoryImportOptions> configure)
    {
        services.AddSingleton(sp =>
        {
            var options = configure(sp).ThrowIfNull();
            options.ConnectionString.ThrowIfNullOrWhiteSpace();
            options.ReportContainer.ThrowIfNullOrWhiteSpace();
            options.TenantId.ThrowIfNullOrWhiteSpace();
            return options;
        });
        services.AddScoped<IInventoryImportService, InventoryImportService>();
        services.AddScoped<InventoryImportRunner>();
        services.AddScoped<InventoryBlobImport>();
        services.AddScoped<InventoryImportReport>();
        services.AddScoped(sp => new SqlConnection(sp.GetRequiredService<InventoryImportOptions>().ConnectionString));
        services.AddSqlServerDatabase();
        services.AddSqlServerOutboxPublisher((_, publisher) => publisher.SetStatementByConvention("Inventory"));
        services.AddSqlServerUnitOfWork();
        return services;
    }
}
