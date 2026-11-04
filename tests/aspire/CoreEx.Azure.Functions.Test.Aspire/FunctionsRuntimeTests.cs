namespace CoreEx.Azure.Functions.Test.Aspire;

[TestFixture]
[NonParallelizable]
public class FunctionsRuntimeTests : WithAspireTester<Projects.CoreEx_Azure_Functions_Test_AppHost>
{
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public async Task StartAsync()
    {
        IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Should().NotContain(endpoint =>
            new[] { 7099, 5682, 5310, 10000, 10001, 10002, 14335 }.Contains(endpoint.Port), "the fixture owns its host and emulators");
        try
        {
            await Test.WaitForResourceAsync("functions", TimeSpan.FromMinutes(4));
            await Test.WaitForFunctionsAsync("functions", ["ReadContext", "ReadTraceObservation", "Create", "Failure", "Messages", "ProcessMessage", "ProcessNativeMessage",
                "CreateIdempotent", "CreateIdempotentRequired", "ReadIdempotencyCount", "ReleaseIdempotencyOperation"], TimeSpan.FromSeconds(90));
        }
        catch
        {
            Test.Checkpoint("Functions runtime startup failed.");
            throw;
        }
        _client = (await Test.GetDistributedApplicationAsync()).CreateHttpClient("functions", "http");
        _client.Timeout = TimeSpan.FromSeconds(30);
    }

    [OneTimeTearDown]
    public async Task StopAsync()
    {
        _client?.Dispose();
        await DisposeAsync();
    }

    [Test]
    public async Task Idempotency_ReplaysRenderedResponseAndRejectsChangedPayload()
    {
        var id = Runtime.NewGuid().ToString("N");
        using var first = await SendIdempotentAsync(id, "example", id);
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await first.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        document.RootElement.GetProperty("executions").GetInt32().Should().Be(1);
        first.Headers.Location.Should().Be(new Uri($"/api/idempotency/{id}", UriKind.Relative));
        first.Headers.ETag.Should().NotBeNull();
        first.Headers.Contains("Set-Cookie").Should().BeTrue();
        first.Headers.GetValues("X-Info-Messages").Should().Equal("Idempotent operation processed.");

        using var replay = await SendIdempotentAsync(id, "example", id);
        replay.StatusCode.Should().Be(first.StatusCode);
        (await replay.Content.ReadAsStringAsync()).Should().Be(body);
        replay.Headers.Location.Should().Be(first.Headers.Location);
        replay.Headers.ETag.Should().Be(first.Headers.ETag);
        replay.Headers.GetValues("X-Info-Messages").Should().Equal("Idempotent operation processed.");
        replay.Headers.Contains("Set-Cookie").Should().BeFalse();
        using var changed = await SendIdempotentAsync(id, "changed", id);
        await AssertIdempotencyProblemAsync(changed, HttpStatusCode.UnprocessableEntity);
        (await ReadIdempotencyCountAsync(id)).Should().Be(1);
    }

    [TestCase(null)]
    [TestCase("bad")]
    public async Task Idempotency_RequiredKeyFailsBeforeApplicationExecution(string? key)
    {
        var id = Runtime.NewGuid().ToString("N");
        using var response = await SendIdempotentAsync(id, "example", key, required: true);
        await AssertIdempotencyProblemAsync(response, HttpStatusCode.BadRequest);
        (await ReadIdempotencyCountAsync(id)).Should().Be(0);
    }

    [Test]
    public async Task Idempotency_InvalidJsonIsNotCached()
    {
        var id = Runtime.NewGuid().ToString("N");
        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/idempotency/{id}")
            {
                Content = new StringContent("{", System.Text.Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Idempotency-Key", id);
            using var response = await _client.SendAsync(request);
            await AssertIdempotencyProblemAsync(response, HttpStatusCode.BadRequest);
        }

        (await ReadIdempotencyCountAsync(id)).Should().Be(0);
    }

    [Test]
    public async Task Idempotency_OptionalMissingKeyExecutesEveryRequest()
    {
        var id = Runtime.NewGuid().ToString("N");
        using var first = await SendIdempotentAsync(id, "example", null);
        using var second = await SendIdempotentAsync(id, "example", null);
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Created);
        (await ReadIdempotencyCountAsync(id)).Should().Be(2);
    }

