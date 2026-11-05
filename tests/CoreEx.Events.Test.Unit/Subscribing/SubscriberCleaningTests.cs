namespace CoreEx.Events.Test.Unit.Subscribing;

public class SubscriberCleaningTests
{
    [TestCase(false, null, false)]
    [TestCase(true, null, true)]
    [TestCase(false, true, true)]
    [TestCase(true, false, false)]
    public async Task TypedPayload_ResolvesCleanupBeforeValidationAndHandler(bool ownerClean, bool? overrideClean, bool cleaned)
    {
        var subscriber = new TypedSubscriber { AutoCleanValue = overrideClean };
        var owner = new Owner(subscriber) { AutoCleanValue = ownerClean };
        var data = new EventData().WithValue(new Payload
        {
            Name = "hello ",
            Child = new Payload { Name = "child " },
            Date = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Unspecified)
        });
        data.Key = "key ";
        var originalData = data.Data;
        var result = await owner.InvokeAsync(data);

        result.IsSuccess.Should().BeTrue();
        subscriber.Value!.Name.Should().Be(cleaned ? "hello" : "hello ");
        subscriber.Value.Child!.Name.Should().Be(cleaned ? "child" : "child ");
        subscriber.Value.Date.Kind.Should().Be(cleaned ? DateTimeKind.Utc : DateTimeKind.Unspecified);
        subscriber.Validated.Should().BeSameAs(subscriber.Value);
        subscriber.ValidatedName.Should().Be(subscriber.Value.Name);
        data.Data.Should().BeSameAs(originalData);
        data.Key.Should().Be("key ");
        subscriber.Event.Should().BeSameAs(data);
    }

    [Test]
    public void Defaults_AreOptIn()
    {
        new Owner(new TypedSubscriber()).AutoCleanValue.Should().BeFalse();
        new TypedSubscriber().AutoCleanValue.Should().BeNull();
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ScalarCleanup_PrecedesRequiredCheck(bool required)
    {
        var owner = new Owner(new TypedSubscriber()) { AutoCleanValue = true };
        var result = owner.DeserializeValue<string?>(new EventData().WithValue(" "), required);

        result.IsFailure.Should().Be(required);
        if (!required)
            result.Value.Should().BeNull();
    }

    [Test]
    public async Task CleanupFailure_UsesReceiveErrorHandlingNotInvalidDataPolicy()
    {
        var owner = new Owner(new FailingSubscriber()) { AutoCleanValue = true, InvalidDataHandling = ErrorHandling.DeadLetter };
        var result = await owner.InvokeAsync(new EventData().WithValue(new FailingPayload()));

        result.Error.Should().BeOfType<CoreEx.Events.Subscribing.Exceptions.EventSubscriberUnhandledException>();
        result.Error.InnerException.Should().BeOfType<InvalidOperationException>().Which.Message.Should().Be("Cleanup failed.");
    }

    [Test]
    public async Task KeyOnlyHandler_DoesNotCleanEnvelopeOrAttemptPayloadDeserialization()
    {
        var subscriber = new KeySubscriber();
        var owner = new Owner(subscriber) { AutoCleanValue = true };
        var data = new EventData { Key = "key " };

        (await owner.InvokeAsync(data)).IsSuccess.Should().BeTrue();
        subscriber.Key.Should().Be("key ");
        data.Data.Should().BeNull();
    }

    [Test]
    public void Cleanup_UsesDefaultArgsAndPersistsReplacementExactlyOnce()
    {
        var owner = new Owner(new TypedSubscriber()) { AutoCleanValue = true };
        var result = owner.DeserializeValue<TrackingPayload>(new EventData().WithValue(new TrackingPayload()));

        result.IsSuccess.Should().BeTrue();
        result.Value.CleanCalls.Should().Be(1);
        result.Value.UsedDefaultArgs.Should().BeTrue();
    }

    private sealed class Owner(SubscribedBase subscriber) : EventSubscriberBase(new EventFormatter(), NullLogger<EventSubscriberBase>.Instance)
    {
        public Task<Result> InvokeAsync(EventData data) => ReceiveAsync(data);

        protected override Task<Result> OnReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken)
            => subscriber.ReceiveAsync(@event, args, cancellationToken);
    }

    private sealed class TypedSubscriber : SubscribedBase<Payload>
    {
        public Payload? Value { get; private set; }

        public Payload? Validated { get; private set; }

        public string? ValidatedName { get; private set; }

        public EventData? Event { get; private set; }

        public override CoreEx.Validation.IValidator<Payload>? ValueValidator => new PayloadValidator(this);

        protected override Task<Result> OnReceiveAsync(Payload value, EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken)
        {
            Value = value;
            Event = @event;
            return Result.SuccessTask;
        }

        private sealed class PayloadValidator(TypedSubscriber subscriber) : CoreEx.Validation.IValidator<Payload>
        {
            public Task<CoreEx.Validation.IValidationResult<Payload>> ValidateAsync(Payload value, CancellationToken cancellationToken)
            {
                subscriber.Validated = value;
                subscriber.ValidatedName = value.Name;
                var result = new Moq.Mock<CoreEx.Validation.IValidationResult<Payload>>();
                result.SetupGet(x => x.HasErrors).Returns(false);
                return Task.FromResult(result.Object);
            }
        }
    }

    public sealed class Payload
    {
        public string? Name { get; set; }

        public Payload? Child { get; set; }

        public DateTime Date { get; set; }
    }

    public sealed class TrackingPayload : CoreEx.Entities.Abstractions.IWrappedValueCleaner<TrackingPayload>
    {
        public int CleanCalls { get; set; }

        public bool UsedDefaultArgs { get; set; }

        public TrackingPayload Clean(CoreEx.Entities.CleanArgs args)
            => new() { CleanCalls = CleanCalls + 1, UsedDefaultArgs = !args.CleanAndDefaultRoot && !args.CleanAndDefaultNested };
    }

    public sealed class FailingPayload : CoreEx.Entities.Abstractions.IWrappedValueCleaner<FailingPayload>
    {
        public FailingPayload Clean(CoreEx.Entities.CleanArgs args) => throw new InvalidOperationException("Cleanup failed.");
    }

    private sealed class FailingSubscriber : SubscribedBase<FailingPayload>
    {
        protected override Task<Result> OnReceiveAsync(FailingPayload value, EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Handler must not run.");
    }

    private sealed class KeySubscriber : SubscribedBase
    {
        public string? Key { get; private set; }

        protected override Task<Result> OnReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken)
        {
            Key = @event.Key;
            return Result.SuccessTask;
        }
    }
}
