namespace CoreEx.Test.Unit;

[TestFixture]
[NonParallelizable]
public class CorrelationTests
{
    [TearDown]
    public void Reset() => ExecutionContext.Reset();

    [TestCase(null)]
    [TestCase("A")]
    [TestCase("Order-123:AbC/xyz")]
    public void ValidValues_PreserveIdentityAndCopy(string? value)
    {
        using var ec = new ExecutionContext { CorrelationId = value };
        ec.CorrelationId.Should().Be(value);
        ec.CreateCopy().CorrelationId.Should().Be(value);
    }

    [TestCase("")]
    [TestCase("two words")]
    [TestCase("a,b")]
    [TestCase("a\tb")]
    [TestCase("a\r\nb")]
    [TestCase("\u007f")]
    [TestCase("\u00e9")]
    public void InvalidAssignment_PreservesPriorValue(string value)
    {
        using var ec = new ExecutionContext { CorrelationId = "prior" };
        Action assign = () => ec.CorrelationId = value;
        assign.Should().Throw<ArgumentException>();
        ec.CorrelationId.Should().Be("prior");
    }

    [Test]
    public void ExactLengthLimits()
    {
        CoreEx.Diagnostics.Correlation.IsValid(new string('x', 256)).Should().BeTrue();
        CoreEx.Diagnostics.Correlation.IsValid(new string('x', 257)).Should().BeFalse();
    }

