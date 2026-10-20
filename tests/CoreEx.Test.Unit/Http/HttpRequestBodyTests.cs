namespace CoreEx.Test.Unit.Http;

[TestFixture]
public class HttpRequestBodyTests
{
    [TestCase("GET")]
    [TestCase("POST")]
    [TestCase("PUT")]
    [TestCase("PATCH")]
    [TestCase("DELETE")]
    public async Task VerbShortcuts_BuildWithoutSending_AndDoNotImplyContent(string verb)
    {
        var calls = 0;
        using var handler = new InspectHandler((request, _) =>
        {
            calls++;
            request.Method.Method.Should().Be(verb);
            request.RequestUri.Should().Be(new Uri("https://example.com/api"));
            request.Content.Should().BeNull();
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        });
        using var client = CreateClient(handler);
        var builder = verb switch
        {
            "GET" => client.Get("/api"),
            "POST" => client.Post("/api"),
            "PUT" => client.Put("/api"),
            "PATCH" => client.Patch("/api"),
            _ => client.Delete("/api")
        };

        calls.Should().Be(0);
        using var response = await builder.SendAsync();
        calls.Should().Be(1);
    }

    [TestCase(null, "null")]
    [TestCase("hello", "\"hello\"")]
    public async Task WithJson_SerializesNullAndStringsAsJson(string? value, string expected)
    {
        using var handler = new InspectHandler(async (request, ct) =>
        {
            (await request.Content!.ReadAsStringAsync(ct)).Should().Be(expected);
            request.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });
        using var client = CreateClient(handler);
        using var response = await client.Post("/api").WithJson(value).SendAsync();
    }

