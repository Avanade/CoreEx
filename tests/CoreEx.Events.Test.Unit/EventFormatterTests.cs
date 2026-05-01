using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Extensions;
using CoreEx.Hosting;
using CoreEx.Security;

namespace CoreEx.Events.Test.Unit;

[TestFixture]
public class EventFormatterTests
{
    [Test]
    public void Property_GetSet_All()
    {
        var ef = new EventFormatter
        {
            TraceParentAttributeName = "tp",
            TraceStateAttributeName = "ts",
            TraceBaggageAttributeName = "tb",
            TenantIdAttributeName = "tid",
            DataSchemaVersionAttributeName = "dsv",
            AuthTypeAttributeName = "at",
            AuthIdAttributeName = "aid",
            ReplyToAttributeName = "rt",
            TitlePrefix = "prefix",
            SourceBaseUri = new Uri("https://base/"),
            DomainName = "dn"
        };

        ef.TraceParentAttributeName.Should().Be("tp");
        ef.TraceStateAttributeName.Should().Be("ts");
        ef.TraceBaggageAttributeName.Should().Be("tb");
        ef.TenantIdAttributeName.Should().Be("tid");
        ef.DataSchemaVersionAttributeName.Should().Be("dsv");
        ef.AuthTypeAttributeName.Should().Be("at");
        ef.AuthIdAttributeName.Should().Be("aid");
        ef.ReplyToAttributeName.Should().Be("rt");
        ef.TitlePrefix.Should().Be("prefix");
        ef.SourceBaseUri.Should().Be(new Uri("https://base/"));
        ef.DomainName.Should().Be("dn");
    }

    [Test]
    public void Format_TitleAndSource()
    {
        var ef = new EventFormatter { TitlePrefix = "pre", SourceBaseUri = new Uri("https://base/"), DomainName = "dom", PartitionKeyIsRequired = false };
        var ed = new EventData
        {
            DomainName = "dom",
            Entity = "ent",
            Action = "act",
            DataSchemaVersion = new Version(2, 3),
            TenantId = "tenant"
        };

        var result = ef.Format(ed);
        result.Title.Should().Be("pre.dom.ent.act.v2");
        result.Source.Should().Be(new Uri("https://base/tenant"));
    }

    [Test]
    public void Format_Title_From_HostSettings()
    {
        var hs = new HostSettings { SolutionName = "Coreex.Test", DomainName = "Dom", EnvironmentName = "Env" };
        var ef = new EventFormatter(hs) { PartitionKeyIsRequired = false };
        var ed = new EventData
        {
            Entity = "Ent",
            Action = "Act",
            DataSchemaVersion = new Version(1, 0)
        };

        var result = ef.Format(ed);
        result.Title.Should().Be("coreex.test.dom.ent.act.v1");
        result.Source.Should().Be(new Uri("urn:coreex:test:dom"));
    }

    [Test]
    public void Parse_Title_Full()
    {
        var ef = new EventFormatter { TitlePrefix = "pre" };
        var ed = new EventData { Title = "pre.dom.ent.act.v2" };
        var result = ef.Parse(ed);
        result.DomainName.Should().Be("dom");
        result.Entity.Should().Be("ent");
        result.Action.Should().Be("act");
        result.DataSchemaVersion.Should().Be(new Version(2, 0));
    }

    [Test]
    public void Parse_Title_Partial()
    {
        var ef = new EventFormatter();
        var ed = new EventData { Title = "dom.ent.act" };
        var result = ef.Parse(ed);
        result.DomainName.Should().Be("dom");
        result.Entity.Should().Be("ent");
        result.Action.Should().Be("act");
        result.DataSchemaVersion.Should().BeNull();
    }