    [Test]
    public async Task Idempotency_FailedResponseAllowsRetry()
    {
        var id = Runtime.NewGuid().ToString("N");
        using var failure = await SendIdempotentAsync(id, "fail-once", id);
        await AssertIdempotencyProblemAsync(failure, HttpStatusCode.InternalServerError);
        (await failure.Content.ReadAsStringAsync()).Should().NotContain("Internal idempotency diagnostic.");
        using var retry = await SendIdempotentAsync(id, "fail-once", id);
        retry.StatusCode.Should().Be(HttpStatusCode.Created);
        using var replay = await SendIdempotentAsync(id, "fail-once", id);
        (await replay.Content.ReadAsStringAsync()).Should().Be(await retry.Content.ReadAsStringAsync());
        (await ReadIdempotencyCountAsync(id)).Should().Be(2);
    }

    [Test]
    public async Task Idempotency_OverlappingDuplicateIsRejected()
    {
        var id = Runtime.NewGuid().ToString("N");
        var first = SendIdempotentAsync(id, "wait", id);
        try
        {
            var entered = false;
            for (var attempt = 0; attempt < 100; attempt++)
            {
                if (await ReadIdempotencyCountAsync(id) == 1)
                {
                    entered = true;
                    break;
                }

                await Task.Delay(100);
            }

            entered.Should().BeTrue("the first request must be inside application execution before sending the duplicate");
            using var duplicate = await SendIdempotentAsync(id, "wait", id);
            await AssertIdempotencyProblemAsync(duplicate, HttpStatusCode.Conflict);
            (await ReadIdempotencyCountAsync(id)).Should().Be(1);
        }
        finally
        {
            using var release = await _client.PostAsync($"/api/idempotency-release/{id}", null);
            release.EnsureSuccessStatusCode();
            using var original = await first;
            original.StatusCode.Should().Be(HttpStatusCode.Created);
        }
    }

    private async Task<HttpResponseMessage> SendIdempotentAsync(string id, string name, string? key, bool required = false)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/{(required ? "idempotency-required" : "idempotency")}/{id}")
        {
            Content = new StringContent(JsonSerializer.Serialize(name), System.Text.Encoding.UTF8, "application/json")
        };
        if (key is not null)
            request.Headers.Add("Idempotency-Key", key);

