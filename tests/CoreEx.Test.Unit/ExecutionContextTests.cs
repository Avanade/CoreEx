using CoreEx.Entities;
using CoreEx.Localization;
using System.Globalization;

namespace CoreEx.Test.Unit;

[TestFixture]
public class ExecutionContextTests
{
    [TearDown]
    public void TearDown()
    {
        ExecutionContext.Reset();
        ExecutionContext.Create = () => new ExecutionContext();
    }

    [Test]
    public void ServiceProvider_GetSet()
    {
        var ec = new ExecutionContext();
        var sp = new TestServiceProvider();
        ec.ServiceProvider = sp;
        ec.ServiceProvider.Should().BeSameAs(sp);
    }

    [Test]
    public void UserName_GetSet()
    {
        var ec = new ExecutionContext
        {
            User = new Security.AuthenticationUser { Type = Security.AuthenticationType.AccountUser, UserName = "user1" }
        };
        ec.User.Should().NotBeNull();
        ec.User.UserName.Should().Be("user1");
        ec.User.Type.Should().Be(Security.AuthenticationType.AccountUser);
    }

    [Test]
    public void TenantId_GetSet()
    {
        var ec = new ExecutionContext
        {
            TenantId = "tenant1"
        };
        ec.TenantId.Should().Be("tenant1");
    }

    [Test]
    public void Timestamp_DefaultAndSet()
    {
        var ec = new ExecutionContext();
        var now = DateTimeOffset.UtcNow;
        ec.Timestamp.Should().BeOnOrAfter(now.AddSeconds(-1));
        var dt = new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero);
        ec.Timestamp = dt;
        ec.Timestamp.Should().Be(dt);
    }

    [Test]
    public void UICulture_GetSet()
    {
        var ec = new ExecutionContext();
        var culture = new CultureInfo("fr-FR");
        ec.UICulture = culture;
        ec.UICulture.Should().Be(culture);
    }

    [Test]
    public void AddWarningMessage_AddsMessage()
    {
        var ec = new ExecutionContext();
        ec.AddWarningMessage(new LText("warn"));
        ec.Messages.Should().NotBeNull();
        ec.Messages!.Count.Should().Be(1);
        ec.Messages[0].Type.Should().Be(MessageType.Warning);
        ec.Messages[0].Text.Should().Be(new LText("warn"));
    }

    [Test]
    public void AddInfoMessage_AddsMessage()
    {
        var ec = new ExecutionContext();
        ec.AddInfoMessage(new LText("info"));
        ec.Messages.Should().NotBeNull();
        ec.Messages!.Count.Should().Be(1);
        ec.Messages[0].Type.Should().Be(MessageType.Info);
        ec.Messages[0].Text.Should().Be(new LText("info"));
    }

    [Test]
    public void Attributes_IsLazilyCreatedAndWorks()
    {
        var ec = new ExecutionContext();
        ec.Attributes.Should().NotBeNull();
        ec.Attributes["foo"] = 123;
        ec.Attributes["foo"].Should().Be(123);
    }

    [Test]
    public void IsACopy_FalseByDefault_TrueWhenCopied()
    {
        var ec = new ExecutionContext();
        ec.IsACopy.Should().BeFalse();
        var copy = ec.CreateCopy();
        copy.IsACopy.Should().BeTrue();
    }

    [Test]
    public void CreateCopy_CopiesPropertiesAndSharesMessagesAndAttributes()
    {
        var ec = new ExecutionContext
        {
            User = new Security.AuthenticationUser { Type = Security.AuthenticationType.AccountUser, UserName = "user" },
            TenantId = "tenant",
            UICulture = new CultureInfo("en-US")
        };
        ec.AddInfoMessage(new LText("msg"));
        ec.Attributes["k"] = "v";
        var copy = ec.CreateCopy();
        copy.User.Should().NotBeNull();
        copy.User.UserName.Should().Be("user");
        copy.User.Type.Should().Be(Security.AuthenticationType.AccountUser);
        copy.TenantId.Should().Be("tenant");
        copy.UICulture.Should().Be(new CultureInfo("en-US"));
        copy.Messages.Should().BeSameAs(ec.Messages);
        copy.Attributes.Should().NotBeNull();
        copy.Attributes["k"].Should().Be("v");
    }

    [Test]
    public void CreateCopy_ThrowsIfCreateIsNull()
    {
        ExecutionContext.Create = null;
        var ec = new ExecutionContext();
        Action act = () => ec.CreateCopy();
        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void OperationType_Read()
    {
        var ec = new ExecutionContext { OperationType = OperationType.Get };
        ec.OperationType.IsRead.Should().BeTrue();
    }

    private class TestServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}