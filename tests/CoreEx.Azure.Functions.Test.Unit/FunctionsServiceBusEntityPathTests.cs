namespace CoreEx.Azure.Functions.Test.Unit;

public class FunctionsServiceBusEntityPathTests
{
    [TestCase("literal", "literal")]
    [TestCase("%Queue%", "orders")]
    [TestCase("%Topic%/Subscriptions/%Subscription%", "events/Subscriptions/orders")]
    [TestCase("prefix-%Queue%-suffix", "prefix-orders-suffix")]
    [TestCase("%Queue%%Subscription%", "ordersorders")]
    [TestCase("%Queue%/%Queue%", "orders/orders")]
    [TestCase("%Verbatim%", "%Queue%")]
    public void EntityPath_ResolvesSettingsOnce(string path, string expected)
    {
        using var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Queue"] = "orders",
                ["Topic"] = "events",
                ["Subscription"] = "orders",
                ["Verbatim"] = "%Queue%"
            }).Build()).BuildServiceProvider();
        CoreEx.ExecutionContext.TryGetCurrent(out var previous);
        CoreEx.ExecutionContext.Reset();
        CoreEx.ExecutionContext.SetCurrent(new CoreEx.ExecutionContext { ServiceProvider = services });
        try
        {
            var adapter = new FunctionsServiceBusMessageActions(ServiceBusModelFactory.ServiceBusReceivedMessage(), new RecordingServiceBusMessageActions(), path);
            adapter.EntityPath.Should().Be(expected);
        }
        finally
        {
            CoreEx.ExecutionContext.Reset();
            if (previous is not null)
                CoreEx.ExecutionContext.SetCurrent(previous);
        }
    }

    [TestCase("literal", null)]
    [TestCase("%Queue%", typeof(InvalidOperationException))]
    [TestCase("bad%", typeof(ArgumentException))]
    [TestCase("%%", typeof(ArgumentException))]
    [TestCase("% %", typeof(ArgumentException))]
    public void WithoutContext_OnlyLiteralPathsSucceed(string path, Type? errorType)
    {
        CoreEx.ExecutionContext.TryGetCurrent(out var previous);
        CoreEx.ExecutionContext.Reset();
        try
        {
            Action create = () => _ = new FunctionsServiceBusMessageActions(ServiceBusModelFactory.ServiceBusReceivedMessage(), new RecordingServiceBusMessageActions(), path);
            if (errorType is null)
                create.Should().NotThrow();
            else
                Assert.Throws(errorType, () => create());
        }
        finally
        {
            if (previous is not null)
                CoreEx.ExecutionContext.SetCurrent(previous);
        }
    }

    [TestCase(null, "%Queue%", typeof(InvalidOperationException))]
    [TestCase("", "%Queue%", typeof(InvalidOperationException))]
    [TestCase(" ", "%Queue%", typeof(InvalidOperationException))]
    [TestCase("orders", "%Queue%/bad%", typeof(ArgumentException))]
    [TestCase("orders", "%%", typeof(ArgumentException))]
    [TestCase("orders", "% %", typeof(ArgumentException))]
    public void InvalidPath_FailsBeforeHandlerOrSettlement(string? value, string path, Type errorType)
    {
        using var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Queue"] = value }).Build())
            .BuildServiceProvider();
        CoreEx.ExecutionContext.TryGetCurrent(out var previous);
        CoreEx.ExecutionContext.Reset();
        CoreEx.ExecutionContext.SetCurrent(new CoreEx.ExecutionContext { ServiceProvider = services });
        try
        {
            var actions = new RecordingServiceBusMessageActions();
            var invoked = false;
            Func<Task> receive = () => actions.FunctionReceiveAsync(ServiceBusModelFactory.ServiceBusReceivedMessage(), path, _ =>
            {
                invoked = true;
                return Result.SuccessTask;
            });
            var error = Assert.ThrowsAsync(errorType, async () => await receive());
            if (errorType == typeof(InvalidOperationException))
                error!.Message.Should().Contain("'Queue'");

            invoked.Should().BeFalse();
            actions.Settlements.Should().BeEmpty();
        }
        finally
        {
            CoreEx.ExecutionContext.Reset();
            if (previous is not null)
                CoreEx.ExecutionContext.SetCurrent(previous);
        }
    }
}
