using CoreEx.Events.Publishing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreEx.Events.Test.Unit.Publishing;

[TestFixture]
public class NamedDestinationProviderTests
{
    [SetUp]
    public void SetUp() => ExecutionContext.Reset();

    [TearDown]
    public void TearDown() => ExecutionContext.Reset();

    private static void SetConfiguration(params (string Key, string Value)[] settings)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings.Select(s => new KeyValuePair<string, string?>(s.Key, s.Value))).Build();
        var services = new ServiceCollection().AddSingleton<IConfiguration>(config).BuildServiceProvider();
        ExecutionContext.SetCurrent(new ExecutionContext { ServiceProvider = services });
    }

    private static void SetHostSettings(string domainName)
    {
        var services = new ServiceCollection().AddSingleton<CoreEx.Hosting.IHostSettings>(new CoreEx.Hosting.HostSettings { SolutionName = "Contoso", DomainName = domainName, EnvironmentName = "Test" }).BuildServiceProvider();
        ExecutionContext.SetCurrent(new ExecutionContext { ServiceProvider = services });
    }

    #region DI

    [Test]
    public void AddNamedDestinationProvider_WithDestination_ShouldRegister()
    {
        using var sp = new ServiceCollection().AddNamedDestinationProvider("my-topic").BuildServiceProvider();
        var provider = sp.GetRequiredService<IDestinationProvider>();
        provider.Should().BeOfType<NamedDestinationProvider>();
        provider.CreateNew(MessageType.Command, "products").Should().Be("my-topic-products");
        provider.CreateNew(MessageType.Event).Should().Be("my-topic");
    }

    [Test]
    public void AddNamedDestinationProvider_NoDestination_ShouldUseConfiguration()
    {
        SetConfiguration(("CoreEx:Events:Destination", "configured-dest"));
        using var sp = new ServiceCollection().AddNamedDestinationProvider().BuildServiceProvider();
        sp.GetRequiredService<IDestinationProvider>().CreateNew(MessageType.Event).Should().Be("configured-dest");
    }

    #endregion

    #region Properties

    [Test]
    public void Defaults_ShouldHaveExpectedValues()
    {
        var provider = new NamedDestinationProvider();
        provider.AppendSeparatorCharacter.Should().Be('-');
    }

    [Test]
    public void Default_ShouldBeSingleton() => NamedDestinationProvider.Default.Should().BeSameAs(NamedDestinationProvider.Default);

    [Test]
    public void Destination_SetAndGet_ShouldReturnValue()
    {
        var provider = new NamedDestinationProvider { Destination = "my-destination" };
        provider.Destination.Should().Be("my-destination");
    }

    [Test]
    public void Destination_SetNull_ShouldThrowArgumentNullException()
    {
        Action act = static () => _ = new NamedDestinationProvider { Destination = null! };
        act.Should().Throw<ArgumentNullException>().WithParameterName("value");
    }

    [Test]
    public void Destination_SetEmpty_ShouldThrowArgumentException()
    {
        Action act = static () => _ = new NamedDestinationProvider { Destination = "" };
        act.Should().Throw<ArgumentException>().WithParameterName("value");
    }

    [Test]
    public void Destination_NotSetNoConfiguration_ShouldFallbackToDefault()
        => new NamedDestinationProvider().Destination.Should().Be("default");

    [Test]
    public void Destination_NotSetWithConfiguration_ShouldUseConfiguredValue()
    {
        SetConfiguration(("CoreEx:Events:Destination", "configured-dest"));
        new NamedDestinationProvider().Destination.Should().Be("configured-dest");
    }

    [Test]
    public void Destination_NotSetConfigurationWithoutSetting_ShouldFallbackToDefault()
    {
        SetConfiguration(("Other:Setting", "value"));
        new NamedDestinationProvider().Destination.Should().Be("default");
    }

    [Test]
    public void Destination_ExplicitlySetWithConfiguration_ShouldPreferExplicitValue()
    {
        SetConfiguration(("CoreEx:Events:Destination", "configured-dest"));
        new NamedDestinationProvider { Destination = "explicit" }.Destination.Should().Be("explicit");
    }

    [Test]
    public void Destination_ConfigurationResolvedOnce_ShouldBeCached()
    {
        SetConfiguration(("CoreEx:Events:Destination", "first"));
        var provider = new NamedDestinationProvider();
        provider.Destination.Should().Be("first");

        ExecutionContext.Reset();
        SetConfiguration(("CoreEx:Events:Destination", "second"));
        provider.Destination.Should().Be("first");
    }

    #endregion

    #region CreateFrom(string)

    [Test]
    public void CreateFrom_String_ShouldReturnSuppliedDestination()
        => new NamedDestinationProvider { Destination = "fixed" }.CreateFrom("any-dest").Should().Be("any-dest");

    [Test]
    public void CreateFrom_String_DeadLetter_ShouldAppendDeadLetter()
        => new NamedDestinationProvider { Destination = "fixed" }.CreateFrom("any-dest", true).Should().Be("any-dest-dead-letter");

    [Test]
    public void CreateFrom_String_DeadLetterCustomSeparator_ShouldUseSeparator()
        => new NamedDestinationProvider { Destination = "fixed", AppendSeparatorCharacter = '.' }.CreateFrom("any-dest", true).Should().Be("any-dest.dead-letter");

    #endregion

    #region CreateFrom(EventData)

    [Test]
    public void CreateFrom_EventData_Null_ShouldThrowArgumentNullException()
    {
        var provider = new NamedDestinationProvider { Destination = "fixed" };
        Action act = () => provider.CreateFrom((EventData)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void CreateFrom_EventData_Event_ShouldIgnoreDomainName()
    {
        var provider = new NamedDestinationProvider { Destination = "fixed" };
        var ed = new EventData { MessageType = MessageType.Event, DomainName = "shopping" };
        provider.CreateFrom(ed).Should().Be("fixed");
    }

    [Test]
    public void CreateFrom_EventData_Event_DeadLetter_ShouldAppendDeadLetter()
    {
        var provider = new NamedDestinationProvider { Destination = "fixed" };
        var ed = new EventData { MessageType = MessageType.Event, DomainName = "shopping" };
        provider.CreateFrom(ed, true).Should().Be("fixed-dead-letter");
    }

    [Test]
    public void CreateFrom_EventData_Command_ShouldAppendDomainName()
    {
        var provider = new NamedDestinationProvider { Destination = "fixed" };
        var ed = new EventData { MessageType = MessageType.Command, DomainName = "shopping" };
        provider.CreateFrom(ed).Should().Be("fixed-shopping");
    }

    [Test]
    public void CreateFrom_EventData_Command_DeadLetter_ShouldAppendDomainNameThenDeadLetter()
    {
        var provider = new NamedDestinationProvider { Destination = "fixed" };
        var ed = new EventData { MessageType = MessageType.Command, DomainName = "shopping" };
        provider.CreateFrom(ed, true).Should().Be("fixed-shopping-dead-letter");
    }

    [Test]
    public void CreateFrom_EventData_Command_NoDomainName_ShouldSelfAddressUsingHostDomain()
    {
        SetHostSettings("Products");
        var provider = new NamedDestinationProvider { Destination = "fixed" };
        provider.CreateFrom(new EventData { MessageType = MessageType.Command }).Should().Be("fixed-Products");
    }

    [Test]
    public void CreateFrom_EventData_Command_NoDomainNameNoHostSettings_ShouldThrowInvalidOperationException()
    {
        var provider = new NamedDestinationProvider { Destination = "fixed" };
        Action act = () => provider.CreateFrom(new EventData { MessageType = MessageType.Command });
        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void CreateFrom_EventData_UnsupportedMessageType_ShouldThrowNotSupportedException()
    {
        var provider = new NamedDestinationProvider { Destination = "fixed" };
        var ed = new EventData { MessageType = (MessageType)999 };
        Action act = () => provider.CreateFrom(ed);
        act.Should().Throw<NotSupportedException>();
    }

    #endregion

    #region CreateNew

    [Test]
    public void CreateNew_DefaultParams_ShouldReturnDestination()
        => new NamedDestinationProvider { Destination = "fixed" }.CreateNew().Should().Be("fixed");

    [Test]
    public void CreateNew_DefaultParams_NoDestinationSet_ShouldReturnConfiguredDestination()
    {
        SetConfiguration(("CoreEx:Events:Destination", "configured-dest"));
        new NamedDestinationProvider().CreateNew().Should().Be("configured-dest");
    }

    [Test]
    public void CreateNew_Event_ShouldIgnoreDomainName()
        => new NamedDestinationProvider { Destination = "fixed" }.CreateNew(MessageType.Event, "shopping").Should().Be("fixed");

    [Test]
    public void CreateNew_Event_DeadLetter_ShouldAppendDeadLetter()
        => new NamedDestinationProvider { Destination = "fixed" }.CreateNew(MessageType.Event, "shopping", true).Should().Be("fixed-dead-letter");

    [Test]
    public void CreateNew_Command_WithDomainName_ShouldAppendDomainName()
        => new NamedDestinationProvider { Destination = "fixed" }.CreateNew(MessageType.Command, "shopping").Should().Be("fixed-shopping");

    [Test]
    public void CreateNew_Command_WithDomainName_DeadLetter_ShouldAppendDomainNameThenDeadLetter()
        => new NamedDestinationProvider { Destination = "fixed" }.CreateNew(MessageType.Command, "shopping", true).Should().Be("fixed-shopping-dead-letter");

    [TestCase(null)]
    [TestCase("")]
    public void CreateNew_Command_NullOrEmptyDomainName_ShouldSelfAddressUsingHostDomain(string? domainName)
    {
        SetHostSettings("Products");
        var provider = new NamedDestinationProvider { Destination = "fixed" };
        provider.CreateNew(MessageType.Command, domainName).Should().Be("fixed-Products");
        provider.CreateNew(MessageType.Command, domainName, true).Should().Be("fixed-Products-dead-letter");
    }

    [Test]
    public void CreateNew_Command_ExplicitDomainName_ShouldPreferExplicitOverHostDomain()
    {
        SetHostSettings("Products");
        new NamedDestinationProvider { Destination = "fixed" }.CreateNew(MessageType.Command, "shopping").Should().Be("fixed-shopping");
    }

    [TestCase(null)]
    [TestCase("")]
    public void CreateNew_Command_NullOrEmptyDomainNameNoHostSettings_ShouldThrowInvalidOperationException(string? domainName)
    {
        var provider = new NamedDestinationProvider { Destination = "fixed" };
        Action act = () => provider.CreateNew(MessageType.Command, domainName);
        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void CreateFrom_EventData_CreateCommand_ShouldUseTargetDomain()
    {
        var provider = new NamedDestinationProvider { Destination = "fixed" };
        provider.CreateFrom(EventData.CreateCommand("shopping", "order", "place")).Should().Be("fixed-shopping");
    }

    [Test]
    public void CreateNew_Command_CustomSeparator_ShouldUseSeparatorForDomainAndDeadLetter()
    {
        var provider = new NamedDestinationProvider { Destination = "fixed", AppendSeparatorCharacter = '_' };
        provider.CreateNew(MessageType.Command, "shopping").Should().Be("fixed_shopping");
        provider.CreateNew(MessageType.Command, "shopping", true).Should().Be("fixed_shopping_dead-letter");
    }

    [Test]
    public void CreateNew_ReplyTo_ShouldThrowNotSupportedException()
    {
        var provider = new NamedDestinationProvider { Destination = "fixed" };
        Action act = () => provider.CreateNew(MessageType.ReplyTo);
        act.Should().Throw<NotSupportedException>().WithMessage("*ReplyTo*");
    }

    [Test]
    public void CreateNew_UnsupportedMessageType_ShouldThrowNotSupportedException()
    {
        var provider = new NamedDestinationProvider { Destination = "fixed" };
        Action act = () => provider.CreateNew((MessageType)999);
        act.Should().Throw<NotSupportedException>().WithMessage("*999*");
    }

    #endregion
}
