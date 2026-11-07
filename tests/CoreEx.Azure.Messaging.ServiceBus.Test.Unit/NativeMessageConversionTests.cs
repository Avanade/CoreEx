namespace CoreEx.Azure.Messaging.ServiceBus.Test.Unit;

public class NativeMessageConversionTests
{
    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void Correlation_IsSharedAcrossRepresentationsAndContentModes(bool native, bool binary)
    {
        var value = EventData.CreateEventWith(new { Value = 1 }, "created").WithKey("key");
        if (native)
            value.AsNative();

        value.CorrelationId = "workflow";
        value.CausationId = "cause";
        var formatter = new EventFormatter { SourceBaseUri = new Uri("urn:test") };
        var envelope = formatter.ConvertToCloudEvent(formatter.Format(value)).EncodeToJsonElement().DecodeToCloudEvent();
        var message = envelope.ToServiceBusMessage(binary ? CloudNative.CloudEvents.ContentMode.Binary : CloudNative.CloudEvents.ContentMode.Structured);
        message.CorrelationId.Should().Be("workflow");
        if (native)
            message.ApplicationProperties.Keys.Should().NotContain("ce_correlationid").And.NotContain("causationid");
        else
        {
            var restored = ServiceBusMessageTests.ConvertToReceivedMessage(message).ToCloudEvent();
            restored["correlationid"].Should().Be("workflow");
            restored["causationid"].Should().Be("cause");
        }
    }

    [Test]
    public void BinaryExcludedAttributes_PreservesCorrelation_ButNotOptionalCausation()
    {
        var envelope = new CloudNative.CloudEvents.CloudEvent { Id = "id", Type = "type", Source = new Uri("urn:test") };
        envelope.SetExtensionAttribute("correlationid", "workflow");
        envelope.SetExtensionAttribute("causationid", "cause");
        var message = envelope.ToServiceBusMessage(CloudNative.CloudEvents.ContentMode.Binary, false);
        message.CorrelationId.Should().Be("workflow");
        var restored = ServiceBusMessageTests.ConvertToReceivedMessage(message).ToCloudEvent();
        restored["correlationid"].Should().Be("workflow");
        restored["causationid"].Should().BeNull();
    }

    [Test]
    public void BinaryCorrelation_EnvelopeWinsWithBrokerFallback()
    {
        var properties = new Dictionary<string, object> { ["ce_specversion"] = "1.0", ["ce_source"] = "urn:test", ["ce_correlationid"] = "envelope" };
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(messageId: "id", subject: "type", correlationId: "broker", properties: properties);
        message.ToCloudEvent()["correlationid"].Should().Be("envelope");
        properties.Remove("ce_correlationid");
        ServiceBusModelFactory.ServiceBusReceivedMessage(messageId: "id", subject: "type", correlationId: "broker", properties: properties)
            .ToCloudEvent()["correlationid"].Should().Be("broker");
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Native_MapsOnlyExplicitMetadata_AfterStorage(bool opaque)
    {
        var value = opaque ? EventData.CreateNativeWithData(BinaryData.FromString("{ \"etag\": \"keep\" }", "application/json")) : EventData.CreateNativeWith(new { ETag = "keep" });
        value.WithKey("key").WithTitle("external.created");
        value.ReplyTo = "replies";
        value.UserId = "private-user";
        value.TraceParent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01";
        value.CorrelationId = "corr";
        value.SetAttribute("attempt", "2");
        value.SetAttribute("_private", "secret");
        value.SetAttribute<string?>("absent", null);
        var formatter = new EventFormatter();
        var envelope = formatter.ConvertToCloudEvent(formatter.Format(value)).EncodeToJsonElement().DecodeToCloudEvent();
        var message = envelope.ToServiceBusMessage();
        message.Body.ToArray().Should().Equal(value.Data!.ToArray());
        message.ContentType.Should().Be("application/json");
        message.Subject.Should().Be("external.created");
        message.MessageId.Should().Be(value.Id);
        message.PartitionKey.Should().Be("key");
        message.CorrelationId.Should().Be("corr");
        message.ReplyTo.Should().Be("replies");
        message.ApplicationProperties.Keys.Should().BeEquivalentTo("attempt", "traceparent");
        message.ApplicationProperties["attempt"].Should().Be("2");
        ServiceBusMessageTests.ConvertToReceivedMessage(message).IsCloudEvent().Should().BeFalse();
    }

    [TestCase(true, "title")]
    [TestCase(false, null)]
    public void Native_SubjectPolicySurvivesPersistenceAndDifferentRelayFormatter(bool includeSubject, string? expected)
    {
        var value = EventData.CreateNativeWith(new { }).WithKey("key").WithTitle("title");
        var formatter = new EventFormatter { IncludeNativeSubject = includeSubject };
        var publisher = new CoreEx.Events.Publishing.NoOpEventPublisher(formatter: formatter);
        publisher.Add("external", value);
        var stored = publisher.GetEvents().Single().Event.EncodeToJsonElement().DecodeToCloudEvent();
        stored.ToServiceBusMessage().Subject.Should().Be(expected);
        var relayFormatter = new EventFormatter { IncludeNativeSubject = !includeSubject };
        var reconstructed = relayFormatter.ConvertFromCloudEvent(stored);
        relayFormatter.ConvertToCloudEvent(relayFormatter.Format(reconstructed)).ToServiceBusMessage().Subject.Should().Be(expected);
    }

    [Test]
    public void BinaryExcludedAttributes_PreservesRequiredMetadataAndOriginalTime()
    {
        var value = EventData.CreateEventWith(new { Id = "key" }, "created").WithKey("key");
        value.Timestamp = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var formatter = new EventFormatter { SourceBaseUri = new Uri("urn:test") };
        var envelope = formatter.ConvertToCloudEvent(formatter.Format(value));
        var restored = ServiceBusMessageTests.ConvertToReceivedMessage(envelope.ToServiceBusMessage(CloudNative.CloudEvents.ContentMode.Binary, false)).ToCloudEvent();
        restored.Source.Should().Be(envelope.Source);
        restored.Time.Should().Be(value.Timestamp);
        restored.Id.Should().Be(value.Id);
        restored.Type.Should().Be(envelope.Type);
    }

    [Test]
    public void StructuredDetection_DoesNotRequireDuplicateProperties()
    {
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("""{"specversion":"1.0","id":"id","type":"test","source":"urn:test","data":42}"""),
            contentType: "application/cloudevents+json");
        message.IsCloudEvent().Should().BeTrue();
        message.ToCloudEvent().Id.Should().Be("id");
    }

