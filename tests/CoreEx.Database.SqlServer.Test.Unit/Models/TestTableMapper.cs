using CoreEx.Database.Mapping;
using System.Text.Json;

namespace CoreEx.Database.SqlServer.Test.Unit.Models;

public class TestTableMapper : DatabaseMapper<TestTable>
{
    public override TestTable MapFromDb(DatabaseRecord r, OperationType operationType = OperationType.Unspecified) => new TestTable()
    {
        Id = r.GetValue<Guid>("TableId"),
        Text = r.GetValue<string?>("Text"),
        Number = r.GetValue<int?>("Number"),
        Amount = r.GetValue<decimal?>("Amount"),
        Flag = r.GetValue<bool?>("Flag"),
        Date = r.GetValue<DateOnly?>("Date"),
        Time = r.GetValue<TimeOnly?>("Time"),
        Json = r.GetValueFromJson<JsonElement?>("Json")
    }.MapStandardFromDb(r);
}