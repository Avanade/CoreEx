using CoreEx.Database.SqlServer.Test.Unit.Contracts;
using CoreEx.Database.SqlServer.Test.Unit.Models;
using CoreEx.Database.SqlServer.Test.Unit.Repository;
using CoreEx.Results;
using System.Text.Json;

namespace CoreEx.Database.SqlServer.Test.Unit;

public partial class EntityFrameworkCrudTests 
{
    [Test]
    public void Create_IsDeleted() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var m = new TestTable
        {
            IsDeleted = true
        };

        var act = async () => await ef.Table.CreateWithResultAsync(m).ConfigureAwait(false);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Cannot create a model with a deleted state; IsDeleted must be false.*");
    }).AssertSuccess());

    [Test]
    public void Create_NotAuthorized() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var m = new TestTable
        {
            Flag = false,
        };

        var r = await ef.Table.CreateWithResultAsync(m).ConfigureAwait(false);
        r.IsAuthorizationError.Should().BeTrue();
    }).AssertSuccess());

    [Test]
    public void Create_Duplicate_Id() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var m = new TestTable
        {
            Id = 2.ToGuid(),
            Flag = true
        };

        var r = await ef.Table.CreateWithResultAsync(m).ConfigureAwait(false);
        r.IsDuplicateError.Should().BeTrue();
    }).AssertSuccess());

    [Test]
    public void Create_Duplicate_Text() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var m = new TestTable
        {
            Id = Runtime.NewGuid(),
            Flag = true,
            Text = "Abc"
        };

        var r = await ef.Table.CreateWithResultAsync(m).ConfigureAwait(false);
        r.IsDuplicateError.Should().BeTrue();
    }).AssertSuccess());

    [Test]
    public void Create_Success() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        using var jd = JsonDocument.Parse("{\"Key\": \"Value\"}");
        var id = Runtime.NewGuid();

        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var m = new TestTable
        {
            Id = id,
            Text = "New",
            Number = 999,
            Amount = 12.34m,
            Flag = true,
            Date = new DateOnly(2024, 7, 1),
            Time = new TimeOnly(10, 20, 30),
            Json = jd.RootElement.Clone()
        };

        var created = await ef.Table.CreateAsync(m).ConfigureAwait(false);

        created.Should().NotBeNull();
        created.Value.Id.Should().Be(id);
        created.Value.Text.Should().Be("New");
        created.Value.Number.Should().Be(999);
        created.Value.Amount.Should().Be(12.34m);
        created.Value.Flag.Should().BeTrue();
        created.Value.Date.Should().Be(new DateOnly(2024, 7, 1));
        created.Value.Time.Should().Be(new TimeOnly(10, 20, 30));
        created.Value.Json.Should().NotBeNull().And.Subject.ToString().Should().Be("{\"Key\": \"Value\"}");
        created.Value.ETag.Should().NotBeNull();
        created.Value.TenantId.Should().Be("A");
        created.Value.CreatedBy.Should().NotBeNull();
        created.Value.CreatedOn.Should().NotBeNull();
        created.Value.UpdatedBy.Should().BeNull();
        created.Value.UpdatedOn.Should().BeNull();

        // Verify can be retrieved and is same.
        m = await ef.Table.GetAsync(id).ConfigureAwait(false);
        ObjectComparer.Assert(created.Value, m);
    }).AssertSuccess());

    [Test]
    public void Create_Mapped_Success() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var id = Runtime.NewGuid();
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var v = new TestTableDto
        {
            Id = id,
            Text = "Val",
            Number = 999,
            Amount = 12.34m,
            Date = new DateOnly(2024, 7, 1),
            Time = new TimeOnly(10, 20, 30),
            Key = new TestTableDto.KeyValueDto { Key = "Value" }
        };

        var created = await ef.TableDto.CreateAsync(v).ConfigureAwait(false);

        created.Should().NotBeNull();
        created.Value.Id.Should().Be(id);
        created.Value.Text.Should().Be("Val");
        created.Value.Number.Should().Be(999);
        created.Value.Amount.Should().Be(12.34m);
        created.Value.Date.Should().Be(new DateOnly(2024, 7, 1));
        created.Value.Time.Should().Be(new TimeOnly(10, 20, 30));
        created.Value.Key.Should().NotBeNull();
        created.Value.Key.Key.Should().Be("Value");
        created.Value.ETag.Should().NotBeNullOrEmpty();
        created.Value.ChangeLog.Should().NotBeNull();
        created.Value.ChangeLog.CreatedBy.Should().NotBeNull();
        created.Value.ChangeLog.CreatedOn.Should().NotBeNull();
        created.Value.ChangeLog.UpdatedBy.Should().BeNull();
        created.Value.ChangeLog.UpdatedOn.Should().BeNull();

        // Verify can be retrieved and is same.
        v = await ef.TableDto.GetAsync(id).ConfigureAwait(false);
        ObjectComparer.Assert(created.Value, v);
    }).AssertSuccess());
}