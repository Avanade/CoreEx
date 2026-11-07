namespace CoreEx.Events.Test.Unit;

public class NativeMessageTests
{
    [TestCase("application/json")]
    [TestCase("application/problem+json; charset=utf-8")]
    public void Json_RoundTrip_PreservesContentAndContentType(string contentType)
    {
        var value = EventData.CreateNativeWith(new { ETag = "keep", Nested = new[] { 1, 2 } }).WithKey("key");
        value.Data = value.Data!.WithMediaType(contentType);
        var formatter = new EventFormatter();
        var envelope = formatter.ConvertToCloudEvent(formatter.Format(value));
        var stored = envelope.EncodeToJsonElement();
        stored.GetProperty("data").GetProperty("etag").GetString().Should().Be("keep");
        var restored = formatter.ConvertFromCloudEvent(stored.DecodeToCloudEvent());
        restored.DataRepresentation.Should().Be(MessageRepresentation.Native);
        restored.Data!.MediaType.Should().Be(contentType);
        restored.Data.ToString().Should().Be(value.Data.ToString());
        restored.Attributes.Keys.Should().NotContain(NativeMessageEnvelopeCodec.NativeAttributeName);
    }

    [TestCase("application/json", "{ \"x\": 1 }")]
    [TestCase("application/octet-stream", "\0\u00ff\n")]
    [TestCase("text/plain; charset=utf-8", "hello \"world\"")]
    [TestCase("application/octet-stream", "")]
    public void Opaque_RoundTrip_PreservesExactBytes(string contentType, string body)
    {
        var value = EventData.CreateNativeWithData(BinaryData.FromString(body, contentType)).WithKey("key");
        var formatter = new EventFormatter();
        var envelope = formatter.ConvertToCloudEvent(formatter.Format(value));
        var stored = envelope.EncodeToJsonElement();
        stored.TryGetProperty("data_base64", out _).Should().BeTrue();
        var storedEnvelope = stored.DecodeToCloudEvent();
        storedEnvelope.Data.Should().BeOfType<byte[]>();
        var restored = formatter.ConvertFromCloudEvent(storedEnvelope);
        restored.Data!.ToArray().Should().Equal(value.Data!.ToArray());
        restored.Data.MediaType.Should().Be(contentType);
        restored.IsDataOpaque.Should().BeTrue();
    }

    [Test]
    public void NativeMarker_DoesNotPersistMessageType()
    {
        var value = EventData.CreateCommandWith("products", new { Sku = "sku" }, "reserve").AsNative().WithKey("key");
        value.ReplyTo = "replies";
        value.CorrelationId = "corr";
        value.WithTitle("external");
        value.SetAttribute("s", "hello");
        value.SetAttribute("revision", "1");
        var formatter = new EventFormatter();
        var stored = formatter.ConvertToCloudEvent(formatter.Format(value)).EncodeToJsonElement();
        stored.GetProperty(NativeMessageEnvelopeCodec.NativeAttributeName).GetBoolean().Should().BeTrue();
        stored.EnumerateObject().Select(x => x.Name).Where(x => x.StartsWith("coreex", StringComparison.Ordinal)).Should().ContainSingle()
            .Which.Should().Be(NativeMessageEnvelopeCodec.NativeAttributeName);
        var restored = formatter.ConvertFromCloudEvent(stored.DecodeToCloudEvent());
        restored.MessageType.Should().Be(MessageType.Event);
        restored.ReplyTo.Should().Be("replies");
        restored.Attributes.Should().BeEquivalentTo(value.Attributes);
        var republished = formatter.ConvertToCloudEvent(restored);
        republished.Type.Should().Be("external");
        NativeMessageEnvelopeCodec.ReadAttributes(republished).Should().BeEquivalentTo(value.Attributes);
    }

    [Test]
    public void NativeMarker_DoesNotRequireMessageType()
    {
        var envelope = new CloudEvent([CloudEventAttribute.CreateExtension(NativeMessageEnvelopeCodec.NativeAttributeName, CloudEventAttributeType.Boolean)]) { Id = "id", Type = "test", Source = new Uri("urn:test") };
        envelope[NativeMessageEnvelopeCodec.NativeAttributeName] = true;

        var restored = new EventFormatter().ConvertFromCloudEvent(envelope);

        restored.DataRepresentation.Should().Be(MessageRepresentation.Native);
        restored.MessageType.Should().Be(MessageType.Event);
    }

