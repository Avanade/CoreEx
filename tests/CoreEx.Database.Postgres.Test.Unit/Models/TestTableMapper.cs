using CoreEx.Database.Mapping;
using System.Text.Json;

namespace CoreEx.Database.Postgres.Test.Unit.Models;

public class TestTableMapper : DatabaseMapper<TestTable>
{
    public override TestTable MapFromDb(DatabaseRecord r, OperationType operationType = OperationType.Unspecified) => new TestTable()
    {
        Id = r.GetValue<Guid>("table_id"),
        Text = r.GetValue<string?>("text"),
        Number = r.GetValue<int?>("number"),
        Amount = r.GetValue<decimal?>("amount"),
        Flag = r.GetValue<bool?>("flag"),
        Date = r.GetValue<DateOnly?>("date"),
        Time = r.GetValue<TimeOnly?>("time"),
        Json = r.GetValueFromJson<JsonElement?>("json")
    }.MapStandardFromDb(r);
}