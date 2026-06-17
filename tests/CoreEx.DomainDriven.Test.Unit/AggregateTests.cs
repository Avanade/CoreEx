using CoreEx.Entities;
using CoreEx.Events;

namespace CoreEx.DomainDriven.Test.Unit;

[TestFixture]
public class AggregateTests
{
    public sealed class TestEntity(int id) : Aggregate<int, TestEntity>(id)
    {
        // Expose wrappers to call protected base methods for testing.
        public TestEntity PublicSetPersistenceState(PersistenceState state) => SetPersistenceState(state);
        public TestEntity PublicMakeReadOnly() => MakeReadOnly();
        public TestEntity PublicSetChangeLog(ChangeLog? changeLog) => SetChangeLog(changeLog);
        public TestEntity PublicSetETag(string? eTag) => SetETag(eTag);
        public TestEntity PublicAddEvent(EventData eventData) => AddEvent(eventData);
        public TestEntity PublicClearEvents() => ClearEvents();

        // Expose wrappers to call protected modification helpers.
        public void PublicModify(Action? action = null) => Modify(action);
        public TResult PublicModify<TResult>(Func<TResult> func) => Modify(func);
        public void PublicModifyAndMakeReadOnly(Action? action = null) => ModifyAndMakeReadOnly(action);
        public TResult PublicModifyAndMakeReadOnly<TResult>(Func<TResult> func) => ModifyAndMakeReadOnly(func);

        public void PublicRemove(Action? action = null) => Remove(action);
    }

    [Test]
    public void Constructor_SetsId_And_Defaults()
    {
        var e = new TestEntity(42);

        e.Id.Should().Be(42);
        e.IsReadOnly.Should().BeFalse();
        e.PersistenceState.Should().Be(PersistenceState.Unknown);
        e.ChangeLog.Should().BeNull();
        e.ETag.Should().BeNull();
    }

    [Test]
    public void SetPersistenceState_ValidTransitions()
    {
        var e = new TestEntity(1);

        e.PublicSetPersistenceState(PersistenceState.NotModified);
        e.PersistenceState.Should().Be(PersistenceState.NotModified);

        e.PublicSetPersistenceState(PersistenceState.Modified);
        e.PersistenceState.Should().Be(PersistenceState.Modified);

        e.PublicSetPersistenceState(PersistenceState.Removed);
        e.PersistenceState.Should().Be(PersistenceState.Removed);
    }

    [Test]
    public void SetPersistenceState_InvalidTransitions_Throw()
    {
        var e = new TestEntity(1);
        e.PublicSetPersistenceState(PersistenceState.New);

        // Unknown is invalid target
        Action actUnknown = () => e.PublicSetPersistenceState(PersistenceState.Unknown);
        actUnknown.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be set to 'Unknown'*");

        // NotModified only allowed from Unknown
        Action actNotModifiedAgain = () => e.PublicSetPersistenceState(PersistenceState.NotModified);
        actNotModifiedAgain.Should().Throw<ArgumentException>()
            .WithMessage("*can only be set to 'NotModified' from 'Unknown'*");
    }

    [Test]
    public void MakeReadOnly_SetsFlag()
    {
        var e = new TestEntity(1);

        e.IsReadOnly.Should().BeFalse();
        e.PublicMakeReadOnly();
        e.IsReadOnly.Should().BeTrue();
    }

    [Test]
    public void CheckReadOnly_Throws_When_ReadOnly()
    {
        var e = new TestEntity(1);
        e.PublicMakeReadOnly();

        Action act = () => e.PublicModify();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage(EntityBase.ReadOnlyErrorMessage);
    }

    [Test]
    public void Modify_SetsModified_When_NotModified()
    {
        var e = new TestEntity(1);

        e.PublicSetPersistenceState(PersistenceState.NotModified);
        e.PersistenceState.Should().Be(PersistenceState.NotModified);

        e.PublicModify(); // no-op.

        e.PersistenceState.Should().Be(PersistenceState.Modified);
    }

    [Test]
    public void Modify_DoesNotChangeState_When_AlreadyModified()
    {
        var e = new TestEntity(1);

        e.PublicSetPersistenceState(PersistenceState.Modified);
        e.PublicModify(); // no-op.

        e.PersistenceState.Should().Be(PersistenceState.Modified);
    }

