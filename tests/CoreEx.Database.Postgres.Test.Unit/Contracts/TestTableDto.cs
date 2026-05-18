using CoreEx.Entities;

namespace CoreEx.Database.Postgres.Test.Unit.Contracts;

public class TestTableDto : IIdentifier<Guid>, IETag, IChangeLog
{
    public Guid Id { get; set; }
    public string? Text { get; set; }
    public int? Number { get; set; }
    public decimal? Amount { get; set; }
    public DateOnly? Date { get; set; }
    public TimeOnly? Time { get; set; }
    public KeyValueDto? Key { get; set; }
    public string? ETag { get; set; }
    public ChangeLog? ChangeLog { get; set; }

    public class KeyValueDto
    {
        public string? Key { get; set; }
    }
}