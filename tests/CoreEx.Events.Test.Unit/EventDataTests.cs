using CoreEx.Data;
using CoreEx.Entities;
using CoreEx.Security;

namespace CoreEx.Events.Test.Unit;

[TestFixture]
public class EventDataTests
{
    [Test]
    public void Property_GetSet_All()
    {
        var dt = DateTimeOffset.UtcNow;
        var uri = new Uri("https://test");
        var ver = new Version(1, 2, 3, 4);

        var ed = new EventData
        {
            Id = "id1",
            Timestamp = dt,
            DomainName = "DOMAIN",
            TenantId = "TENANT",
            Entity = "ENTITY",
            Action = "ACTION",
            Key = "key1",
            TraceParent = "tp",
            TraceState = "ts",
            UserType = AuthenticationType.AccountUser,
            UserId = "user1",
            Data = new BinaryData("abc"),
            DataSchema = uri,
            DataSchemaVersion = ver,
            PartitionKey = "pk",
            ReplyTo = "reply",
            Title = "title",
            Source = uri
        };

        ed.Id.Should().Be("id1");
        ed.Timestamp.Should().Be(dt);
        ed.DomainName.Should().Be("DOMAIN");
        ed.TenantId.Should().Be("TENANT");
        ed.Entity.Should().Be("ENTITY");
        ed.Action.Should().Be("ACTION");
        ed.Key.Should().Be("key1");
        ed.TraceParent.Should().Be("tp");
        ed.TraceState.Should().Be("ts");
        ed.UserType.Should().Be(AuthenticationType.AccountUser);
        ed.UserId.Should().Be("user1");
        ed.Data!.ToString().Should().Be("abc");
        ed.DataSchema.Should().Be(uri);
        ed.DataSchemaVersion.Should().Be(ver);
        ed.PartitionKey.Should().Be("pk");
        ed.ReplyTo.Should().Be("reply");
        ed.Title.Should().Be("title");
        ed.Source.Should().Be(uri);
    }

    [Test]
    public void SetAttribute_And_TryGetAttribute()
    {
        var ed = new EventData();
        ed.SetAttribute("foo", 123);
        ed.SetAttribute("bar", "baz");

        ed.Attributes.Should().ContainKey("foo");
        ed.Attributes.Should().ContainKey("bar");
        ed.Attributes["foo"].Should().Be(123);
        ed.Attributes["bar"].Should().Be("baz");

        ed.TryGetAttribute<int>("foo", out var v1).Should().BeTrue();
        v1.Should().Be(123);

        ed.TryGetAttribute<string>("bar", out var v2).Should().BeTrue();
        v2.Should().Be("baz");

        ed.TryGetAttribute<string>("notfound", out var v3).Should().BeFalse();
        v3.Should().BeNull();
    }

    [Test]
    public void WithEntity_SetsEntity()
    {
        var ed = new EventData().WithEntity("Order");
        ed.Entity.Should().Be("Order");
    }

    [Test]
    public void WithAction_String_SetsAction()
    {
        var ed = new EventData().WithAction("Created");
        ed.Action.Should().Be("Created");
    }

    private enum TestAction { Created, Updated }

    [Test]
    public void WithAction_Enum_SetsAction()
    {
        var ed = new EventData().WithAction(TestAction.Updated);
        ed.Action.Should().Be("Updated");
    }

    [Test]
    public void WithKey_SetsKey()
    {
        var ed = new EventData().WithKey("123");
        ed.Key.Should().Be("123");
    }

    [Test]
    public void WithVersion_SetsDataSchemaVersion()
    {
        var v = new Version(1, 2, 3);
        var ed = new EventData().WithVersion(v);
        ed.DataSchemaVersion.Should().Be(v);
    }

    [Test]
    public void WithUser_SetsUserTypeAndUserId()
    {
        var user = new Security.AuthenticationUser { Type = AuthenticationType.AccountUser, Id = "u1", UserName = "user1" };
        var ed = new EventData().WithUser(user);
        ed.UserType.Should().Be(AuthenticationType.AccountUser);
        ed.UserId.Should().Be("u1");
    }

    [Test]
    public void WithSchema_SetsDataSchema()
    {
        var uri = new Uri("https://schema");
        var ed = new EventData().WithSchema(uri);
        ed.DataSchema.Should().Be(uri);
    }

    [Test]
    public void WithTitle_SetsTitle()
    {
        var ed = new EventData().WithTitle("my.title");
        ed.Title.Should().Be("my.title");
    }

    [Test]
    public void WithValue_SetsDataAndEntity()
    {
        var obj = new TestValue { Id = "42", TenantId = "t1", PartitionKey = "pk1", ETag = "abc" };
        var ed = new EventData().WithValue(obj);
        ed.Data.Should().NotBeNull();
        ed.Data.MediaType.Should().Be(System.Net.Mime.MediaTypeNames.Application.Json);
        ed.Data.Length.Should().BeGreaterThan(0);
        ed.Entity.Should().Be(nameof(TestValue));
        ed.Key.Should().Be("42");
        ed.TenantId.Should().Be("t1");
        ed.PartitionKey.Should().Be("pk1");

        var json = ed.Data!.ToString();
        json.Should().Contain("abc");
        json.Length.Should().BeGreaterThan(0);
    }

