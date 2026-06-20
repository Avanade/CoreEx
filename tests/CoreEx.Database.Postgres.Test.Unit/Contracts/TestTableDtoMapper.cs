using CoreEx.Database.Postgres.Test.Unit.Models;
using CoreEx.Json;
using CoreEx.Mapping;
using System.Text.Json;

namespace CoreEx.Database.Postgres.Test.Unit.Contracts;

public class TestTableDtoMapper : BiDirectionMapper<TestTableDto, TestTable, TestTableDtoMapper>
{
    protected override TestTable OnMap(TestTableDto source) => new TestTable()
    {
        Id = source.Id,
        Text = source.Text,
        Number = source.Number,
        Amount = source.Amount,
        Date = source.Date,
        Time = source.Time,
        Json = source.Key is null ? null : JsonSerializer.SerializeToElement(source.Key, JsonDefaults.SerializerOptions),
        Flag = true
    }.MapStandardFrom(source);

    protected override TestTableDto OnMap(TestTable source) => new TestTableDto()
    {
        Id = source.Id,
        Text = source.Text,
        Number = source.Number,
        Amount = source.Amount,
        Date = source.Date,
        Time = source.Time,
        Key = source.Json?.Deserialize<TestTableDto.KeyValueDto>(JsonDefaults.SerializerOptions)
    }.MapStandardFrom(source);
}