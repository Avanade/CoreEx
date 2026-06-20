using CoreEx.Database.Postgres.Test.Unit.Repository;
using CoreEx.Results;

namespace CoreEx.Database.Postgres.Test.Unit;

public partial class EntityFrameworkCrudTests 
{
    [Test]
    public void Get_NotFound() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var tt = await ef.Table.GetAsync(404.ToGuid()).ConfigureAwait(false);
        tt.Should().BeNull();
    }).AssertSuccess());

    [Test]
    public void Get_NotFound_WrongTenant() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var tt = await ef.Table.GetAsync(5.ToGuid()).ConfigureAwait(false);
        tt.Should().BeNull();
    }).AssertSuccess());

    [Test]
    public void Get_NotFound_IsDeleted() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var tt = await ef.Table.GetAsync(4.ToGuid()).ConfigureAwait(false);
        tt.Should().BeNull();
    }).AssertSuccess());

    [Test]
    public void Get_NotAuthorized() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var r = await ef.Table.GetWithResultAsync(7.ToGuid()).ConfigureAwait(false);
        r.IsAuthorizationError.Should().BeTrue();
    }).AssertSuccess());

    [Test]
    public void Get_Found() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();

        var tt = await ef.Table.GetAsync(2.ToGuid()).ConfigureAwait(false);
        tt.Should().NotBeNull();
        tt.Id.Should().Be(2.ToGuid());
        tt.Text.Should().Be("Abc");
        tt.Number.Should().Be(123);
        tt.Amount.Should().Be(45.67m);
        tt.Flag.Should().BeTrue();
        tt.Date.Should().Be(new DateOnly(2024, 6, 20));
        tt.Time.Should().Be(new TimeOnly(14, 30, 59));
        tt.Json.Should().NotBeNull().And.Subject.ToString().Should().Be("{\"Key\": \"Value\"}");
        tt.CreatedBy.Should().NotBeNull();
        tt.CreatedOn.Should().NotBeNull();
        tt.UpdatedBy.Should().BeNull();
        tt.UpdatedOn.Should().BeNull();
    }).AssertSuccess());

    [Test]
    public void Get_Mapped_Found() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();

        var dto = await ef.TableDto.GetAsync(2.ToGuid()).ConfigureAwait(false);
        dto.Should().NotBeNull();
        dto.Id.Should().Be(2.ToGuid());
        dto.Text.Should().Be("Abc");
        dto.Number.Should().Be(123);
        dto.Amount.Should().Be(45.67m);
        dto.Date.Should().Be(new DateOnly(2024, 6, 20));
        dto.Time.Should().Be(new TimeOnly(14, 30, 59));
        dto.Key.Should().NotBeNull();
        dto.Key.Key.Should().Be("Value"); dto.ETag.Should().NotBeNullOrEmpty();
        dto.ChangeLog.Should().NotBeNull();
        dto.ChangeLog.CreatedBy.Should().NotBeNull();
        dto.ChangeLog.CreatedOn.Should().NotBeNull();
        dto.ChangeLog.UpdatedBy.Should().BeNull();
        dto.ChangeLog.UpdatedOn.Should().BeNull();
    }).AssertSuccess());
}