    [Test]
    public void EventData_OrdinaryJson_IgnoresPublishingInstructions()
    {
        var value = EventData.CreateNative().WithKey("key");
        value.IsDataOpaque = true;
        value.CorrelationId = "corr";
        value.CausationId = "cause";
        value.SetAttribute("revision", "1");
        var json = System.Text.Json.JsonSerializer.Serialize(value);
        var restored = System.Text.Json.JsonSerializer.Deserialize<EventData>(json)!;
        json.Should().NotContain("DataRepresentation").And.NotContain("IsDataOpaque");
        restored.DataRepresentation.Should().Be(MessageRepresentation.CloudEvent);
        restored.IsDataOpaque.Should().BeFalse();
        restored.CorrelationId.Should().Be("corr");
        restored.CausationId.Should().Be("cause");
    }

    [Test]
    public void UnsupportedPropertyValues_AreRejected()
    {
        foreach (var unsupported in new object[] { new { Nested = true }, double.NaN, double.PositiveInfinity, Guid.Empty, 1f, 1L })
        {
            var value = EventData.CreateNative();
            value.SetAttribute("value", unsupported!);
            Action convert = () => new EventFormatter().ConvertToCloudEvent(value);
            convert.Should().Throw<ArgumentException>();
        }
    }

    [TestCase("coreexisdatanative")]
    [TestCase("CoreExNative")]
    public void Attributes_CannotOverrideControlMetadata(string name)
    {
        var value = EventData.CreateNative().SetAttribute(name, "native");
        Action convert = () => new EventFormatter().ConvertToCloudEvent(value);
        convert.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void MissingContentType_JsonRemainsJson()
    {
        var envelope = new CloudEvent { Id = "id", Type = "test", Source = new Uri("urn:test"), Data = BinaryData.FromString("[1,true]") };
        var restored = envelope.EncodeToJsonElement().DecodeToCloudEvent();
        ((BinaryData)restored.Data!).ToString().Should().Be("[1,true]");
    }

    [Test]
    public void MalformedContentType_IsRejected()
    {
        Action encode = () => new CloudEvent { Id = "id", Type = "test", Source = new Uri("urn:test"), DataContentType = "not a media type", Data = BinaryData.FromString("{}") }.EncodeToJsonElement();
        encode.Should().Throw<ArgumentException>();
    }

    [Test]
    public void PayloadReplacement_ResetsOpaqueFlag()
    {
        var value = EventData.CreateNativeWithData(BinaryData.FromString("{}"));
        value.WithValue(new { Number = 1 }).IsDataOpaque.Should().BeFalse();
        value.WithData(BinaryData.FromString("{}")).IsDataOpaque.Should().BeTrue();
        value.WithValue<object>(null).IsDataOpaque.Should().BeFalse();
    }

    [TestCase("null")]
    [TestCase("false")]
    [TestCase("42")]
    [TestCase("\"hello\"")]
    [TestCase("[]")]
    public void JsonTokens_RoundTrip(string body)
    {
        var envelope = new CloudEvent { Id = "id", Type = "test", Source = new Uri("urn:test"), DataContentType = "application/json", Data = BinaryData.FromString(body, "application/json") };
        var restored = envelope.EncodeToJsonElement().DecodeToCloudEvent();
        ((BinaryData)restored.Data!).ToString().Should().Be(body);
        ((BinaryData)restored.Data).MediaType.Should().Be("application/json");
    }

    [Test]
    public void ExternalText_DecodesWithoutJsonQuotes()
    {
        var stored = BinaryData.FromString("""{"specversion":"1.0","id":"id","type":"test","source":"urn:test","datacontenttype":"text/plain; charset=utf-8","data":"hello \"world\""}""");
        var restored = stored.DecodeToCloudEvent();
        ((BinaryData)restored.Data!).ToString().Should().Be("hello \"world\"");
        ((BinaryData)restored.Data).MediaType.Should().Be("text/plain; charset=utf-8");
    }

    [TestCase("traceparent")]
    [TestCase("coreexfuture")]
    public void ReservedProperties_AreRejected(string name)
    {
        var value = EventData.CreateNativeWith(new { });
        value.Attributes[name] = "value";
        Action convert = () => new EventFormatter().ConvertToCloudEvent(value);
        convert.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void MalformedNativeMarker_IsRejected()
    {
        var envelope = new CloudEvent([CloudEventAttribute.CreateExtension(NativeMessageEnvelopeCodec.NativeAttributeName, CloudEventAttributeType.String)]);
        envelope[NativeMessageEnvelopeCodec.NativeAttributeName] = "native";
        Action read = () => NativeMessageEnvelopeCodec.GetRepresentation(envelope);
        read.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void Queue_SnapshotsPayloadAndSubject_AndPreservesRouting()
    {
        var formatter = new EventFormatter { SourceBaseUri = new Uri("urn:test") };
        var publisher = new NoOpEventPublisher(new NamedDestinationProvider { Destination = "events" }, formatter);
        var value = EventData.CreateCommandWith("products", new { ETag = "keep" }, "reserve").AsNative().WithKey("key");
        value.WithTitle("first");
        publisher.Add(value);
        value.Title = "changed";
        value.Data = BinaryData.FromString("{}");
        var queued = publisher.GetEvents().Single();
        queued.Destination.Should().Be("events-products");
        queued.Event.Type.Should().Be("first");
        ((BinaryData)queued.Event.Data!).ToString().Should().Contain("keep");
    }

    [Test]
    public void WithData_SnapshotsCallerOwnedMemory_AndQueueSnapshotsProperties()
    {
        byte[] bytes = [1, 2, 3];
        var value = EventData.CreateNative().WithKey("key");
        value.WithData(BinaryData.FromBytes(bytes, "application/octet-stream"));
        value.SetAttribute("revision", "1");
        bytes[0] = 9;
        var publisher = new NoOpEventPublisher();
        publisher.Add("external", value);
        bytes[1] = 9;
        value.SetAttribute("revision", "2");
        var queued = publisher.GetEvents().Single().Event;
        var queuedBytes = queued.Data.Should().BeOfType<ReadOnlyMemory<byte>>().Subject;
        queuedBytes.ToArray().Should().Equal(1, 2, 3);
        System.Runtime.InteropServices.MemoryMarshal.TryGetArray(value.Data!.ToMemory(), out var sourceBuffer).Should().BeTrue();
        System.Runtime.InteropServices.MemoryMarshal.TryGetArray(queuedBytes, out var queuedBuffer).Should().BeTrue();
        queuedBuffer.Array.Should().BeSameAs(sourceBuffer.Array);
        queuedBuffer.Offset.Should().Be(sourceBuffer.Offset);
        NativeMessageEnvelopeCodec.ReadAttributes(queued)["revision"].Should().Be("1");
    }

    [Test]
    public void NativeAttributes_ExcludePrivateNullAndFrameworkMetadata()
    {
        var value = EventData.CreateNative().WithKey("key").SetAttribute("partner", "abc").SetAttribute("_secret", new { Token = "private" }).SetAttribute<string?>("absent", null);
        value.UserId = "internal-user";
        var formatter = new EventFormatter();
        var stored = formatter.ConvertToCloudEvent(formatter.Format(value)).EncodeToJsonElement();
        stored.TryGetProperty("_secret", out _).Should().BeFalse();
        var attributes = NativeMessageEnvelopeCodec.ReadAttributes(stored.DecodeToCloudEvent());
        attributes.Should().ContainSingle().Which.Should().Be(new KeyValuePair<string, string>("partner", "abc"));
    }

    [Test]
    public void NativeMarker_IsTheOnlyRequiredProtocolMetadata()
    {
        var envelope = new CloudEvent([CloudEventAttribute.CreateExtension(NativeMessageEnvelopeCodec.NativeAttributeName, CloudEventAttributeType.Boolean)]);
        envelope[NativeMessageEnvelopeCodec.NativeAttributeName] = true;
        NativeMessageEnvelopeCodec.GetRepresentation(envelope).Should().Be(MessageRepresentation.Native);
        envelope[NativeMessageEnvelopeCodec.NativeAttributeName] = false;
        NativeMessageEnvelopeCodec.GetRepresentation(envelope).Should().Be(MessageRepresentation.CloudEvent);
    }

    [Test]
    public void Reconstruction_ForwardsEligibleCustomExtensions()
    {
        var formatter = new EventFormatter();
        var value = EventData.CreateNative().WithKey("key").SetAttribute("partner", "abc");
        var envelope = formatter.ConvertToCloudEvent(formatter.Format(value));
        envelope["frameworkextra"] = "external";
        var restored = formatter.ConvertFromCloudEvent(envelope.EncodeToJsonElement().DecodeToCloudEvent());
        restored.Attributes.Keys.Should().BeEquivalentTo("partner", "frameworkextra");
        NativeMessageEnvelopeCodec.ReadAttributes(envelope).Keys.Should().BeEquivalentTo("partner", "frameworkextra");
    }

    [Test]
    public void NativeAttributes_CannotClaimConfiguredFrameworkAliases()
    {
        var formatter = new EventFormatter { TenantIdAttributeName = "customtenant" };
        Action convert = () => formatter.ConvertToCloudEvent(EventData.CreateNative().SetAttribute("customtenant", "override"));
        convert.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void NativeMetadata_UsesCanonicalNamesAcrossFormatterConfigurations()
    {
        var producerFormatter = new EventFormatter
        {
            TenantIdAttributeName = "customtenant",
            AuthTypeAttributeName = "authentication",
            AuthIdAttributeName = "userid",
            ReplyToAttributeName = "customreply",
            CorrelationIdAttributeName = "customcorrelation",
            CausationIdAttributeName = "customcausation"
        };
        var value = EventData.CreateNativeWith(new { Value = 1 }).WithKey("key");
        value.TenantId = "tenant";
        value.UserType = CoreEx.Security.AuthenticationType.AccountUser;
        value.UserId = "internal-user";
        value.ReplyTo = "replies";
        value.CorrelationId = "correlation";
        value.CausationId = "causation";

        var stored = producerFormatter.ConvertToCloudEvent(producerFormatter.Format(value)).EncodeToJsonElement();
        var restored = new EventFormatter().ConvertFromCloudEvent(stored.DecodeToCloudEvent());

        restored.TenantId.Should().Be("tenant");
        restored.UserType.Should().Be(CoreEx.Security.AuthenticationType.AccountUser);
        restored.UserId.Should().Be("internal-user");
        restored.ReplyTo.Should().Be("replies");
        restored.CorrelationId.Should().Be("correlation");
        restored.CausationId.Should().Be("causation");
        NativeMessageEnvelopeCodec.ReadAttributes(stored.DecodeToCloudEvent()).Should().BeEmpty();
    }

    [Test]
    public async Task NativePublish_UsesCanonicalTracingAndPreservesCustomAttributesAcrossFormatterAliases()
    {
        var formatter = new EventFormatter
        {
            TenantIdAttributeName = "tenantalias",
            TraceParentAttributeName = "tp",
            TraceStateAttributeName = "ts",
            TraceBaggageAttributeName = "tb"
        };
        var publisher = new NoOpEventPublisher(formatter: formatter);
        publisher.Add(EventData.CreateNativeWith(new { Value = 1 }).WithKey("key").SetAttribute("partner", "abc"));
        using var activity = new Activity("publish").SetIdFormat(ActivityIdFormat.W3C).Start();

        await publisher.PublishAsync().ConfigureAwait(false);

        var envelope = publisher.GetEvents().Single().Event;
        envelope["traceparent"].Should().Be(activity!.Id);
        envelope["tracestate"].Should().BeNull();
        envelope["baggage"].Should().BeNull();
        envelope["tp"].Should().BeNull();
        envelope["ts"].Should().BeNull();
        envelope["tb"].Should().BeNull();

        var relayFormatter = new EventFormatter { TenantIdAttributeName = "partner" };
        var reconstructed = relayFormatter.ConvertFromCloudEvent(envelope);
        reconstructed.Attributes.Should().ContainKey("partner").WhoseValue.Should().Be("abc");
        reconstructed.TenantId.Should().BeNull();
    }

    [Test]
    public void CustomFormatterCannotLoseNativeRepresentation()
    {
        var publisher = new NoOpEventPublisher(formatter: new LosingFormatter());
        Action add = () => publisher.Add("external", EventData.CreateNative().WithKey("key"));
        add.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void NativeStillRequiresPartitionKey()
    {
        Action format = () => new EventFormatter().Format(EventData.CreateNative());
        format.Should().Throw<InvalidOperationException>();
    }

    private sealed class LosingFormatter : EventFormatter
    {
        public override CloudEvent ConvertToCloudEvent(EventData value) => new() { Id = value.Id, Type = value.Title, Source = value.Source };
    }
}
