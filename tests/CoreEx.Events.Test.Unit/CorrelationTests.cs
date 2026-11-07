namespace CoreEx.Events.Test.Unit;

public class CorrelationTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void CorrelationAndCausation_RoundTripThroughStorage_ForBothRepresentations(bool native)
    {
        var value = EventData.CreateEventWith(new { Value = 1 }, "created").WithKey("key");
        if (native)
            value.AsNative();

        value.CorrelationId = "workflow";
        value.CausationId = "trigger";
        var formatter = new EventFormatter { SourceBaseUri = new Uri("urn:test") };
        var envelope = formatter.ConvertToCloudEvent(formatter.Format(value)).EncodeToJsonElement().DecodeToCloudEvent();
        envelope["correlationid"].Should().Be("workflow");
        envelope["causationid"].Should().Be("trigger");
        if (native)
            ((string)envelope[NativeMessageEnvelopeCodec.MetadataAttributeName]!).Should().NotContain("correlation");

        var restored = formatter.ConvertFromCloudEvent(envelope);
        restored.CorrelationId.Should().Be("workflow");
        restored.CausationId.Should().Be("trigger");
        restored.Attributes.Keys.Should().NotContain("correlationid").And.NotContain("causationid");
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EmptyIdentifiers_FollowExistingExtensionBehavior(bool native)
    {
        var value = EventData.CreateEventWith(new { Value = 1 }, "created").WithKey("key");
        if (native)
            value.AsNative();

        value.CorrelationId = "";
        value.CausationId = "";
        var formatter = new EventFormatter { SourceBaseUri = new Uri("urn:test") };
        var envelope = formatter.ConvertToCloudEvent(formatter.Format(value)).EncodeToJsonElement().DecodeToCloudEvent();
        envelope["correlationid"].Should().Be("");
        envelope["causationid"].Should().Be("");
        var restored = formatter.ConvertFromCloudEvent(envelope);
        restored.CorrelationId.Should().BeEmpty();
        restored.CausationId.Should().BeEmpty();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ConfiguredAttributeNames_RoundTripAndAreExcludedFromCustomAttributes(bool native)
    {
        var formatter = new EventFormatter { CorrelationIdAttributeName = "workflow", CausationIdAttributeName = "trigger", SourceBaseUri = new Uri("urn:test") };
        var value = EventData.CreateEventWith(new { Value = 1 }, "created").WithKey("key");
        if (native)
            value.AsNative();

        value.CorrelationId = "corr";
        value.CausationId = "cause";
        var envelope = formatter.ConvertToCloudEvent(formatter.Format(value)).EncodeToJsonElement().DecodeToCloudEvent();
        envelope["workflow"].Should().Be("corr");
        envelope["trigger"].Should().Be("cause");
        envelope["correlationid"].Should().BeNull();
        envelope["causationid"].Should().BeNull();
        var restored = formatter.ConvertFromCloudEvent(envelope);
        restored.CorrelationId.Should().Be("corr");
        restored.CausationId.Should().Be("cause");
        restored.Attributes.Should().BeEmpty();
    }

    [Test]
    public void MissingIdentifiers_AreNotInvented()
    {
        var envelope = new EventFormatter().ConvertToCloudEvent(new EventData());
        envelope["correlationid"].Should().BeNull();
        envelope["causationid"].Should().BeNull();
    }
}