    [Test]
    public void Modify_Function_ReturnsValue_And_SetsModified()
    {
        var e = new TestEntity(1);
        e.PublicSetPersistenceState(PersistenceState.NotModified);

        var result = e.PublicModify(() => "ok");
        result.Should().Be("ok");

        e.PersistenceState.Should().Be(PersistenceState.Modified);
    }

    [Test]
    public void Modify_Function_Throws_When_Func_Is_Null()
    {
        var e = new TestEntity(1);

        Action act = () => e.PublicModify<string>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void ModifyAndMakeReadOnly_SetsModified_Then_ReadOnly()
    {
        var e = new TestEntity(1);
        e.PublicSetPersistenceState(PersistenceState.NotModified);

        e.PublicModifyAndMakeReadOnly(); // no-op.

        e.PersistenceState.Should().Be(PersistenceState.Modified);
        e.IsReadOnly.Should().BeTrue();
    }

    [Test]
    public void ModifyAndMakeReadOnly_Function_ReturnsValue_And_SetsModified_Then_ReadOnly()
    {
        var e = new TestEntity(1);
        e.PublicSetPersistenceState(PersistenceState.NotModified);

        var result = e.PublicModifyAndMakeReadOnly(() => 123);
        result.Should().Be(123);

        e.PersistenceState.Should().Be(PersistenceState.Modified);
        e.IsReadOnly.Should().BeTrue();
    }

    [Test]
    public void Remove_SetsRemoved_And_ReadOnly()
    {
        var e = new TestEntity(1);

        e.PublicRemove();

        e.PersistenceState.Should().Be(PersistenceState.Removed);
        e.IsReadOnly.Should().BeTrue();
    }

    [Test]
    public void SetChangeLog_Bypasses_ReadOnly_And_DoesNotChangeState()
    {
        var e = new TestEntity(1);
        e.PublicSetPersistenceState(PersistenceState.NotModified);
        e.PublicMakeReadOnly();

        var cl = new ChangeLog { UpdatedBy = "user", UpdatedOn = DateTime.UtcNow };

        e.PublicSetChangeLog(cl);

        e.ChangeLog.Should().Be(cl);
        e.PersistenceState.Should().Be(PersistenceState.NotModified);
        e.IsReadOnly.Should().BeTrue();
    }

    [Test]
    public void SetETag_Bypasses_ReadOnly_And_DoesNotChangeState()
    {
        var e = new TestEntity(1);
        e.PublicSetPersistenceState(PersistenceState.NotModified);
        e.PublicMakeReadOnly();

        e.PublicSetETag("etag-1");

        e.ETag.Should().Be("etag-1");
        e.PersistenceState.Should().Be(PersistenceState.NotModified);
        e.IsReadOnly.Should().BeTrue();
    }

    [Test]
    public void Equality_Compares_By_Id()
    {
        var e1 = new TestEntity(100);
        var e2 = new TestEntity(100);
        var e3 = new TestEntity(101);

        e1.Equals(e2).Should().BeTrue("same Id implies equality per DDD");
        e1.Equals(e3).Should().BeFalse();
        e1.Equals((TestEntity?)null).Should().BeFalse();

        (e1 == e2).Should().BeTrue();
        (e1 == e3).Should().BeFalse();
        (e1 != e2).Should().BeFalse();
        (e1 != e3).Should().BeTrue();
    }

    [Test]
    public void ToString_Delegates_To_IEntityKey()
    {
        var e = new TestEntity(7);
        var s = e.ToString();

        s.Should().Be("7");
    }

    [Test]
    public void Create_And_Clear_Events()
    {
        var e = new TestEntity(7);

        // No events by default.
        e.HasEvents.Should().BeFalse();

        // Add event.
        e.PublicAddEvent(EventData.CreateEvent("test-event", "emitted"));
        e.HasEvents.Should().BeTrue();
        e.Events.Should().HaveCount(1);

        // Clear events.
        e.PublicClearEvents();
        e.HasEvents.Should().BeFalse();
    }
}