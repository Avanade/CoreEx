namespace CoreEx.Azure.Functions.TestApp.InventoryImports;

/// <summary>Defines deployment-owned persistence, report storage, and tenant routing for inventory imports.</summary>
/// <param name="ConnectionString">The SQL Server connection string; never input metadata.</param>
/// <param name="ReportContainer">An existing Blob container outside the input trigger route.</param>
/// <param name="TenantId">The trusted tenant selected by this deployment, never a CSV field or file name.</param>
public sealed record InventoryImportOptions(string ConnectionString, string ReportContainer, string TenantId);
