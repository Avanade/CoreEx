namespace CoreEx.Azure.Messaging.ServiceBus.Test.Unit;

[TestFixture]
public class ServiceBusSubscriberBaseTTests
{
    [Test]
    public async Task NativeJson_EnrichesTransportTracingBeforeHandler()
    {
        using var activity = new Activity("native-receive").Start();
        var subscriber = new NativeSubscriber { PropagateBaggage = true };
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString("{}"),
            properties: new Dictionary<string, object> { ["traceparent"] = "00-0123456789abcdef0123456789abcdef-0123456789abcdef-01", ["baggage"] = "region=west" });

        (await subscriber.ReceiveAsync(message)).IsSuccess.Should().BeTrue();
        activity.Links.Should().ContainSingle();
        activity.GetBaggageItem("region").Should().Be("west");
        subscriber.Calls.Should().Be(1);
    }

    [Test]
    public async Task NativeJson_DeserializesAndPreservesMessageArgumentsAndToken()
    {
        var subscriber = new NativeSubscriber();
        var message = Message("{\"name\":\"example\"}");
        var args = new EventSubscriberArgs();
        args.Properties["custom"] = 42;
        using var cancellation = new CancellationTokenSource();
        using var activity = new Activity("native-receive").Start();
        ServiceBusSubscriberBase untyped = subscriber;

        var result = await untyped.ReceiveAsync(message, args, cancellation.Token);

        result.IsSuccess.Should().BeTrue();
        subscriber.Value?.Name.Should().Be("example");
        subscriber.Message.Should().BeSameAs(message);
        subscriber.Args.Should().BeSameAs(args);
        subscriber.Token.Should().Be(cancellation.Token);
        args.Owner.Should().BeSameAs(subscriber);
        args.Message.Should().BeSameAs(message);
        args.CloudEvent.Should().BeNull();
        args.Properties["custom"].Should().Be(42);
        activity.GetTagItem("messaging.outcome").Should().Be("success");
        activity.Tags.Should().NotContain(tag => tag.Key.StartsWith("cloudevents.", StringComparison.Ordinal));
    }

    [TestCase("{")]
    [TestCase("")]
    [TestCase("null")]
    [TestCase("[]")]
    [TestCase("42")]
    [TestCase("{\"name\":42}")]
    public async Task InvalidPayload_UsesExplicitInvalidDataPolicyWithoutCallingHandler(string json)
    {
        var subscriber = new NativeSubscriber { InvalidDataHandling = ErrorHandling.DeadLetter };
        var args = new EventSubscriberArgs();
        var result = await subscriber.ReceiveAsync(Message(json), args);

        result.Error.Should().BeOfType<EventSubscriberDeadLetterException>();
        args.ResultingErrorHandling.Should().Be(ErrorHandling.DeadLetter);
        subscriber.Calls.Should().Be(0);
    }

    [Test]
    public async Task InvalidPayload_DefaultsToCompleteAsError()
    {
        var result = await new NativeSubscriber().ReceiveAsync(Message("{"));
        result.Error.Should().BeOfType<EventSubscriberHandledException>();
        ((IEventSubscriberException)result.Error).ErrorHandling.Should().Be(ErrorHandling.CompleteAsError);
    }

    [TestCase("{}")]
    [TestCase("{\"name\":null}")]
    public async Task ValidationFailure_UsesErrorHandlerRatherThanInvalidDataPolicy(string json)
    {
        var subscriber = new NativeSubscriber
        {
            Validator = Validator.Create<Payload>().HasProperty(x => x.Name, p => p.Mandatory()),
            InvalidDataHandling = ErrorHandling.CompleteAsSilent
        };
        subscriber.ErrorHandler.Add<ValidationException>(ErrorHandling.DeadLetter);
        var result = await subscriber.ReceiveAsync(Message(json));

        result.Error.Should().BeOfType<EventSubscriberDeadLetterException>();
        result.Error.InnerException.Should().BeOfType<ValidationException>();
        subscriber.Calls.Should().Be(0);
    }

    [Test]
    public async Task NoValidator_DoesNotInventPropertyRequirements()
    {
        var subscriber = new NativeSubscriber();
        (await subscriber.ReceiveAsync(Message("{}"))).IsSuccess.Should().BeTrue();
        subscriber.Value?.Name.Should().BeNull();
        subscriber.Calls.Should().Be(1);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task OptionalNull_IsDeliveredToNullableHandler(bool clean)
    {
        var subscriber = new OptionalSubscriber { AutoCleanValue = clean };
        (await subscriber.ReceiveAsync(Message("null"))).IsSuccess.Should().BeTrue();
        subscriber.Calls.Should().Be(1);
        subscriber.Value.Should().BeNull();
    }

    [Test]
    public async Task CustomSerializerOptions_AreHonored()
    {
        var subscriber = new NativeSubscriber
        {
            JsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, NumberHandling = JsonNumberHandling.AllowReadingFromString }
        };
        (await subscriber.ReceiveAsync(Message("{\"NAME\":\"example\",\"NUMBER\":\"42\"}"))).IsSuccess.Should().BeTrue();
        subscriber.Value?.Name.Should().Be("example");
        subscriber.Value?.Number.Should().Be(42);
    }

    [Test]
    public async Task ConverterConfigurationFailure_IsNotTreatedAsMalformedJson()
    {
        var error = new InvalidOperationException("Converter configuration failed.");
        var subscriber = new NativeSubscriber
        {
            InvalidDataHandling = ErrorHandling.DeadLetter,
            JsonSerializerOptions = new JsonSerializerOptions { Converters = { new FailingConverter(error) } }
        };
        var result = await subscriber.ReceiveAsync(Message("{}"));
        result.Error.Should().BeOfType<EventSubscriberUnhandledException>();
        result.Error.InnerException.Should().BeSameAs(error);
        subscriber.Calls.Should().Be(0);
    }

    [TestCase(42, true, true)]
    [TestCase(0, true, false)]
    [TestCase(0, false, true)]
    public async Task ScalarBody_UsesExistingRequiredValueSemantics(int value, bool required, bool success)
    {
        var subscriber = new ScalarSubscriber { Required = required, InvalidDataHandling = ErrorHandling.DeadLetter };
        var result = await subscriber.ReceiveAsync(Message(value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        result.IsSuccess.Should().Be(success);
        subscriber.Calls.Should().Be(success ? 1 : 0);
        if (success)
            subscriber.Value.Should().Be(value);
        else
            result.Error.Should().BeOfType<EventSubscriberDeadLetterException>();
    }

    [Test]
    public async Task PreReceiveHook_CanRejectWithoutDeserializing()
    {
        var subscriber = new NativeSubscriber { BeforeResult = Result.Fail(new EventSubscriberRetryException("Pre-processing rejected.")) };
        var result = await subscriber.ReceiveAsync(Message("{"));
        result.Error.Should().BeOfType<EventSubscriberRetryException>();
        subscriber.Calls.Should().Be(0);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task HandlerFailure_IsClassifiedWhetherReturnedOrThrown(bool throws)
    {
        var error = new TimeoutException("Retry.");
        var subscriber = new NativeSubscriber { Handler = _ => throws ? Task.FromException<Result>(error) : Task.FromResult(Result.Fail(error)) };
        subscriber.ErrorHandler.Add<TimeoutException>(ErrorHandling.Retry);
        var result = await subscriber.ReceiveAsync(Message("{}"));
        result.Error.Should().BeOfType<EventSubscriberRetryException>();
        result.Error.InnerException.Should().BeSameAs(error);
    }

    [Test]
    public async Task UnhandledFailure_RemainsUnhandled()
    {
        var error = new InvalidOperationException("Unexpected.");
        var subscriber = new NativeSubscriber { Handler = _ => Task.FromException<Result>(error) };
        var result = await subscriber.ReceiveAsync(Message("{}"));
        result.Error.Should().BeOfType<EventSubscriberUnhandledException>();
        result.Error.InnerException.Should().BeSameAs(error);
    }

    [Test]
    public async Task OwnCancellation_IsNotClassifiedOrDeliveredToHandler()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var subscriber = new NativeSubscriber { UnhandledErrorHandling = ErrorHandling.DeadLetter };
        var result = await subscriber.ReceiveAsync(Message("{}"), cancellationToken: cancellation.Token);
        result.Error.Should().BeOfType<OperationCanceledException>();
        subscriber.Calls.Should().Be(0);
    }

    [Test]
    public async Task CancellationAfterValidation_DoesNotCallHandler()
    {
        using var cancellation = new CancellationTokenSource();
        var subscriber = new NativeSubscriber { Validator = new CancellingValidator(cancellation) };
        var result = await subscriber.ReceiveAsync(Message("{}"), cancellationToken: cancellation.Token);
        result.Error.Should().BeOfType<OperationCanceledException>();
        subscriber.Calls.Should().Be(0);
    }

    [Test]
    public async Task UnrelatedCancellation_UsesConfiguredErrorHandler()
    {
        using var cancellation = new CancellationTokenSource();
        var subscriber = new NativeSubscriber { Handler = _ => Task.FromException<Result>(new OperationCanceledException(cancellation.Token)) };
        subscriber.ErrorHandler.Add<OperationCanceledException>(ErrorHandling.Retry);
        (await subscriber.ReceiveAsync(Message("{}"))).Error.Should().BeOfType<EventSubscriberRetryException>();
    }

    [Test]
    public async Task NativeSubscriber_RegistersWithBothReceiverTypes()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddExecutionContext();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton(new ServiceBusClient("Endpoint=sb://localhost/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA="));
        services.AzureServiceBusReceiving().WithReceiver(_ => ServiceBusReceiverOptions.CreateForQueue("native"))
            .WithSubscriber<NativeSubscriber>().Build();
        services.AzureServiceBusReceiving().WithSessionReceiver(_ => ServiceBusSessionReceiverOptions.CreateForQueue("native-session"))
            .WithSubscriber<NativeSubscriber>().Build();
        await using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ServiceBusReceiver<NativeSubscriber>>().Should().NotBeNull();
        provider.GetRequiredService<ServiceBusSessionReceiver<NativeSubscriber>>().Should().NotBeNull();
        await using var scope = provider.CreateAsyncScope();
        (await scope.ServiceProvider.GetRequiredService<NativeSubscriber>().ReceiveAsync(Message("{}"))).IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task NativeSubscriber_DoesNotRequireFormatterRegistration()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTransient<LoggerOnlySubscriber>();
        await using var provider = services.BuildServiceProvider();
        provider.GetService<IEventFormatter>().Should().BeNull();
        var subscriber = provider.GetRequiredService<LoggerOnlySubscriber>();
        (await subscriber.ReceiveAsync(Message("{}"))).IsSuccess.Should().BeTrue();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task AutoCleanValue_IsOptInAndRunsBeforeValidation(bool clean)
    {
        new NativeSubscriber().AutoCleanValue.Should().BeFalse();
        string? validatedName = null;
        var subscriber = new NativeSubscriber
        {
            AutoCleanValue = clean,
            Validator = Validator.Create<Payload>().AdditionalAsync((context, _) =>
            {
                validatedName = context.Value.Name;
                return Task.CompletedTask;
            })
        };
        var message = Message("{\"name\":\"hello \"}");

        (await subscriber.ReceiveAsync(message)).IsSuccess.Should().BeTrue();

        subscriber.Value!.Name.Should().Be(clean ? "hello" : "hello ");
        validatedName.Should().Be(subscriber.Value.Name);
        subscriber.Message.Should().BeSameAs(message);
        message.Body.ToString().Should().Be("{\"name\":\"hello \"}");
    }

    [Test]
    public async Task AutoCleanValue_ValidationSeesWhitespaceAsNull()
    {
        var subscriber = new NativeSubscriber
        {
            AutoCleanValue = true,
            Validator = Validator.Create<Payload>().HasProperty(x => x.Name, rule => rule.NotNull())
        };
        subscriber.ErrorHandler.Add<ValidationException>(ErrorHandling.DeadLetter);

        var result = await subscriber.ReceiveAsync(Message("{\"name\":\" \"}"));

        result.Error.Should().BeOfType<EventSubscriberDeadLetterException>();
        result.Error.InnerException.Should().BeOfType<ValidationException>();
        subscriber.Calls.Should().Be(0);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task AutoCleanValue_ScalarWhitespaceIsCheckedAfterCleanup(bool required)
    {
        var subscriber = new StringSubscriber { AutoCleanValue = true, Required = required, InvalidDataHandling = ErrorHandling.DeadLetter };
        var result = await subscriber.ReceiveAsync(Message("\" \""));

        result.IsSuccess.Should().Be(!required);
        subscriber.Calls.Should().Be(required ? 0 : 1);
        subscriber.Value.Should().BeNull();
        if (required)
            result.Error.Should().BeOfType<EventSubscriberDeadLetterException>();
    }

    [Test]
    public async Task AutoCleanValue_FailureIsNotMalformedJson()
    {
        var subscriber = new FailingCleanSubscriber { AutoCleanValue = true, InvalidDataHandling = ErrorHandling.DeadLetter };
        var result = await subscriber.ReceiveAsync(Message("{}"));

        result.Error.Should().BeOfType<EventSubscriberUnhandledException>();
        result.Error.InnerException.Should().BeOfType<InvalidOperationException>().Which.Message.Should().Be("Cleanup failed.");
    }

    [Test]
    public async Task AutoCleanValue_MalformedJsonStillUsesInvalidDataPolicy()
    {
        var subscriber = new NativeSubscriber { AutoCleanValue = true, InvalidDataHandling = ErrorHandling.DeadLetter };

        (await subscriber.ReceiveAsync(Message("{"))).Error.Should().BeOfType<EventSubscriberDeadLetterException>();
        subscriber.Calls.Should().Be(0);
    }

    private sealed class StringSubscriber() : ServiceBusSubscriberBase<string?>(NullLogger<ServiceBusSubscriberBase>.Instance)
    {
        public bool Required { get; set; }

        public override bool ValueIsRequired => Required;

        public string? Value { get; private set; }

        public int Calls { get; private set; }

        protected override Task<Result> OnReceiveAsync(string? value, ServiceBusReceivedMessage message, EventSubscriberArgs args, CancellationToken cancellationToken)
        {
            Value = value;
            Calls++;
            return Result.SuccessTask;
        }
    }

    public sealed class FailingCleanPayload : CoreEx.Entities.Abstractions.IWrappedValueCleaner<FailingCleanPayload>
    {
        public FailingCleanPayload Clean(CoreEx.Entities.CleanArgs args) => throw new InvalidOperationException("Cleanup failed.");
    }

    private sealed class FailingCleanSubscriber() : ServiceBusSubscriberBase<FailingCleanPayload>(NullLogger<ServiceBusSubscriberBase>.Instance)
    {
        protected override Task<Result> OnReceiveAsync(FailingCleanPayload value, ServiceBusReceivedMessage message, EventSubscriberArgs args, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Handler must not run.");
    }

    private sealed class LoggerOnlySubscriber(ILogger<ServiceBusSubscriberBase> logger) : ServiceBusSubscriberBase<Payload>(logger)
    {
        protected override Task<Result> OnReceiveAsync(Payload value, ServiceBusReceivedMessage message, EventSubscriberArgs args, CancellationToken cancellationToken = default) => Result.SuccessTask;
    }

    private static ServiceBusReceivedMessage Message(string body)
        => ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString(body), messageId: "native-id", subject: "not-a-cloudevent", contentType: "application/json");

    public sealed record Payload(string? Name, int Number);

    private sealed class NativeSubscriber() : ServiceBusSubscriberBase<Payload>(NullLogger<ServiceBusSubscriberBase>.Instance)
    {
        public Payload? Value { get; private set; }
        public ServiceBusReceivedMessage? Message { get; private set; }
        public EventSubscriberArgs? Args { get; private set; }
        public CancellationToken Token { get; private set; }
        public int Calls { get; private set; }
        public IValidator<Payload>? Validator { get; set; }
        public Func<CancellationToken, Task<Result>> Handler { get; set; } = _ => Result.SuccessTask;
        public Result BeforeResult { get; set; } = Result.Success;
        public override IValidator<Payload>? ValueValidator => Validator;

        protected override Task<Result> OnBeforeReceiveAsync(ServiceBusReceivedMessage message, EventSubscriberArgs args, CancellationToken cancellationToken = default)
            => Task.FromResult(BeforeResult);

        protected override Task<Result> OnReceiveAsync(Payload value, ServiceBusReceivedMessage message, EventSubscriberArgs args, CancellationToken cancellationToken = default)
        {
            Calls++;
            Value = value;
            Message = message;
            Args = args;
            Token = cancellationToken;
            return Handler(cancellationToken);
        }
    }

    private sealed class OptionalSubscriber() : ServiceBusSubscriberBase<Payload?>(NullLogger<ServiceBusSubscriberBase>.Instance)
    {
        public override bool ValueIsRequired => false;
        public Payload? Value { get; private set; }
        public int Calls { get; private set; }

        protected override Task<Result> OnReceiveAsync(Payload? value, ServiceBusReceivedMessage message, EventSubscriberArgs args, CancellationToken cancellationToken = default)
        {
            Value = value;
            Calls++;
            return Result.SuccessTask;
        }
    }

    private sealed class CancellingValidator(CancellationTokenSource cancellation) : Validator<Payload>
    {
        protected override Task OnValidateAsync(ValidationContext<Payload> context, CancellationToken cancellationToken = default)
        {
            cancellationToken.Should().Be(cancellation.Token);
            cancellation.Cancel();
            return Task.CompletedTask;
        }
    }

    private sealed class ScalarSubscriber() : ServiceBusSubscriberBase<int>(NullLogger<ServiceBusSubscriberBase>.Instance)
    {
        public bool Required { get; set; } = true;
        public override bool ValueIsRequired => Required;
        public int Value { get; private set; }
        public int Calls { get; private set; }

        protected override Task<Result> OnReceiveAsync(int value, ServiceBusReceivedMessage message, EventSubscriberArgs args, CancellationToken cancellationToken = default)
        {
            Value = value;
            Calls++;
            return Result.SuccessTask;
        }
    }

    private sealed class FailingConverter(Exception error) : JsonConverter<Payload>
    {
        public override Payload Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw error;
        public override void Write(Utf8JsonWriter writer, Payload value, JsonSerializerOptions options) => throw error;
    }
}