    [Test]
    public async Task DisabledCorrelation_DoesNotEnrichReceiveOrPropagateAutomatically()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["CoreEx:Correlation:Enabled"] = "false" }).Build();
        using var services = new ServiceCollection().AddSingleton<IConfiguration>(configuration).AddLogging().AddExecutionContext().BuildServiceProvider();
        using var scope = services.CreateScope();
        var ec = scope.ServiceProvider.GetRequiredService<ExecutionContext>();
        ec.CorrelationId = "explicit";
        using var activity = new Activity("receive").Start();
        activity.SetTag(CoreEx.Diagnostics.Correlation.ActivityTagName, "existing");
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("test");
        CoreEx.Diagnostics.Correlation.IsEnabled().Should().BeFalse();
        CoreEx.Diagnostics.Correlation.BeginScope(logger).Should().BeNull();
        using (CoreEx.Diagnostics.Correlation.BeginReceive("invalid value", logger))
        {
            ec.CorrelationId.Should().Be("explicit");
            activity.GetTagItem(CoreEx.Diagnostics.Correlation.ActivityTagName).Should().Be("existing");
        }

        var terminal = new RecordingHandler();
        using var client = new HttpClient(new CorrelationIdHandler(configuration) { InnerHandler = terminal });
        using var absent = await client.GetAsync("https://example.test/");
        using var explicitRequest = new HttpRequestMessage(HttpMethod.Get, "https://example.test/");
        explicitRequest.Headers.TryAddWithoutValidation(HttpNames.CorrelationIdHeaderName, "invalid value");
        using var explicitResponse = await client.SendAsync(explicitRequest);
        terminal.Values.Should().Equal(null, "invalid value");
        ec.CorrelationId.Should().Be("explicit");
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task DisabledFactoryClients_PreserveExplicitHeadersWithoutAmbientContext(bool typed)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["CoreEx:Correlation:Enabled"] = "false";
        builder.Configuration["factory:BaseAddress"] = "https://example.test/";
        var terminal = new RecordingHandler();
        var registration = typed ? builder.AddTypedHttpClient<CorrelationHttpClient>("factory") : builder.AddNamedHttpClient("factory");
        registration.ConfigurePrimaryHttpMessageHandler(() => terminal);
        using var host = builder.Build();
        using var client = typed ? host.Services.GetRequiredService<CorrelationHttpClient>().Client : host.Services.GetRequiredService<IHttpClientFactory>().CreateClient("factory");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.TryAddWithoutValidation(HttpNames.CorrelationIdHeaderName, "invalid value");
        using var response = await client.SendAsync(request);
        terminal.Values.Should().Equal("invalid value");
    }

    [Test]
    public void Validation_AcceptsExactlyTheAllowedCharacters()
    {
        for (var c = 0; c <= char.MaxValue; c++)
            CoreEx.Diagnostics.Correlation.IsValid(((char)c).ToString()).Should().Be(c >= 33 && c <= 126 && c != 44, $"character U+{c:X4}");
    }

    [Test]
    public void AssignmentDoesNotMutateSharedActivity()
    {
        using var activity = new Activity("host").SetIdFormat(ActivityIdFormat.W3C).Start();
        var id = activity.Id;
        using var ec = new ExecutionContext();
        ExecutionContext.SetCurrent(ec);
        ec.CorrelationId = "first";
        activity.GetTagItem(CoreEx.Diagnostics.Correlation.ActivityTagName).Should().BeNull();
        using var unrelated = new ExecutionContext { CorrelationId = "unrelated" };
        using var copy = ec.CreateCopy();
        activity.GetTagItem(CoreEx.Diagnostics.Correlation.ActivityTagName).Should().BeNull();
        ec.CorrelationId = null;
        activity.GetTagItem(CoreEx.Diagnostics.Correlation.ActivityTagName).Should().BeNull();
        activity.Id.Should().Be(id);
        activity.Baggage.Should().BeEmpty();
    }

    [Test]
    public async Task ScopedChild_DoesNotInheritCorrelationOrMutateCallerActivity()
    {
        using var services = new ServiceCollection().AddExecutionContext().BuildServiceProvider();
        using var ec = new ExecutionContext { CorrelationId = "parent" };
        ExecutionContext.SetCurrent(ec);
        using var activity = new Activity("host").SetIdFormat(ActivityIdFormat.W3C).Start();
        activity.SetTag(CoreEx.Diagnostics.Correlation.ActivityTagName, "parent");
        await ExecutionContext.RunScopedAsync(services, async (child, ct) =>
        {
            child.CorrelationId.Should().BeNull();
            await Task.Yield();
            child.CorrelationId = "child";
            activity.GetTagItem(CoreEx.Diagnostics.Correlation.ActivityTagName).Should().Be("parent");
        });
        ExecutionContext.Current.Should().BeSameAs(ec);
        ec.CorrelationId.Should().Be("parent");
        activity.GetTagItem(CoreEx.Diagnostics.Correlation.ActivityTagName).Should().Be("parent");
    }

    [Test]
    public async Task ScopedChild_FailureDoesNotMutateCallerActivity()
    {
        using var services = new ServiceCollection().AddExecutionContext().BuildServiceProvider();
        using var ec = new ExecutionContext { CorrelationId = "parent" };
        ExecutionContext.SetCurrent(ec);
        using var activity = new Activity("host").SetIdFormat(ActivityIdFormat.W3C).Start();
        activity.SetTag(CoreEx.Diagnostics.Correlation.ActivityTagName, "parent");

        Func<Task> run = () => ExecutionContext.RunScopedAsync(services, (child, ct) =>
        {
            child.CorrelationId = "child";
            throw new InvalidOperationException("child failed");
        });

        await run.Should().ThrowAsync<InvalidOperationException>();
        ExecutionContext.Current.Should().BeSameAs(ec);
        ec.CorrelationId.Should().Be("parent");
        activity.GetTagItem(CoreEx.Diagnostics.Correlation.ActivityTagName).Should().Be("parent");
    }

    [Test]
    public async Task ConcurrentScopedChildren_DoNotOverwriteCallerActivityCorrelation()
    {
        using var provider = new ServiceCollection().AddExecutionContext().BuildServiceProvider();
        using var ec = new ExecutionContext { CorrelationId = "parent" };
        ExecutionContext.SetCurrent(ec);
        using var activity = new Activity("host").SetIdFormat(ActivityIdFormat.W3C).Start();
        activity.SetTag(CoreEx.Diagnostics.Correlation.ActivityTagName, "parent");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = 0;

        try
        {
            var tasks = Enumerable.Range(0, 2).Select(index => ExecutionContext.RunScopedAsync(provider, async (child, ct) =>
            {
                child.CorrelationId = $"child-{index}";
                if (Interlocked.Increment(ref started) == 2)
                    entered.SetResult();

                await release.Task.ConfigureAwait(false);
                activity.GetTagItem(CoreEx.Diagnostics.Correlation.ActivityTagName).Should().Be("parent");
            })).ToArray();

            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            activity.GetTagItem(CoreEx.Diagnostics.Correlation.ActivityTagName).Should().Be("parent");
            release.SetResult();
            await Task.WhenAll(tasks).ConfigureAwait(false);
            activity.GetTagItem(CoreEx.Diagnostics.Correlation.ActivityTagName).Should().Be("parent");
        }
        finally
        {
            release.TrySetResult();
            ExecutionContext.Reset();
        }
    }

    [Test]
    public async Task HttpHandler_OverrideSuppressionAndReuseArePerRequest()
    {
        var terminal = new RecordingHandler();
        using var client = new HttpClient(new CorrelationIdHandler { InnerHandler = terminal });
        using var ec = new ExecutionContext { CorrelationId = "ambient" };
        ExecutionContext.SetCurrent(ec);
        using var first = await client.GetAsync("https://example.test/");
        using var overridden = new HttpRequestMessage(HttpMethod.Post, "https://example.test/").WithCorrelationId("override");
        using var second = await client.SendAsync(overridden);
        using var suppressed = new HttpRequestMessage(HttpMethod.Put, "https://example.test/").WithCorrelationId(null);
        using var third = await client.SendAsync(suppressed);
        ec.CorrelationId = null;
        using var fourth = await client.DeleteAsync("https://example.test/");
        terminal.Values.Should().Equal("ambient", "override", null, null);
    }

    [Test]
    public async Task HttpHandler_InvalidExplicitHeaderNeverSends()
    {
        var terminal = new RecordingHandler();
        using var client = new HttpClient(new CorrelationIdHandler { InnerHandler = terminal });
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/");
        request.Headers.TryAddWithoutValidation(HttpNames.CorrelationIdHeaderName, ["first", "second"]);
        await ((Func<Task>)(() => client.SendAsync(request))).Should().ThrowAsync<ArgumentException>();
        terminal.Values.Should().BeEmpty();
    }

    [Test]
    public void OpenTelemetryScopes_BufferedScopesRetainExecutionSnapshotAndRefreshOnNextScope()
    {
        var processor = new CorrelationLogProcessor();
        using var factory = LoggerFactory.Create(builder => builder.AddOpenTelemetry(options =>
        {
            options.IncludeScopes = true;
            options.AddProcessor(processor);
        }));
        var logger = factory.CreateLogger("test");
        using var ec = new ExecutionContext();
        ExecutionContext.SetCurrent(ec);
        using (CoreEx.Diagnostics.Correlation.BeginReceive("first", logger))
        {
            logger.LogInformation("First.");
            ec.CorrelationId = "changed";
            logger.LogInformation("Still first.");
        }

        using (CoreEx.Diagnostics.Correlation.BeginReceive("second", logger))
            logger.LogInformation("Second.");

        using (CoreEx.Diagnostics.Correlation.BeginReceive(null, logger))
            logger.LogInformation("Cleared.");

        processor.Values.Should().Equal("first", "first", "second", null);
        ec.CorrelationId.Should().BeNull();
    }

    [Test]
    public async Task NestedLoggingScopes_ReadIndependentChildAndRestoredParentAtEmission()
    {
        var processor = new CorrelationLogProcessor();
        using var services = new ServiceCollection()
            .AddLogging(builder => builder.AddOpenTelemetry(options =>
            {
                options.IncludeScopes = true;
                options.AddProcessor(processor);
            }))
            .AddExecutionContext()
            .BuildServiceProvider();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("test");
        using var ec = new ExecutionContext { CorrelationId = "parent" };
        ExecutionContext.SetCurrent(ec);
        using (CoreEx.Diagnostics.Correlation.BeginScope(logger))
        {
            logger.LogInformation("Parent.");
            await ExecutionContext.RunScopedAsync(services, (child, ct) =>
            {
                logger.LogInformation("Empty child.");
                return Task.CompletedTask;
            });
            await ExecutionContext.RunScopedAsync(services, (child, ct) =>
            {
                logger.LogInformation("Correlated child.");
                return Task.CompletedTask;
            }, (_, child) => child.CorrelationId = "child");
            logger.LogInformation("Restored parent.");
        }
        processor.Values.Should().Equal("parent", null, "child", "parent");
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task FactoryClients_PropagateAcrossRetriesAndRespectRequestOverrides(bool typed)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["factory:BaseAddress"] = "https://example.test/";
        builder.Configuration["factory:Resilience:Retry:MaxRetryAttempts"] = "1";
        builder.Configuration["factory:Resilience:Retry:Delay"] = "00:00:00.001";
        builder.Configuration["factory:Resilience:Retry:UseJitter"] = "false";
        var terminal = new RetryingHandler();
        var registration = typed ? builder.AddTypedHttpClient<CorrelationHttpClient>("factory") : builder.AddNamedHttpClient("factory");
        registration.ConfigurePrimaryHttpMessageHandler(() => terminal);
        using var host = builder.Build();
        using var client = typed ? host.Services.GetRequiredService<CorrelationHttpClient>().Client : host.Services.GetRequiredService<IHttpClientFactory>().CreateClient("factory");
        using var ec = new ExecutionContext { CorrelationId = "ambient" };
        ExecutionContext.SetCurrent(ec);
        using var first = await client.GetAsync("/");
        first.EnsureSuccessStatusCode();
        using var overridden = new HttpRequestMessage(HttpMethod.Get, "/").WithCorrelationId("override");
        using var second = await client.SendAsync(overridden);
        using var suppressed = new HttpRequestMessage(HttpMethod.Get, "/").WithCorrelationId(null);
        using var third = await client.SendAsync(suppressed);
        terminal.Values.Should().Equal("ambient", "ambient", "override", null);
        client.DefaultRequestHeaders.Contains(HttpNames.CorrelationIdHeaderName).Should().BeFalse();
    }

    [Test]
    public void ConsoleScopes_ReadValuesAtEmissionIncludingClear()
    {
        var original = Console.Out;
        using var output = new StringWriter();
        Console.SetOut(output);
        try
        {
            using (var factory = LoggerFactory.Create(builder => builder.AddJsonConsole(options => options.IncludeScopes = true)))
            {
                var logger = factory.CreateLogger("test");
                using var ec = new ExecutionContext();
                ExecutionContext.SetCurrent(ec);
                ec.CorrelationId = "first";
                using (CoreEx.Diagnostics.Correlation.BeginScope(logger))
                {
                    logger.LogInformation("First.");
                    ec.CorrelationId = "changed";
                    logger.LogInformation("Still first.");
                }

                ec.CorrelationId = "second";
                using (CoreEx.Diagnostics.Correlation.BeginScope(logger))
                    logger.LogInformation("Second.");

                ec.CorrelationId = null;
                using (CoreEx.Diagnostics.Correlation.BeginScope(logger))
                    logger.LogInformation("Cleared.");
            }
            output.ToString().Should().Contain("\"CorrelationId\":\"first\"").And.Contain("\"CorrelationId\":\"second\"").And.Contain("\"CorrelationId\":null");
        }
        finally
        {
            Console.SetOut(original);
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task InvokerSpans_InheritCurrentCorrelationWithoutChangingParentage(bool enabled)
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name.Contains(nameof(CorrelationInvoker), StringComparison.Ordinal),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["CoreEx:Correlation:Enabled"] = enabled.ToString() }).Build();
        using var services = new ServiceCollection().AddSingleton<IConfiguration>(configuration).BuildServiceProvider();
        using var ec = new ExecutionContext { CorrelationId = "workflow", ServiceProvider = services };
        ExecutionContext.SetCurrent(ec);
        using var host = new Activity("host").SetIdFormat(ActivityIdFormat.W3C).Start();
        var invoker = new CorrelationInvoker();
        await invoker.InvokeAsync(new object(), async tracer =>
        {
            tracer.Activity.Should().NotBeNull();
            tracer.Activity!.GetTagItem(CoreEx.Diagnostics.Correlation.ActivityTagName).Should().Be(enabled ? "workflow" : null);
            tracer.Activity.TraceId.Should().Be(host.TraceId);
            tracer.Activity.ParentSpanId.Should().Be(host.SpanId);
            ec.CorrelationId = null;
            tracer.Activity.GetTagItem(CoreEx.Diagnostics.Correlation.ActivityTagName).Should().Be(enabled ? "workflow" : null);
            await Task.Yield();
        });
        host.GetTagItem(CoreEx.Diagnostics.Correlation.ActivityTagName).Should().BeNull();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task RealHttpClientSpans_UseActualHeaderAndPreserveConsumerEnrichment(bool enabled)
    {
        var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            var endpoint = (IPEndPoint)listener.LocalEndpoint;
            var serve = ServeAsync(listener, 3);
            var processor = new ActivityProcessor();
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["CoreEx:Correlation:Enabled"] = enabled.ToString() }).Build();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddOpenTelemetry().WithCoreExTelemetry().WithTracing(builder => builder.AddProcessor(processor));
            services.Configure<OpenTelemetry.Instrumentation.Http.HttpClientTraceInstrumentationOptions>(options =>
            {
                var enrich = options.EnrichWithHttpRequestMessage;
                options.EnrichWithHttpRequestMessage = (activity, request) =>
                {
                    enrich?.Invoke(activity, request);
                    activity.SetTag("consumer.enriched", true);
                };
            });
            using var provider = services.BuildServiceProvider();
            provider.GetRequiredService<TracerProvider>();
            using var client = new HttpClient(new CorrelationIdHandler(configuration) { InnerHandler = new SocketsHttpHandler() });
            using var ec = new ExecutionContext { CorrelationId = "ambient" };
            ExecutionContext.SetCurrent(ec);
            foreach (var correlation in new string?[] { "ambient", "override", null })
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"http://127.0.0.1:{endpoint.Port}/");
                if (correlation != "ambient")
                    request.WithCorrelationId(correlation);

                using var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
            }
            await serve;
            processor.Values.Should().Equal(enabled ? "ambient" : null, enabled ? "override" : null, null);
            processor.ConsumerTags.Should().OnlyContain(value => value == true);
        }
        finally
        {
            listener.Stop();
        }
    }

    [Test]
    public async Task ConcurrentScopedRequests_DoNotLeakHeadersOrContext()
    {
        using var provider = new ServiceCollection().AddExecutionContext().BuildServiceProvider();
        var tasks = Enumerable.Range(0, 8).Select(index => ExecutionContext.RunScopedAsync(provider, async (ec, ct) =>
        {
            ec.CorrelationId = $"request-{index}";
            await Task.Yield();
            var handler = new RecordingHandler();
            using var client = new HttpClient(new CorrelationIdHandler { InnerHandler = handler });
            using var response = await client.GetAsync("https://example.test/", ct);
            handler.Values.Should().Equal($"request-{index}");
        }));
        await Task.WhenAll(tasks);
        ExecutionContext.HasCurrent.Should().BeFalse();
    }

    private static async Task ServeAsync(System.Net.Sockets.TcpListener listener, int count)
    {
        for (var i = 0; i < count; i++)
        {
            using var socket = await listener.AcceptTcpClientAsync();
            await using var stream = socket.GetStream();
            var buffer = new byte[4096];
            var request = "";
            while (!request.Contains("\r\n\r\n", StringComparison.Ordinal))
            {
                var length = await stream.ReadAsync(buffer);
                if (length == 0)
                    throw new InvalidOperationException("Client disconnected before request headers.");

                request += Encoding.ASCII.GetString(buffer, 0, length);
            }
            await stream.WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"));
        }
    }

    private sealed class CorrelationInvoker : CoreEx.Invokers.InvokerBase<object> { }

    private sealed class ActivityProcessor : OpenTelemetry.BaseProcessor<Activity>
    {
        public List<string?> Values { get; } = [];
        public List<bool?> ConsumerTags { get; } = [];

        public override void OnEnd(Activity data)
        {
            if (data.Kind == ActivityKind.Client)
            {
                Values.Add(data.GetTagItem(CoreEx.Diagnostics.Correlation.ActivityTagName) as string);
                ConsumerTags.Add(data.GetTagItem("consumer.enriched") as bool?);
            }
        }
    }

    private sealed class CorrelationLogProcessor : OpenTelemetry.BaseProcessor<LogRecord>
    {
        private readonly List<List<object?>> _scopes = [];

        public List<string?> Values => [.. _scopes.Select(GetCorrelationId)];

        public override void OnEnd(LogRecord data)
        {
            var scopes = new List<object?>();
            data.ForEachScope((scope, state) => state.Add(scope.Scope), scopes);
            _scopes.Add(scopes);
        }

        private static string? GetCorrelationId(IEnumerable<object?> scopes)
        {
            var value = default(string?);
            var found = false;
            foreach (var scope in scopes.OfType<IEnumerable<KeyValuePair<string, object?>>>())
            {
                foreach (var item in scope)
                {
                    if (item.Key == "CorrelationId")
                    {
                        value = item.Value as string;
                        found = true;
                    }
                }
            }

            if (!found)
                throw new InvalidOperationException("The log record did not capture a CorrelationId scope.");

            return value;
        }
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<string?> Values { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Values.Add(request.Headers.TryGetValues(HttpNames.CorrelationIdHeaderName, out var values) ? values.Single() : null);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private sealed class CorrelationHttpClient(HttpClient client)
    {
        public HttpClient Client { get; } = client;
    }

    private sealed class RetryingHandler : HttpMessageHandler
    {
        public List<string?> Values { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Values.Add(request.Headers.TryGetValues(HttpNames.CorrelationIdHeaderName, out var values) ? values.Single() : null);
            return Task.FromResult(new HttpResponseMessage(Values.Count == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK));
        }
    }
}
