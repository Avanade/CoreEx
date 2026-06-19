using CoreEx.Data;
using CoreEx.Entities;
using System.Text.Json;

namespace CoreEx.Database.Postgres.Test.Unit.Models;

public class TestTable : IIdentifier<Guid>, IETag, IChangeLogEx, ITenantId, ILogicallyDeleted
{
    public Guid Id { get; set; }
    public string? Text { get; set; }
    public int? Number { get; set; }
    public decimal? Amount { get; set; }
    public bool? Flag { get; set; }
    public DateOnly? Date { get; set; }
    public TimeOnly? Time { get; set; }
    public JsonElement? Json { get; set; }
    public string? ETag { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? CreatedOn { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTimeOffset? UpdatedOn { get; set; }
    public string? TenantId { get; set; }
    public bool IsDeleted { get; set; }
}