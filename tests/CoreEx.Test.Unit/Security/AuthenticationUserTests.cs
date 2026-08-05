using CoreEx.Entities;
using CoreEx.Security;

namespace CoreEx.Test.Unit.Security;

[TestFixture]
public class AuthenticationUserTests
{
    [TearDown]
    public void TearDown()
    {
        // These statics are settable; reset to defaults to avoid cross-test leakage.
        AuthenticationUser.Unknown = new AuthenticationUser { Type = AuthenticationType.Unknown, UserName = nameof(AuthenticationUser.Unknown) };
        AuthenticationUser.Anonymous = new AuthenticationUser { Type = AuthenticationType.Unauthenticated, UserName = nameof(AuthenticationUser.Anonymous) };
    }

    [Test]
    public void Unknown_HasExpectedDefaults()
    {
        AuthenticationUser.Unknown.Type.Should().Be(AuthenticationType.Unknown);
        AuthenticationUser.Unknown.UserName.Should().Be("Unknown");
        AuthenticationUser.Unknown.Id.Should().BeNull();
    }

    [Test]
    public void Anonymous_HasExpectedDefaults()
    {
        AuthenticationUser.Anonymous.Type.Should().Be(AuthenticationType.Unauthenticated);
        AuthenticationUser.Anonymous.UserName.Should().Be("Anonymous");
    }

    [Test]
    public void EnvironmentUser_HasExpectedDefaults()
    {
        AuthenticationUser.EnvironmentUser.Type.Should().Be(AuthenticationType.AccountUser);
        AuthenticationUser.EnvironmentUser.UserName.Should().NotBeNullOrEmpty();
        AuthenticationUser.EnvironmentUser.Id.Should().Be(AuthenticationUser.EnvironmentUser.UserName);
    }

    [Test]
    public void Statics_AreSettable_AndOverridable()
    {
        var custom = new AuthenticationUser { Type = AuthenticationType.SystemUser, UserName = "svc-account" };
        AuthenticationUser.Unknown = custom;

        AuthenticationUser.Unknown.Should().BeSameAs(custom);
    }

    [Test]
    public void ToString_ReturnsUserName()
    {
        var user = new AuthenticationUser { Type = AuthenticationType.AccountUser, UserName = "jdoe" };
        user.ToString().Should().Be("jdoe");
    }

    [Test]
    public void UserName_NullOrEmpty_Throws()
    {
        Action act = () => new AuthenticationUser { Type = AuthenticationType.AccountUser, UserName = null! };
        act.Should().Throw<ArgumentException>();

        Action act2 = () => new AuthenticationUser { Type = AuthenticationType.AccountUser, UserName = string.Empty };
        act2.Should().Throw<ArgumentException>();
    }

    [Test]
    public void RecordEquality_IsStructural()
    {
        var user1 = new AuthenticationUser { Type = AuthenticationType.AccountUser, Id = "1", UserName = "jdoe" };
        var user2 = new AuthenticationUser { Type = AuthenticationType.AccountUser, Id = "1", UserName = "jdoe" };
        var user3 = new AuthenticationUser { Type = AuthenticationType.AccountUser, Id = "2", UserName = "jdoe" };

        user1.Should().Be(user2);
        user1.Should().NotBe(user3);
    }

    [Test]
    public void IsReadOnlyIdentifier_ExposesIdAndEntityKey()
    {
        IReadOnlyIdentifier<string?> user = new AuthenticationUser { Type = AuthenticationType.AccountUser, Id = "abc", UserName = "jdoe" };

        user.Id.Should().Be("abc");
        ((IEntityKey)user).EntityKey.Should().Be(CompositeKey.Create("abc"));
    }

    [TestCase(AuthenticationType.Unknown)]
    [TestCase(AuthenticationType.Unauthenticated)]
    [TestCase(AuthenticationType.ApplicationUser)]
    [TestCase(AuthenticationType.AccountUser)]
    [TestCase(AuthenticationType.SystemUser)]
    public void AuthenticationType_AllValues_AssignableToUser(AuthenticationType type)
    {
        var user = new AuthenticationUser { Type = type, UserName = "test" };
        user.Type.Should().Be(type);
    }
}
