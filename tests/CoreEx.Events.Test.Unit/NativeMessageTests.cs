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
        restored.Attributes.Keys.Should().NotContain(NativeMessageEnvelopeCodec.RepresentationAttributeName);
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
        var restored = formatter.ConvertFromCloudEvent(stored.DecodeToCloudEvent());
        restored.Data!.ToArray().Should().Equal(value.Data!.ToArray());
        restored.Data.MediaType.Should().Be(contentType);
        restored.IsDataOpaque.Should().BeTrue();
    }

    [Test]
    public void IntentAndAttributes_SurvivePersistenceAndReconstruction()
    {
        var value = EventData.CreateCommandWith("products", new { Sku = "sku" }, "reserve").AsNative().WithKey("key");
        value.ReplyTo = "replies";
        value.CorrelationId = "corr";
        value.WithTitle("external");
        value.SetAttribute("s", "hello");
        value.SetAttribute("revision", "1");
        var formatter = new EventFormatter();
        var restored = formatter.ConvertFromCloudEvent(formatter.ConvertToCloudEvent(formatter.Format(value)).EncodeToJsonElement().DecodeToCloudEvent());
        restored.MessageType.Should().Be(MessageType.Command);
        restored.ReplyTo.Should().Be("replies");
        restored.Attributes.Should().BeEquivalentTo(value.Attributes);
        var republished = formatter.ConvertToCloudEvent(restored);
        NativeMessageEnvelopeCodec.ReadSubject(republished).Should().Be("external");
        NativeMessageEnvelopeCodec.ReadAttributes(republished).Should().BeEquivalentTo(value.Attributes);
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
        json.Should().NotContain("DataRepresentation").And.NotContain("IsDataOpaque").And.NotContain("NativeSubject");
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

    [TestCase("datarepresentation")]
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

    [TestCase("ce_source")]
    [TestCase("traceparent")]
    [TestCase("DATAREPRESENTATION")]
    [TestCase("")]
    public void ReservedProperties_AreRejected(string name)
    {
        var value = EventData.CreateNativeWith(new { });
        value.Attributes[name] = "value";
        Action convert = () => new EventFormatter().ConvertToCloudEvent(value);
        convert.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void UnknownRepresentationAndVersion_AreRejected()
    {
        var envelope = new CloudEvent();
        envelope[NativeMessageEnvelopeCodec.RepresentationAttributeName] = "unknown";
        Action read = () => NativeMessageEnvelopeCodec.ReadSubject(envelope);
        read.Should().Throw<NotSupportedException>();
        envelope[NativeMessageEnvelopeCodec.RepresentationAttributeName] = "native";
        envelope[NativeMessageEnvelopeCodec.MetadataAttributeName] = """{"version":2}""";
        read.Should().Throw<NotSupportedException>();
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
        NativeMessageEnvelopeCodec.ReadSubject(queued.Event).Should().Be("first");
        ((BinaryData)queued.Event.Data!).ToString().Should().Contain("keep");
    }

    [Test]
    public void Queue_SnapshotsCallerOwnedMemoryAndPropertyDictionary()
    {
        byte[] bytes = [1, 2, 3];
        var value = EventData.CreateNative().WithKey("key");
        value.Data = BinaryData.FromBytes(bytes, "application/octet-stream");
        value.IsDataOpaque = true;
        value.SetAttribute("revision", "1");
        var publisher = new NoOpEventPublisher();
        publisher.Add("external", value);
        bytes[0] = 9;
        value.SetAttribute("revision", "2");
        var queued = publisher.GetEvents().Single().Event;
        ((BinaryData)queued.Data!).ToArray().Should().Equal(1, 2, 3);
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
    public void InvalidNativeMetadata_DoesNotFallBackToCloudEvents()
    {
        var envelope = new CloudEvent();
        envelope["datarepresentation"] = "native";
        Action read = () => NativeMessageEnvelopeCodec.ReadSubject(envelope);
        read.Should().Throw<InvalidOperationException>();
        envelope["coreexnative"] = """{"version":1,"intent":0}""";
        read.Should().Throw<KeyNotFoundException>();
        envelope["datarepresentation"] = "cloudevent";
        read.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void Reconstruction_DoesNotPromoteUnlistedExtensionsToNativeHeaders()
    {
        var formatter = new EventFormatter();
        var value = EventData.CreateNative().WithKey("key").SetAttribute("partner", "abc");
        var envelope = formatter.ConvertToCloudEvent(formatter.Format(value));
        envelope["frameworkextra"] = "internal";
        var restored = formatter.ConvertFromCloudEvent(envelope.EncodeToJsonElement().DecodeToCloudEvent());
        restored.Attributes.Keys.Should().BeEquivalentTo("partner");
        NativeMessageEnvelopeCodec.ReadAttributes(formatter.ConvertToCloudEvent(restored)).Keys.Should().BeEquivalentTo("partner");
    }

    [TestCase("missing")]
    [TestCase("duplicate")]
    [TestCase("reserved")]
    public void InvalidNativeAttributeLists_AreRejected(string scenario)
    {
        var formatter = new EventFormatter();
        var envelope = formatter.ConvertToCloudEvent(formatter.Format(EventData.CreateNative().WithKey("key").SetAttribute("partner", "abc")));
        var metadata = (string)envelope[NativeMessageEnvelopeCodec.MetadataAttributeName]!;
        envelope[NativeMessageEnvelopeCodec.MetadataAttributeName] = metadata.Replace("[\"partner\"]", scenario switch
        {
            "missing" => "[\"missing\"]",
            "duplicate" => "[\"partner\",\"partner\"]",
            _ => "[\"tenantid\"]"
        });
        Action read = () => NativeMessageEnvelopeCodec.ReadAttributes(envelope);
        if (scenario == "duplicate")
            read.Should().Throw<ArgumentException>();
        else
            read.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void NativeAttributes_CannotClaimConfiguredFrameworkAliases()
    {
        var formatter = new EventFormatter { TenantIdAttributeName = "customtenant" };
        Action convert = () => formatter.ConvertToCloudEvent(EventData.CreateNative().SetAttribute("customtenant", "override"));
        convert.Should().Throw<InvalidOperationException>();
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