    [Test]
    public void WithValue_Null_SetsDataNull()
    {
        var ed = new EventData().WithValue<TestValue>(null);
        ed.Data.Should().BeNull();
    }

    [Test]
    public void WithValue_ExcludePaths_ExcludesProperties()
    {
        var obj = new TestValue { Id = "42", TenantId = "t1", PartitionKey = "pk1", Extra = "should-exclude" };
        var ed = new EventData().WithValue(obj, "Extra");
        var json = ed.Data!.ToString();
        json.Should().NotContain("should-exclude");
    }

    [Test]
    public void Create_Event()
    {
        var ed = EventData.CreateEvent("Order", "Created");
        ed.Entity.Should().Be("Order");
        ed.Action.Should().Be("Created");
        ed.MessageType.Should().Be(MessageType.Event);
        ed.DomainName.Should().BeNull();

        ed = EventData.CreateEvent("Order", TestAction.Created);
        ed.Entity.Should().Be("Order");
        ed.Action.Should().Be("Created");
        ed.MessageType.Should().Be(MessageType.Event);
        ed.DomainName.Should().BeNull();

        ed = EventData.CreateEventWith(new TestValue { Id = "42", TenantId = "t1", PartitionKey = "pk1" }, "Created");
        ed.Data.Should().NotBeNull();
        ed.Data!.ToString().Should().Contain("\"id\":\"42\"");
        ed.Entity.Should().Be("TestValue");
        ed.Action.Should().Be("Created");
        ed.MessageType.Should().Be(MessageType.Event);
        ed.DomainName.Should().BeNull();

        ed = EventData.CreateEventWith(new TestValue { Id = "42", TenantId = "t1", PartitionKey = "pk1" }, TestAction.Created);
        ed.Data.Should().NotBeNull();
        ed.Data!.ToString().Should().Contain("\"id\":\"42\"");
        ed.Entity.Should().Be("TestValue");
        ed.Action.Should().Be("Created");
        ed.MessageType.Should().Be(MessageType.Event);
        ed.DomainName.Should().BeNull();
    }

    [Test]
    public void Create_Command()
    {
        var ed = EventData.CreateCommand("Test", "Order", "Created");
        ed.Entity.Should().Be("Order");
        ed.Action.Should().Be("Created");
        ed.MessageType.Should().Be(MessageType.Command);
        ed.DomainName.Should().Be("Test");

        ed = EventData.CreateCommand("Test", "Order", TestAction.Created);
        ed.Entity.Should().Be("Order");
        ed.Action.Should().Be("Created");
        ed.MessageType.Should().Be(MessageType.Command);
        ed.DomainName.Should().Be("Test");

        ed = EventData.CreateCommandWith("Test", new TestValue { Id = "42", TenantId = "t1", PartitionKey = "pk1" }, "Created");
        ed.Data.Should().NotBeNull();
        ed.Data!.ToString().Should().Contain("\"id\":\"42\"");
        ed.Entity.Should().Be("TestValue");
        ed.Action.Should().Be("Created");
        ed.MessageType.Should().Be(MessageType.Command);
        ed.DomainName.Should().Be("Test");

        ed = EventData.CreateCommandWith("Test", new TestValue { Id = "42", TenantId = "t1", PartitionKey = "pk1" }, TestAction.Created);
        ed.Data.Should().NotBeNull();
        ed.Data!.ToString().Should().Contain("\"id\":\"42\"");
        ed.Entity.Should().Be("TestValue");
        ed.Action.Should().Be("Created");
        ed.MessageType.Should().Be(MessageType.Command);
        ed.DomainName.Should().Be("Test");
    }

    [Test]
    public void ToObjectFromJson_T_ReturnsDeserializedObject()
    {
        var obj = new TestValue { Id = "42", TenantId = "t1", PartitionKey = "pk1", ETag = "abc" };
        var ed = new EventData().WithValue(obj);

        var obj2 = ed.ToObjectFromJson<TestValue>();
        obj2.Should().NotBeNull();
        obj2.Id.Should().Be(obj.Id);
        obj2.TenantId.Should().Be(obj.TenantId);
        obj2.PartitionKey.Should().Be(obj.PartitionKey);
        obj2.ETag.Should().Be("abc");
    }

    [Test]
    public void ToObjectFromJson_T_ReturnsDeserializedObject_Type()
    {
        var obj = new TestValue { Id = "42", TenantId = "t1", PartitionKey = "pk1", ETag = "abc" };
        var ed = new EventData().WithValue(obj);

        TestValue? obj2 = ed.ToObjectFromJson<TestValue>();
        obj2.Should().NotBeNull();
        obj2.Id.Should().Be(obj.Id);
        obj2.TenantId.Should().Be(obj.TenantId);
        obj2.PartitionKey.Should().Be(obj.PartitionKey);
        obj2.ETag.Should().Be("abc");
    }

    private class TestValue : IIdentifier<string?>, IReadOnlyTenantId, IReadOnlyPartitionKey, IReadOnlyETag
    {
        public string? Id { get; set; }
        public string? TenantId { get; set; }
        public string? PartitionKey { get; set; }
        public string? Extra { get; set; }
        public string? ETag { get; set; }
    }
}