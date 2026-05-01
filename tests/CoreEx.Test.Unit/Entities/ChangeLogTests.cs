using CoreEx.Entities;

namespace CoreEx.Test.Unit.Entities;

internal class ChangeLogTests
{
    [Test]
    public void CreateCreated_ShouldSetCreatedByAndCreatedDate()
    {
        var changeLog = ChangeLog.CreateCreated();

        changeLog.IsDefault().Should().BeFalse();
        changeLog.CreatedBy.Should().NotBeNullOrEmpty();
        changeLog.CreatedOn.Should().NotBeNull();
        changeLog.UpdatedBy.Should().BeNull();
        changeLog.UpdatedOn.Should().BeNull();
    }

    [Test]
    public void CreateChanged_ShouldCopyCreatedAndSetUpdated()
    {
        var created = ChangeLog.CreateCreated();
        var changed = ChangeLog.CreateChanged(created);

        changed.IsDefault().Should().BeFalse();
        changed.CreatedBy.Should().Be(created.CreatedBy);
        changed.CreatedOn.Should().Be(created.CreatedOn);
        changed.UpdatedBy.Should().NotBeNullOrEmpty();
        changed.UpdatedOn.Should().NotBeNull();
    }

    [Test]
    public void Clone_ShouldReturnEqualButNotSameInstance()
    {
        var changeLog = ChangeLog.CreateCreated();
        var clone = changeLog with { };

        clone.IsDefault().Should().BeFalse();
        clone.Should().BeEquivalentTo(changeLog);
        clone.Should().NotBeSameAs(changeLog);
    }

    [Test]
    public void New_IsDefaultAndAutoClean()
    {
        var changeLog = new ChangeLog();
        changeLog.CreatedBy.Should().BeNull();
        changeLog.CreatedOn.Should().BeNull();
        changeLog.UpdatedBy.Should().BeNull();
        changeLog.UpdatedOn.Should().BeNull();
        changeLog.IsDefault().Should().BeTrue();

        changeLog = new ChangeLog() { CreatedBy = "", UpdatedBy = "" };
        changeLog.CreatedBy.Should().BeNull();
        changeLog.CreatedOn.Should().BeNull();
        changeLog.UpdatedBy.Should().BeNull();
        changeLog.UpdatedOn.Should().BeNull();
        changeLog.IsDefault().Should().BeTrue();
    }

    [Test]
    public void With_ExpressionSupport()
    {
        var created = ChangeLog.CreateCreated();
        var changeLog = ChangeLog.CreateChanged(created);

        var changed = changeLog with { UpdatedBy = "NewUser" };

        changed.UpdatedBy.Should().Be("NewUser");
        changed.CreatedBy.Should().Be(created.CreatedBy);
        changed.CreatedOn.Should().Be(created.CreatedOn);
        changed.UpdatedBy.Should().NotBeNullOrEmpty();
        changed.UpdatedOn.Should().NotBeNull();
    }
}