    [Test]
    public void Convert_EventData_To_CloudEvent_AndBack()
    {
        var ef = new EventFormatter();
        var ed = new EventData
        {
            Id = "id1",
            Timestamp = DateTimeOffset.UtcNow,
            DomainName = "dom",
            Entity = "ent",
            Action = "act",
            Key = "k1",
            PartitionKey = "pk1",
            ReplyTo = "reply",
            UserType = AuthenticationType.AccountUser,
            UserId = "user1",
            TraceParent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01",
            TraceState = "ts",
            TraceBaggage = [new KeyValuePair<string, string?>("foo", "bar")],
            DataSchemaVersion = new Version(1, 0),
            DataSchema = new Uri("https://schema"),
            Data = new BinaryData("abc"),
            Title = "dom.ent.act.v1",
            Source = new Uri("https://src")
        };
        ed.SetAttribute("foo", "bar");
        ed.SetAttribute("_internal", "shouldnotcopy");

        var ce = ef.ConvertToCloudEvent(ed);
        ce.Id.Should().Be(ed.Id);
        ce.Type.Should().Be(ed.Title);
        ce.Source.Should().Be(ed.Source);
        ce.Subject.Should().Be(ed.Key);
        ce.Time.Should().Be(ed.Timestamp);
        ce.DataContentType.Should().Be(ed.Data!.MediaType);
        ce.DataSchema.Should().Be(ed.DataSchema);
        ce.Data.Should().Be(ed.Data);
        ce[ef.TenantIdAttributeName].Should().Be(ed.TenantId);
        ce.GetPartitionKey().Should().Be(ed.PartitionKey);
        ce[ef.ReplyToAttributeName].Should().Be(ed.ReplyTo);
        ce[ef.AuthTypeAttributeName].Should().Be("user");
        ce[ef.AuthIdAttributeName].Should().Be(ed.UserId);
        ce[ef.TraceParentAttributeName].Should().Be(ed.TraceParent);
        ce[ef.TraceStateAttributeName].Should().Be(ed.TraceState);
        ce[ef.TraceBaggageAttributeName].Should().Be("foo=bar");
        ce[ef.DataSchemaVersionAttributeName].Should().Be(ed.DataSchemaVersion.ToString());
        ce["foo"].Should().Be("bar");
        ce["_internal"].Should().BeNull();

        // Convert back
        var ed2 = ef.ConvertFromCloudEvent(ce);
        ed2.Id.Should().Be(ed.Id);
        ed2.Timestamp.Should().Be(ed.Timestamp);
        ed2.DataSchema.Should().Be(ed.DataSchema);
        ed2.Key.Should().Be(ed.Key);
        ed2.PartitionKey.Should().Be(ed.PartitionKey);
        ed2.DataSchemaVersion.Should().Be(ed.DataSchemaVersion);
        ed2.TenantId.Should().Be(ed.TenantId);
        ed2.ReplyTo.Should().Be(ed.ReplyTo);
        ed2.UserType.Should().Be(ed.UserType);
        ed2.UserId.Should().Be(ed.UserId);
        ed2.TraceParent.Should().Be(ed.TraceParent);
        ed2.TraceState.Should().Be(ed.TraceState);
        ed2.TraceBaggage.Should().BeEquivalentTo(ed.TraceBaggage);
        ed2.Source.Should().Be(ed.Source);
        ed2.Title.Should().Be(ed.Title);
        ed2.Attributes.Should().ContainKey("foo");
        ed2.Attributes.Should().NotContainKey("_internal");
    }

    [Test]
    public void Convert_CloudEvent_DataNotBinary_Throws()
    {
        var ef = new EventFormatter();
        var ce = new CloudEvent
        {
            Id = "id",
            Data = "not-binary"
        };
        Action act = () => ef.ConvertFromCloudEvent(ce);
        act.Should().Throw<NotSupportedException>();
    }

    [Test]
    public void AddTracing_SetsTraceParentAndState()
    {
        var ef = new EventFormatter();
        var ce = new CloudEvent { Id = "id" };
        ef.AddTracing(ce, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01", "vendor=prop");
        ce[ef.TraceParentAttributeName].Should().Be("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
        ce[ef.TraceStateAttributeName].Should().Be("vendor=prop");
        ce[ef.TraceBaggageAttributeName].Should().BeNull();

        ce = new CloudEvent { Id = "id" };
        ef.AddTracing(ce, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01", "vendor=prop", [new KeyValuePair<string, string?>("foo", "bar")]);
        ce[ef.TraceParentAttributeName].Should().Be("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
        ce[ef.TraceStateAttributeName].Should().Be("vendor=prop");
        ce[ef.TraceBaggageAttributeName].Should().Be("foo=bar");
    }

    [Test]
    public void AddTracing_DoesNotOverwriteExisting()
    {
        var ef = new EventFormatter();
        var ce = new CloudEvent { Id = "id" };
        ce.SetExtensionAttribute(ef.TraceParentAttributeName, "existing");
        ef.AddTracing(ce, "tp", "ts");
        ce[ef.TraceParentAttributeName].Should().Be("existing");
    }

    [TestCase(AuthenticationType.Unknown, "unknown")]
    [TestCase(AuthenticationType.Unauthenticated, "unauthenticated")]
    [TestCase(AuthenticationType.ApplicationUser, "app_user")]
    [TestCase(AuthenticationType.AccountUser, "user")]
    [TestCase(AuthenticationType.SystemUser, "system")]
    public void ConvertFromAuthenticationType_And_ConvertToAuthenticationType(AuthenticationType type, string expected)
    {
        var ef = new EventFormatter();
        var methodFrom = ef.GetType().GetMethod("ConvertFromAuthenticationType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var methodTo = ef.GetType().GetMethod("ConvertToAuthenticationType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var str = (string?)methodFrom!.Invoke(ef, [type]);
        str.Should().Be(expected);

        var at = (AuthenticationType?)methodTo!.Invoke(ef, [expected]);
        at.Should().Be(type);
    }

    [Test]
    public void ConvertFromAuthenticationType_Invalid_Throws()
    {
        var ef = new EventFormatter();
        var method = ef.GetType().GetMethod("ConvertFromAuthenticationType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Action act = () => method!.Invoke(ef, [(AuthenticationType)999]);
        act.Should().Throw<Exception>();
    }

    [Test]
    public void ConvertToAuthenticationType_Invalid_Throws()
    {
        var ef = new EventFormatter();
        var method = ef.GetType().GetMethod("ConvertToAuthenticationType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Action act = () => method!.Invoke(ef, ["badtype"]);
        act.Should().Throw<Exception>();
    }
}