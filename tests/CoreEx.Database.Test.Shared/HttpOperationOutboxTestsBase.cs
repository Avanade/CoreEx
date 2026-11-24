namespace CoreEx.Test.Unit.Operations;

public abstract class HttpOperationOutboxTestsBase
{
    protected sealed class OutboxControl
    {
        public bool FailAfterInsert { get; set; }
        public int FlushCount { get; set; }
    }

    protected sealed record Submission(string Value);

    protected abstract ServiceProvider CreateProvider(OutboxControl control);
    protected abstract Task<(int Operations, int Events, string? Event)> ReadPersistedAsync();
    protected abstract void AssertSameDatabase(IServiceProvider services);

    [Test]
    public async Task HttpAcceptance_CommitsOnceAndRollsBackOperationAndRealOutboxTogether()
    {
        var control = new OutboxControl();
        using var provider = CreateProvider(control);
        var key = CoreEx.Entities.CompositeKey.Create("committed-submission");
        var first = await AcceptAsync(provider, key);
        first.Status.Should().Be(202);
        var firstId = first.Id ?? throw new InvalidOperationException("The accepted operation has no identifier.");
        var persisted = await ReadPersistedAsync();
        persisted.Operations.Should().Be(1);
        persisted.Events.Should().Be(1);
        using (var json = System.Text.Json.JsonDocument.Parse(persisted.Event.ThrowIfNull()))
        {
            json.RootElement.GetProperty("operationid").GetString().Should().Be(firstId.ToString("D"));
            json.RootElement.GetProperty("operationname").GetString().Should().Be("proof-submit");
        }

        var duplicate = await AcceptAsync(provider, key);
        duplicate.Status.Should().Be(202);
        duplicate.Id.Should().Be(firstId);
        control.FlushCount.Should().Be(1);
        persisted = await ReadPersistedAsync();
        persisted.Operations.Should().Be(1);
        persisted.Events.Should().Be(1);

        var rollbackKey = CoreEx.Entities.CompositeKey.Create("rolled-back-submission");
        control.FailAfterInsert = true;
        Func<Task> fail = async () => await AcceptAsync(provider, rollbackKey);
        await fail.Should().ThrowAsync<InvalidOperationException>().WithMessage("Injected failure after outbox insert.");
        control.FlushCount.Should().Be(2);
        persisted = await ReadPersistedAsync();
        persisted.Operations.Should().Be(1);
        persisted.Events.Should().Be(1);

        control.FailAfterInsert = false;
        var retried = await AcceptAsync(provider, rollbackKey, explicitPublisher: true);
        retried.Status.Should().Be(202);
        retried.Id.Should().NotBeNull().And.NotBe(firstId);
        control.FlushCount.Should().Be(3);
        persisted = await ReadPersistedAsync();
        persisted.Operations.Should().Be(2);
        persisted.Events.Should().Be(2);
    }

    protected static void ConfigureCommon(IServiceCollection services)
    {
        services.AddLogging();
        services.AddSingleton<IHostSettings>(new HostSettings { SolutionName = "Proof", DomainName = "Proof", EnvironmentName = "Test" });
        services.AddCoreExOperations().AddCoreExOperation<Submission>("proof-submit");
    }

    protected static async Task<string> ReadResourceAsync(string suffix)
    {
        var assembly = typeof(HttpOperationOutboxTestsBase).Assembly;
        var name = assembly.GetManifestResourceNames().Single(x => x.EndsWith(suffix, StringComparison.Ordinal));
        using var reader = new StreamReader(assembly.GetManifestResourceStream(name).ThrowIfNull());
        return await reader.ReadToEndAsync();
    }

    private async Task<(int Status, Guid? Id)> AcceptAsync(ServiceProvider provider, CoreEx.Entities.CompositeKey key, bool explicitPublisher = false)
    {
        using var scope = provider.CreateScope();
        AssertSameDatabase(scope.ServiceProvider);
        var ec = new CoreEx.ExecutionContext
        {
            ServiceProvider = scope.ServiceProvider,
            User = new CoreEx.Security.AuthenticationUser { Type = CoreEx.Security.AuthenticationType.AccountUser, UserName = "owner" }
        };
        CoreEx.ExecutionContext.SetCurrent(ec);
        try
        {
            var context = new Microsoft.AspNetCore.Http.DefaultHttpContext { RequestServices = scope.ServiceProvider };
            context.Request.Method = Microsoft.AspNetCore.Http.HttpMethods.Post;
            context.Request.Scheme = "http";
            context.Request.Host = new Microsoft.AspNetCore.Http.HostString("localhost");
            var webApi = new CoreEx.AspNetCore.Http.WebApi(executionContext: ec);
            var options = CoreEx.AspNetCore.Publishing.WebApiPublishOptions.Create(new Submission("proof"))
                .WithOperationOptions(new CoreEx.AspNetCore.Publishing.WebApiPublishOperationOptions { Key = key })
                .WithEvent(message => message.WithKey("proof-partition"));
            if (explicitPublisher)
                options.WithPublisher(sp => sp.GetRequiredService<CoreEx.Events.Publishing.IEventPublisher>());

            var result = await webApi.PublishAsync(context.Request, options);
            var status = ((Microsoft.AspNetCore.Http.IStatusCodeHttpResult)result).StatusCode ?? 0;
            var content = ((Microsoft.AspNetCore.Http.HttpResults.ContentHttpResult)result).ResponseContent.ThrowIfNull();
            using var json = System.Text.Json.JsonDocument.Parse(content);
            return (status, json.RootElement.GetProperty("id").GetGuid());
        }
        finally
        {
            CoreEx.ExecutionContext.Reset();
        }
    }
}