    [Test]
    public async Task WithJson_UsesExplicitSerializerOptionsAndMergePatchIsOptIn()
    {
        using var handler = new InspectHandler(async (request, ct) =>
        {
            (await request.Content!.ReadAsStringAsync(ct)).Should().Be("{\"Name\":\"Widget\"}");
            request.Content.Headers.ContentType!.MediaType.Should().Be("application/merge-patch+json");
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });
        using var client = CreateClient(handler);
        using var response = await client.Patch("/api")
            .WithJson(new { Name = "Widget" }, jsonSerializerOptions: new System.Text.Json.JsonSerializerOptions())
            .WithMergePatchJsonContentType()
            .SendAsync();
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void Body_CanOnlyBeAssignedOnce(bool firstIsContent, bool secondIsContent)
    {
        using var client = new HttpClient();
        using var first = new TrackedContent();
        using var second = new TrackedContent();
        var builder = client.Post("/api");
        if (firstIsContent)
            builder.WithContent(first);
        else
            builder.WithJson<string?>(null);

        Action assign = () =>
        {
            if (secondIsContent)
                builder.WithContent(second);
            else
                builder.WithJson("second");
        };

        assign.Should().Throw<InvalidOperationException>();
        first.IsDisposed.Should().BeFalse();
        second.IsDisposed.Should().BeFalse();
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void RequestMessage_BodyExtensionsRejectEveryDuplicateCombination(bool firstIsContent, bool secondIsContent)
    {
        using var request = new HttpRequestMessage();
        using var first = new TrackedContent();
        using var second = new TrackedContent();
        if (firstIsContent)
            request.WithContent(first);
        else
            request.WithJson<string?>(null);

        Action assign = () =>
        {
            if (secondIsContent)
                request.WithContent(second);
            else
                request.WithJson("second");
        };

        assign.Should().Throw<InvalidOperationException>();
        second.IsDisposed.Should().BeFalse();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task WithContent_PreservesRawContentAndDisposesIt_EvenOnConfigurationFailure(bool fail)
    {
        using var content = new TrackedContent();
        using var handler = new InspectHandler(async (request, ct) =>
        {
            request.Content.Should().BeSameAs(content);
            content.IsDisposed.Should().BeFalse();
            (await request.Content.ReadAsStringAsync(ct)).Should().Be("raw");
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });
        using var client = CreateClient(handler);
        var builder = client.Post("/api").WithContent(content);
        if (fail)
            builder.Configure(_ => throw new ArgumentException("Configuration failed."));

        if (fail)
        {
            Func<Task> send = () => builder.SendAsync();
            await send.Should().ThrowAsync<ArgumentException>();
        }
        else
        {
            using var response = await builder.SendAsync();
        }

        content.IsDisposed.Should().BeTrue();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Configure_CannotOverrideFluentBody_RegardlessOfRegistrationOrder(bool configureFirst)
    {
        using var original = new TrackedContent();
        using var replacement = new TrackedContent();
        using var handler = new InspectHandler((_, _) => throw new AssertionException("Conflicting body must not be sent."));
        using var client = CreateClient(handler);
        var builder = client.Post("/api");
        if (configureFirst)
            builder.Configure(r => r.Content = replacement);

        builder.WithContent(original);
        if (!configureFirst)
            builder.Configure(r => r.Content = replacement);

        Func<Task> send = () => builder.SendAsync();
        await send.Should().ThrowAsync<InvalidOperationException>();
        original.IsDisposed.Should().BeTrue();
        replacement.IsDisposed.Should().BeTrue();
    }

    [Test]
    public async Task Configure_CannotRemoveBody()
    {
        using var content = new TrackedContent();
        using var client = new HttpClient();
        var builder = client.Post("/api").WithContent(content).Configure(r => r.Content = null);
        Func<Task> send = () => builder.SendAsync();
        await send.Should().ThrowAsync<InvalidOperationException>();
        content.IsDisposed.Should().BeTrue();
    }

    [Test]
    public async Task Configure_CanSupplyBodyWithoutFluentAssignment()
    {
        using var handler = new InspectHandler(async (request, ct) =>
        {
            (await request.Content!.ReadAsStringAsync(ct)).Should().Be("\"value\"");
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });
        using var client = CreateClient(handler);
        using var response = await client.Post("/api").Configure(r => r.WithJson("value")).SendAsync();
    }

    [TestCase("success")]
    [TestCase("failure")]
    [TestCase("cancelled")]
    [TestCase("configuration")]
    public async Task Builder_IsConsumedOnFirstAttempt(string outcome)
    {
        using var handler = new InspectHandler((_, _) => outcome == "failure"
            ? Task.FromException<HttpResponseMessage>(new HttpRequestException("Failed."))
            : Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)));
        using var client = CreateClient(handler);
        var builder = client.Get("/api");
        using var cancellation = new CancellationTokenSource();
        if (outcome == "cancelled")
            cancellation.Cancel();
        if (outcome == "configuration")
            builder.Configure(_ => throw new ArgumentException("Failed."));

        if (outcome == "success")
        {
            using var response = await builder.SendAsync();
        }
        else
        {
            Func<Task> first = () => builder.SendAsync(cancellation.Token);
            if (outcome == "cancelled")
                await first.Should().ThrowAsync<OperationCanceledException>();
            else if (outcome == "configuration")
                await first.Should().ThrowAsync<ArgumentException>();
            else
                await first.Should().ThrowAsync<HttpRequestException>();
        }

        Func<Task> second = () => builder.GetResponseAsync();
        await second.Should().ThrowAsync<InvalidOperationException>();
        Action configure = () => builder.WithPaging(take: 1);
        Action body = () => builder.WithJson("late");
        using var lateContent = new TrackedContent();
        Action content = () => builder.WithContent(lateContent);
        Action status = () => builder.WithStatusResult(System.Net.HttpStatusCode.OK, CoreEx.Results.Result.Success);
        configure.Should().Throw<InvalidOperationException>();
        body.Should().Throw<InvalidOperationException>();
        content.Should().Throw<InvalidOperationException>();
        status.Should().Throw<InvalidOperationException>();
        lateContent.IsDisposed.Should().BeFalse();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task WithContent_IsDisposedOnSendFailureOrCancellation(bool cancel)
    {
        using var content = new TrackedContent();
        using var handler = new InspectHandler((_, _) => Task.FromException<HttpResponseMessage>(new HttpRequestException("Failed.")));
        using var client = CreateClient(handler);
        using var cancellation = new CancellationTokenSource();
        if (cancel)
            cancellation.Cancel();

        Func<Task> send = () => client.Post("/api").WithContent(content).SendAsync(cancellation.Token);
        if (cancel)
            await send.Should().ThrowAsync<OperationCanceledException>();
        else
            await send.Should().ThrowAsync<HttpRequestException>();

        content.IsDisposed.Should().BeTrue();
    }

    [Test]
    public void NullContent_IsRejectedWithoutConsumingBodySlot()
    {
        using var client = new HttpClient();
        var builder = client.Post("/api");
        Action assign = () => builder.WithContent(null!);
        assign.Should().Throw<ArgumentNullException>();
        builder.WithJson("valid").Should().BeSameAs(builder);

        using var request = new HttpRequestMessage();
        Action assignMessage = () => request.WithContent(null!);
        assignMessage.Should().Throw<ArgumentNullException>();
        request.WithJson("valid").Should().BeSameAs(request);
    }

    [Test]
    public async Task ConcurrentExecution_OnlySendsOnce_AndRejectsConfigurationWhileSending()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        using var handler = new InspectHandler(async (_, ct) =>
        {
            Interlocked.Increment(ref calls);
            started.SetResult();
            await release.Task.WaitAsync(ct);
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });
        using var client = CreateClient(handler);
        var builder = client.Get("/api");
        var first = builder.SendAsync();
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Func<Task> second = () => builder.ToResultAsync();
            await second.Should().ThrowAsync<InvalidOperationException>();
            Action configure = () => builder.Configure(_ => { });
            configure.Should().Throw<InvalidOperationException>();
        }
        finally
        {
            release.SetResult();
            using var response = await first;
        }

        calls.Should().Be(1);
    }

    private static HttpClient CreateClient(HttpMessageHandler handler) => new(handler) { BaseAddress = new Uri("https://example.com/") };

    private sealed class InspectHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> inspect) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => inspect(request, cancellationToken);
    }

    private sealed class TrackedContent() : StringContent("raw")
    {
        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }
}
