using CoreEx.Database.Postgres.Test.Unit.Models;
using CoreEx.EntityFrameworkCore;
using CoreEx.Results;

namespace CoreEx.Database.Postgres.Test.Unit.Repository;

public class TestEfDb(TestDbContext dbContext) : EfDb<TestDbContext>(dbContext, _options)
{
    private static readonly EfDbOptions _options = new EfDbOptions()
        .WithModel<Models.TestTable>(mo => mo
            .WithTenantFilter(allowFilterBypass: false)
            .WithLogicalDeleteFilter(allowFilterBypass: true)
            .WithFilter(q => q.Where(x => x.Flag != null && x.Flag == true), (_, _) => Result.AuthorizationError(), allowFilterBypass: true));

    public EfDbModel<Models.TestTable> Table => Model<Models.TestTable>();

    public EfDbMappedModel<Contracts.TestTableDto, TestTable, Contracts.TestTableDtoMapper> TableDto => Table.ToMappedModel<Contracts.TestTableDto, Contracts.TestTableDtoMapper>(Contracts.TestTableDtoMapper.Default);
}