        return await _client.SendAsync(request);
    }

    private async Task<int> ReadIdempotencyCountAsync(string id)
    {
        using var response = await _client.GetAsync($"/api/idempotency-count/{id}");
        response.EnsureSuccessStatusCode();
        return int.Parse(await response.Content.ReadAsStringAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task AssertIdempotencyProblemAsync(HttpResponseMessage response, HttpStatusCode statusCode)
    {
        response.StatusCode.Should().Be(statusCode);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("status").GetInt32().Should().Be((int)statusCode);
    }

    [Test]
    public async Task ConcurrentRequests_KeepIndependentInvocationContexts()
    {
        var results = await Task.WhenAll(Enumerable.Range(0, 12).Select(async _ =>
        {
            using var response = await _client.GetAsync("/api/context");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            document.RootElement.GetProperty("sameContext").GetBoolean().Should().BeTrue();
            document.RootElement.GetProperty("sameService").GetBoolean().Should().BeTrue();
            return document.RootElement.GetProperty("invocationId").GetString();
        }));

        results.Should().OnlyHaveUniqueItems().And.NotContainNulls();
        results.Should().AllSatisfy(id => id.Should().NotBeNullOrEmpty());
    }

    [Test]
    public async Task Create_RendersBodyLocationAndETag()
    {
        using var content = new StringContent("\"example\"", System.Text.Encoding.UTF8, "application/json");
        using var response = await _client.PostAsync("/api/values", content);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().Be(new Uri("/api/values/example", UriKind.Relative));
        response.Headers.ETag.Should().NotBeNull();
        (await response.Content.ReadAsStringAsync()).Should().Be("\"example\"");
    }

    [TestCase("known", HttpStatusCode.NotFound)]
    [TestCase("unexpected", HttpStatusCode.InternalServerError)]
    public async Task Failure_RendersSafeProblemDetails(string kind, HttpStatusCode statusCode)
    {
        using var response = await _client.GetAsync($"/api/failure/{kind}");
        response.StatusCode.Should().Be(statusCode);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var content = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);
        document.RootElement.GetProperty("status").GetInt32().Should().Be((int)statusCode);
        content.Should().NotContain("Internal diagnostic");
    }

    [Test]
    public async Task Messages_AreAddedBeforeResponseStarts()
    {
        using var response = await _client.GetAsync("/api/messages");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("X-Info-Messages").Should().Equal("Processed.");
    }

    [Test]
    public async Task InvalidJson_RendersBadRequest()
    {
        using var content = new StringContent("{", System.Text.Encoding.UTF8, "application/json");
        using var response = await _client.PostAsync("/api/values", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [TestCase("traceparent")]
    [TestCase("Diagnostic-Id")]
    [TestCase("both")]
    public async Task ServiceBus_TracingPreservesHostActivityAndConnectsProducer(string header)
    {
        const string traceParent = "00-0123456789abcdef0123456789abcdef-0123456789abcdef-01";
        const string producer = "0123456789abcdef0123456789abcdef/0123456789abcdef";
        const string connection = "Endpoint=sb://localhost:5682;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";
        await using var client = new ServiceBusClient(connection);
        await using var sender = client.CreateSender("coreex-native-functions");
        var message = new ServiceBusMessage(BinaryData.FromString("{\"outcome\":\"success\"}")) { MessageId = Runtime.NewGuid().ToString(), ContentType = "application/json" };
        message.ApplicationProperties["test-tracing"] = true;
        message.ApplicationProperties["baggage"] = "region=west%20coast";
        message.ApplicationProperties["tracestate"] = "vendor=value";
        if (header == "both")
        {
            message.ApplicationProperties["traceparent"] = traceParent;
            message.ApplicationProperties["Diagnostic-Id"] = traceParent;
        }
        else
            message.ApplicationProperties[header] = traceParent;

        await sender.SendMessageAsync(message);
        var timer = Stopwatch.StartNew();
        while (timer.Elapsed < TimeSpan.FromSeconds(60))
        {
            using var response = await _client.GetAsync($"/api/trace/{message.MessageId}");
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                await Task.Delay(100);
                continue;
            }

            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var before = document.RootElement.GetProperty("before");
            var after = document.RootElement.GetProperty("after");
            before.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
            after.GetProperty("id").GetString().Should().Be(before.GetProperty("id").GetString());
            after.GetProperty("region").GetString().Should().Be("west coast");
            var beforeContexts = before.GetProperty("contexts").EnumerateArray().Select(item => item.GetString()).ToArray();
            var beforeParents = before.GetProperty("parents").EnumerateArray().Select(item => item.GetString()).ToArray();
            var beforeLinks = before.GetProperty("links").EnumerateArray().Select(item => item.GetString()).ToArray();
            var afterLinks = after.GetProperty("links").EnumerateArray().Select(item => item.GetString()).ToArray();
            var alreadyConnected = beforeContexts.Contains(producer) || beforeParents.Contains(producer) || beforeLinks.Contains(producer);
            afterLinks.Count(link => link == producer).Should().Be(alreadyConnected ? beforeLinks.Count(link => link == producer) : 1);
            TestContext.Out.WriteLine($"Trace header={header}; host already connected={alreadyConnected}; producer links before={beforeLinks.Count(link => link == producer)}, after={afterLinks.Count(link => link == producer)}.");
            return;
        }

        Test.Checkpoint("Service Bus tracing observation timed out.");
        Assert.Fail("The real Functions invocation did not publish its trace observation within 60 seconds.");
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ServiceBus_ClassifiedSettlementAndUnhandledRedelivery(bool nativeJson)
    {
        const string connection = "Endpoint=sb://localhost:5682;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";
        await using var client = new ServiceBusClient(connection);
        var queue = nativeJson ? "coreex-native-functions" : "coreex-functions";
        await using var sender = client.CreateSender(queue);
        await using var deadLetters = client.CreateReceiver(queue, new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });
        await using var active = client.CreateReceiver(queue);
        var outcomes = new[] { "success", "handled", "reject", "retry", "unexpected", "catastrophic" };
        var messages = outcomes.ToDictionary(outcome => outcome, outcome =>
        {
            if (nativeJson)
                return new ServiceBusMessage(BinaryData.FromObjectAsJson(new { outcome })) { MessageId = Runtime.NewGuid().ToString(), ContentType = "application/json" };

            var @event = EventData.CreateCommand("functions", "message", "process")
                .WithTitle("coreex.functions.message.process").WithKey(outcome)
                .WithSource(new Uri("urn:coreex:functions:runtime"));
            return new EventFormatter().ConvertToCloudEvent(@event).ToServiceBusMessage();
        });
        if (nativeJson)
        {
            messages.Add("malformed", new ServiceBusMessage(BinaryData.FromString("{")) { MessageId = Runtime.NewGuid().ToString(), ContentType = "application/json" });
            messages.Add("missing-outcome", new ServiceBusMessage(BinaryData.FromString("{}")) { MessageId = Runtime.NewGuid().ToString(), ContentType = "application/json" });
        }

        var poisonOutcomes = nativeJson ? new[] { "reject", "malformed", "missing-outcome" } : new[] { "reject" };
        var rejectedOutcomes = poisonOutcomes.Concat(["retry", "unexpected", "catastrophic"]).ToArray();
        await sender.SendMessagesAsync(messages.Values);

        IReadOnlyList<ServiceBusReceivedMessage> rejected;
        try
        {
            rejected = await Test.EventuallyAsync(
                ct => deadLetters.PeekMessagesAsync(10, fromSequenceNumber: 0, cancellationToken: ct),
                values => values.Count == rejectedOutcomes.Length, timeout: TimeSpan.FromMinutes(2));
        }
        finally
        {
            Test.Checkpoint($"Broker processing finished for {queue}.");
        }

        rejected.Select(message => message.MessageId).Should().BeEquivalentTo(
            rejectedOutcomes.Select(outcome => messages[outcome].MessageId));
        foreach (var outcome in poisonOutcomes)
        {
            var invalid = rejected.Single(message => message.MessageId == messages[outcome].MessageId);
            invalid.DeliveryCount.Should().BeLessThan(3, "payload and validation rejection must not consume the delivery budget");
            invalid.DeadLetterReason.Should().NotBeNullOrEmpty().And.NotBe("MaxDeliveryCountExceeded");
            invalid.DeadLetterErrorDescription.Should().BeNullOrEmpty();
        }

        foreach (var outcome in new[] { "retry", "unexpected", "catastrophic" })
        {
            var message = rejected.Single(message => message.MessageId == messages[outcome].MessageId);
            message.DeliveryCount.Should().BeGreaterThanOrEqualTo(3);
            message.DeadLetterReason.Should().Be("MaxDeliveryCountExceeded");
        }

        (await active.PeekMessagesAsync(10)).Should().BeEmpty();
        Test.Checkpoint("Real Functions settlement and broker redelivery completed.");
    }
}
