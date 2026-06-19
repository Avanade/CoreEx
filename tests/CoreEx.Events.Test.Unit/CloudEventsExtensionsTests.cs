using CoreEx.Entities;

namespace CoreEx.Events.Test.Unit;

[TestFixture]
public class CloudEventsExtensionsTests
{
    [Test]
    public void EncodeToBinaryData_Structured()
    {
        var ed = new EventData().WithValue(new Product { Id = "abc", Name = "A blue carrot" }).WithAction("Created");
        var ef = new EventFormatter { SourceBaseUri = new Uri("urn:Test"), DomainName = "CoreEx" };
        
        var ce = ef.ConvertToCloudEvent(ef.Format(ed));
        var bd = ce.EncodeToBinaryData(CloudNative.CloudEvents.ContentMode.Structured);
        bd.Should().NotBeNull();
        bd.MediaType.Should().Be("application/cloudevents+json; charset=utf-8");

        var ce2 = bd.DecodeToCloudEvent(CloudNative.CloudEvents.ContentMode.Structured);
        ce2.Should().NotBeNull();
        var ed2 = ef.Parse(ef.ConvertFromCloudEvent(ce2));
        ed2.Should().NotBeNull();

        // The casing will have been changed during formatting and cannot be returned to original.
        ed2.DomainName.Should().Be("coreex");
        ed2.Entity.Should().Be("product");
        ed2.Action.Should().Be("created");

        ObjectComparer.Assert(ed.Data, ed2.Data);
    }

    [Test]
    public void EncodeToBinaryData_Binary()
    {
        var ed = new EventData().WithValue(new Product { Id = "abc", Name = "A blue carrot" }).WithAction("Created");
        var ef = new EventFormatter { SourceBaseUri = new Uri("urn:Test"), DomainName = "CoreEx" };

        var ce = ef.ConvertToCloudEvent(ef.Format(ed));
        var bd = ce.EncodeToBinaryData(CloudNative.CloudEvents.ContentMode.Binary);
        bd.Should().NotBeNull();
        bd.ToString().Should().Be("""{"id":"abc","name":"A blue carrot"}""");

        var ce2 = bd.DecodeToCloudEvent(CloudNative.CloudEvents.ContentMode.Binary);
        ce2.Should().NotBeNull();
        var ed2 = ef.Parse(ef.ConvertFromCloudEvent(ce2));
        ed2.Should().NotBeNull();

        // The non-data properties are not included in DataJson format and so will be null.
        ed2.DomainName.Should().BeNull();
        ed2.Entity.Should().BeNull();
        ed2.Action.Should().BeNull();

        ObjectComparer.Assert(ed.Data, ed2.Data);
    }

    //[Test]
    //public void EncodeToBinaryData_FullBinary()
    //{
    //    var ed = new EventData().WithValue(new Product { Id = "abc", Name = "A blue carrot" }).WithAction("Created");
    //    var ef = new EventFormatter { SourceBaseUri = new Uri("urn:Test"), DomainName = "CoreEx" };

    //    var ce = ef.ConvertToCloudEvent(ef.Format(ed));
    //    var bd = ce.EncodeToBinaryData(DataContentFormat.FullBinary);
    //    bd.Should().NotBeNull();

    //    var ce2 = bd.DecodeToCloudEvent(DataContentFormat.FullBinary);
    //    ce2.Should().NotBeNull();
    //    var ed2 = ef.Parse(ef.ConvertFromCloudEvent(ce2));
    //    ed2.Should().NotBeNull();

    //    // The casing will have been changed during formatting and cannot be returned to original.
    //    ed2.DomainName.Should().Be("coreex");
    //    ed2.Entity.Should().Be("product");
    //    ed2.Action.Should().Be("created");

    //    ObjectComparer.Assert(ed.Data, ed2.Data);
    //}

    //[Test]
    //public void EncodeToBinaryData_DataBinary()
    //{
    //    var ed = new EventData().WithValue(new Product { Id = "abc", Name = "A blue carrot" }).WithAction("Created");
    //    var ef = new EventFormatter { SourceBaseUri = new Uri("urn:Test"), DomainName = "CoreEx" };

    //    var ce = ef.ConvertToCloudEvent(ef.Format(ed));
    //    var bd = ce.EncodeToBinaryData(DataContentFormat.DataBinary);
    //    bd.Should().NotBeNull();

    //    var ce2 = bd.DecodeToCloudEvent(DataContentFormat.DataBinary);
    //    ce2.Should().NotBeNull();
    //    var ed2 = ef.Parse(ef.ConvertFromCloudEvent(ce2));
    //    ed2.Should().NotBeNull();

    //    // The non-data properties are not included in DataJson format and so will be null.
    //    ed2.DomainName.Should().BeNull();
    //    ed2.Entity.Should().BeNull();
    //    ed2.Action.Should().BeNull();

    //    ObjectComparer.Assert(ed.Data, ed2.Data);
    //}

    [Test]
    public void EncodeToJsonElement_And_Back_Again()
    {
        var ed = new EventData().WithValue(new Product { Id = "abc", Name = "A blue carrot" }).WithAction("Created");
        var ef = new EventFormatter { SourceBaseUri = new Uri("urn:Test"), DomainName = "coreex" };

        var ce = ef.ConvertToCloudEvent(ef.Format(ed));
        var je = ce.EncodeToJsonElement();
        je.Should().NotBeNull();

        Console.WriteLine(je.ToString());

        var ce2 = je.DecodeToCloudEvent();
        ce2.Should().NotBeNull();
        var ed2 = ef.Parse(ef.ConvertFromCloudEvent(ce2));
        ed2.Should().NotBeNull();

        // The casing will have been changed during formatting and cannot be returned to original.
        ed2.DomainName.Should().Be("coreex");
        ed2.Entity.Should().Be("product");
        ed2.Action.Should().Be("created");

        ObjectComparer.Assert(ed.Data, ed2.Data);
    }

    private class Product : IIdentifier<string?>
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
    }
}