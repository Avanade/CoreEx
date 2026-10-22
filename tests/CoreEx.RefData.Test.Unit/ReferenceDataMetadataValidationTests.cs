namespace CoreEx.RefData.Test.Unit;

public partial class ReferenceDataOrchestratorTests
{
    [Contract]
    internal partial class Booking
    {
        [ReferenceData<DummyRefData>]
        [JsonPropertyName("sales_channel")]
        [Localization("SalesChannel", "Sales channel")]
        public partial string? ChannelCode { get; set; }
    }

    internal class BookingValidator : Validator<Booking, BookingValidator>
    {
        public BookingValidator() => Property(x => x.Channel).Mandatory().IsValid();
    }

    [Test]
    public async Task GeneratedNavigationProperty_CanBeValidatedWithoutChangingContractMetadata()
    {
        var properties = RuntimeMetadata.GetCachedProperties<Booking>();
        properties.Keys.Should().ContainSingle().Which.Should().Be(nameof(Booking.ChannelCode));

        var validator = BookingValidator.Default;
        var missing = await validator.ValidateAsync(new Booking());
        missing.Messages.Should().ContainSingle().Which.Property.Should().Be("sales_channel");
        missing.Messages.Should().ContainSingle().Which.Text.ToString().Should().Be("Sales channel is required.");

        var invalid = await validator.ValidateAsync(new Booking { ChannelCode = "Z" });
        invalid.Messages.Should().ContainSingle().Which.Property.Should().Be("sales_channel");
        invalid.Messages.Should().ContainSingle().Which.Text.ToString().Should().Be("Sales channel is invalid.");

        var valid = await validator.ValidateAsync(new Booking { ChannelCode = "A" });
        valid.HasErrors.Should().BeFalse();
        RuntimeMetadata.GetCachedProperties(typeof(Booking)).Should().BeSameAs(properties);
        properties.Keys.Should().ContainSingle().Which.Should().Be(nameof(Booking.ChannelCode));
    }

    [Test]
    public void GeneratedNavigationProperty_ExpressionFactoriesUseReflectedJsonNameAndLabel()
    {
        var metadata = RuntimeMetadata.GetForExpression<Booking, DummyRefData?>(x => x.Channel);
        var reflected = RuntimeMetadata.GetForExpression<Booking, DummyRefData?>(x => x.Channel, null);
        reflected.Should().BeSameAs(metadata);
        metadata.Name.Should().Be(nameof(Booking.Channel));
        metadata.GetJsonName().Should().Be("sales_channel");
        metadata.Text.ToString().Should().Be("Sales channel");

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseUpper };
        RuntimeMetadata.GetJsonPath<Booking>(x => x.Channel, options).Should().Be("sales_channel");
        var message = MessageItem.CreateErrorMessage<Booking>(x => x.Channel);
        message.Property.Should().Be("sales_channel");
        message.Text.ToString().Should().Be("Sales channel is invalid.");
        ValidationException.Create<Booking>(x => x.Channel).Messages.Should().ContainSingle().Which.Should().Be(message);
        RuntimeMetadata.GetCachedProperties<Booking>().Should().NotContainKey(nameof(Booking.Channel));
    }
}