    [Test]
    public void BinaryWithoutEventTime_DoesNotInventOccurrenceTime()
    {
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString("{}"), messageId: "id", subject: "test", contentType: "application/json",
            properties: new Dictionary<string, object> { ["ce_specversion"] = "1.0", ["ce_source"] = "urn:test" });
        message.ToCloudEvent().Time.Should().BeNull();
    }

    [TestCase("unknown")]
    [TestCase("2.0")]
    [TestCase("0.3")]
    public void UnsupportedBinaryVersions_AreRejected(string version)
    {
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString("{}"), contentType: "application/json",
            properties: new Dictionary<string, object> { ["ce_specversion"] = version });
        Action convert = () => message.ToCloudEvent();
        convert.Should().Throw<NotSupportedException>();
    }

    [Test]
    public void NativeJson_IsNotImplicitlyPromotedToCloudEvent()
    {
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString("{}"), contentType: "application/json");
        Action convert = () => message.ToCloudEvent();
        convert.Should().Throw<InvalidOperationException>();
    }

    [TestCase("1.0")]
    public void Binary_BrokerFieldsTakePrecedence(string version)
    {
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString("42"), messageId: "broker-id", subject: "broker-type", contentType: "application/json; charset=utf-8",
            properties: new Dictionary<string, object>
            {
                ["ce_specversion"] = version, ["ce_source"] = "urn:test", ["ce_id"] = "duplicate-id",
                ["ce_type"] = "duplicate-type", ["ce_datacontenttype"] = "text/plain", ["ce_subject"] = "key"
            });
        var envelope = message.ToCloudEvent();
        envelope.SpecVersion.VersionId.Should().Be(version);
        envelope.Id.Should().Be("broker-id");
        envelope.Type.Should().Be("broker-type");
        envelope.Subject.Should().Be("key");
        envelope.DataContentType.Should().Be("application/json; charset=utf-8");
    }

    [Test]
    public void Structured_BodyIsAuthoritativeOverBrokerDuplicates()
    {
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("""{"specversion":"1.0","id":"body-id","type":"body-type","source":"urn:body","data":42}"""),
            messageId: "broker-id", subject: "broker-type", contentType: "application/cloudevents+json; charset=utf-8",
            properties: new Dictionary<string, object> { ["ce_specversion"] = "unsupported", ["ce_source"] = "urn:duplicate" });
        var envelope = message.ToCloudEvent();
        envelope.Id.Should().Be("body-id");
        envelope.Type.Should().Be("body-type");
        envelope.Source.Should().Be(new Uri("urn:body"));
    }

    [Test]
    public void Binary_MissingSourceIsRejectedOnReceiveAndSend()
    {
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString("{}"), messageId: "id", subject: "type", contentType: "application/json",
            properties: new Dictionary<string, object> { ["ce_specversion"] = "1.0" });
        Action receive = () => message.ToCloudEvent();
        receive.Should().Throw<InvalidOperationException>();
        var envelope = new CloudNative.CloudEvents.CloudEvent { Id = "id", Type = "type", Data = BinaryData.FromString("{}") };
        Action send = () => envelope.ToServiceBusMessage(CloudNative.CloudEvents.ContentMode.Binary);
        send.Should().Throw<InvalidOperationException>();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Native_AbsentAndEmptyBodiesBothSendEmpty(bool absent)
    {
        var value = absent ? EventData.CreateNative() : EventData.CreateNativeWithData(BinaryData.Empty);
        var formatter = new EventFormatter();
        var message = formatter.ConvertToCloudEvent(formatter.Format(value.WithKey("key"))).EncodeToJsonElement().DecodeToCloudEvent().ToServiceBusMessage();
        message.Body.ToArray().Should().BeEmpty();
    }

    [Test]
    public void UnknownMarker_IsRejectedForDirectEnvelope()
    {
        var envelope = new CloudNative.CloudEvents.CloudEvent { Id = "id", Type = "type", Source = new Uri("urn:test") };
        envelope["datarepresentation"] = "future";
        Action convert = () => envelope.ToServiceBusMessage();
        convert.Should().Throw<NotSupportedException>();
    }
}
