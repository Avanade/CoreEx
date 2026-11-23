namespace CoreEx.AspNetCore.Test.Unit;

public class IdempotencyKeyTests
{
    [SetUp]
    [TearDown]
    public void ResetExecutionContext() => CoreEx.ExecutionContext.Reset();

    private static void SetTenant(string? tenantId)
    {
        CoreEx.ExecutionContext.Reset();
        if (tenantId is not null)
            CoreEx.ExecutionContext.SetCurrent(new CoreEx.ExecutionContext { TenantId = tenantId });
    }

    [Test]
    public async Task CreateFromHttpRequestAsync_LargeBody_ComputesStableFingerprintAndRewinds()
    {
        var body = new byte[200_000];
        new Random(42).NextBytes(body);

        async Task<string?> FingerprintAsync()
        {
            var context = new DefaultHttpContext();
            context.Request.Method = "POST";
            context.Request.Body = new MemoryStream(body);
            context.Request.ContentLength = body.Length;

            var key = await IdempotencyKey.CreateFromHttpRequestAsync(context, []);
            context.Request.Body.Position.Should().Be(0);
            return key.Fingerprint;
        }

        var fingerprint1 = await FingerprintAsync();
        var fingerprint2 = await FingerprintAsync();

        fingerprint1.Should().NotBeNullOrEmpty();
        fingerprint1.Should().Be(fingerprint2);
    }

    [Test]
    public async Task CreateFromHttpRequestAsync_IncludesRawQueryAndPreservesPathCasing()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/Items";
        context.Request.QueryString = new QueryString("?x=1");
        var first = await IdempotencyKey.CreateFromHttpRequestAsync(context, []);

        context.Request.QueryString = new QueryString("?x=2");
        var changedQuery = await IdempotencyKey.CreateFromHttpRequestAsync(context, []);
        context.Request.Path = "/items";
        var changedPath = await IdempotencyKey.CreateFromHttpRequestAsync(context, []);

        changedQuery.Fingerprint.Should().NotBe(first.Fingerprint);
        changedPath.Fingerprint.Should().NotBe(changedQuery.Fingerprint);
        context.Request.Method = "get";
        var changedMethod = await IdempotencyKey.CreateFromHttpRequestAsync(context, []);
        changedMethod.Fingerprint.Should().NotBe(changedPath.Fingerprint);
    }

    [Test]
    public void TryParseHeader_HandlesQuotedStringsAndRelaxedLegacyKeys()
    {
        IdempotencyKey.TryParseHeader("legacy-key_123", strict: false, out var bare).Should().BeTrue();
        bare.Should().Be("legacy-key_123");
        IdempotencyKey.TryParseHeader("\"legacy-key_123\"", strict: true, out var quoted).Should().BeTrue();
        quoted.Should().Be(bare);
        IdempotencyKey.TryParseHeader("\"abc\\\"defgh\\\\ijk\"", strict: true, out var escaped).Should().BeTrue();
        escaped.Should().Be("abc\"defgh\\ijk");
        IdempotencyKey.TryParseHeader("\"legacy-key_123\";retry=?1", strict: true, out _).Should().BeFalse();
        IdempotencyKey.TryParseHeader("legacy-key_123", strict: true, out _).Should().BeFalse();
        IdempotencyKey.TryParseHeader("\"legacy-key_123\", \"other-key_123\"", strict: false, out _).Should().BeFalse();
        IdempotencyKey.TryParseHeader("\"legacy-key_123\\q\"", strict: false, out _).Should().BeFalse();
        IdempotencyKey.TryParseHeader("\"legacy-key_123\";bad param=1", strict: false, out _).Should().BeFalse();
        IdempotencyKey.TryParseHeader($"\"{new string('\\', 129).Replace("\\", "\\\\")}\"", strict: true, out _).Should().BeFalse();
        IdempotencyKey.TryParseHeader("\"short\"", strict: true, out _).Should().BeFalse();
        IdempotencyKey.TryParseHeader("legacy-key_123\n", strict: false, out _).Should().BeFalse();
    }

    [Test]
    public async Task OnInvokeAsync_ReplaysNormallyRenderedErrorsAndExcludesCallerCookie()
    {
        using var services = CreateServices();
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IIdempotencyProvider>();
        var first = CreateRequest("test-key-12345678");
        var calls = 0;

        await provider.OnInvokeAsync(new IdempotencyKeyAttribute(), first.HttpContext, context =>
        {
            calls++;
            context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            context.Response.Headers[HeaderNames.SetCookie] = "session=private";
            return context.Response.WriteAsync("rendered error");
        });

        var replay = CreateRequest("\"test-key-12345678\"");
        await provider.OnInvokeAsync(new IdempotencyKeyAttribute(), replay.HttpContext, _ => throw new InvalidOperationException("The completed response should be replayed."));

        calls.Should().Be(1);
        replay.HttpContext.Response.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        ReadBody(replay).Should().Be("rendered error");
        replay.HttpContext.Response.Headers.Should().NotContainKey(HeaderNames.SetCookie);
    }

    [Test]
    public async Task OnInvokeAsync_ExplicitReleaseAllowsSameKeyToReexecuteAfterCapturedResponse()
    {
        using var services = CreateServices();
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IIdempotencyProvider>();
        var first = CreateRequest("retry-release-1234");
        var calls = 0;

        await provider.OnInvokeAsync(new IdempotencyKeyAttribute(), first.HttpContext, context =>
        {
            calls++;
            var feature = context.Features.Get<IIdempotencyAttemptFeature>();
            feature.Should().NotBeNull();
            (feature ?? throw new InvalidOperationException("The idempotency attempt feature was not installed.")).ReleaseForRetry();
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return context.Response.WriteAsync("safe to retry");
        });

        first.HttpContext.Response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        first.HttpContext.Features.Get<IIdempotencyAttemptFeature>().Should().BeNull();
        var retry = CreateRequest("retry-release-1234");
        await provider.OnInvokeAsync(new IdempotencyKeyAttribute(), retry.HttpContext, context =>
        {
            calls++;
            return context.Response.WriteAsync("retried");
        });

        calls.Should().Be(2);
        ReadBody(retry).Should().Be("retried");
    }

    [Test]
    public async Task OnInvokeAsync_OrdinaryServiceUnavailableResponseRetainsAttempt()
    {
        using var services = CreateServices();
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IIdempotencyProvider>();
        var first = CreateRequest("ordinary-503-key");
        var calls = 0;

        await provider.OnInvokeAsync(new IdempotencyKeyAttribute(), first.HttpContext, context =>
        {
            calls++;
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return context.Response.WriteAsync("outcome is not known");
        });

        var replay = CreateRequest("ordinary-503-key");
        await provider.OnInvokeAsync(new IdempotencyKeyAttribute(), replay.HttpContext, _ => throw new InvalidOperationException("The response should be replayed."));

        calls.Should().Be(1);
        ReadBody(replay).Should().Be("outcome is not known");
    }

    [Test]
    public async Task OnInvokeAsync_RejectsReusedKeyWithChangedRequest()
    {
        using var services = CreateServices();
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IIdempotencyProvider>();
        var first = CreateRequest("test-key-87654321", "first");
        await provider.OnInvokeAsync(new IdempotencyKeyAttribute(), first.HttpContext, context => context.Response.WriteAsync("done"));

        var changed = CreateRequest("test-key-87654321", "changed");
        Func<Task> invoke = () => provider.OnInvokeAsync(new IdempotencyKeyAttribute(), changed.HttpContext, _ => throw new InvalidOperationException("A changed request must not execute."));

        (await invoke.Should().ThrowAsync<ConflictException>()).Which.StatusCode.Should().Be(HttpStatusCode.UnprocessableContent);
    }

    [Test]
    public async Task OnInvokeAsync_ScopesKeysByConfiguredCallerIdentity()
    {
        using var services = CreateServices();
        using var scope = services.CreateScope();
        var provider = (IdempotencyProvider)scope.ServiceProvider.GetRequiredService<IIdempotencyProvider>();
        provider.CallerIdentityProvider = context => context.Items["caller"]?.ToString();

        var first = CreateRequest("caller-key-123456", "same");
        first.HttpContext.Items["caller"] = "caller-a";
        await provider.OnInvokeAsync(new IdempotencyKeyAttribute(), first.HttpContext, context => context.Response.WriteAsync("caller-a"));

        var otherCaller = CreateRequest("caller-key-123456", "same");
        otherCaller.HttpContext.Items["caller"] = "caller-b";
        var calls = 0;
        await provider.OnInvokeAsync(new IdempotencyKeyAttribute(), otherCaller.HttpContext, context =>
        {
            calls++;
            return context.Response.WriteAsync("caller-b");
        });

        calls.Should().Be(1);
        ReadBody(otherCaller).Should().Be("caller-b");
    }

    [Test]
    public void CreateKey_RejectsMalformedUnicodeInCallerAndDecodedKey()
    {
        using var services = CreateServices();
        using var scope = services.CreateScope();
        var provider = (IdempotencyProvider)scope.ServiceProvider.GetRequiredService<IIdempotencyProvider>();
        var request = CreateRequest("unicode-key-123456");
        provider.CallerIdentityProvider = _ => "caller\uFFFD";
        var replacement = provider.CreateKey(request.HttpContext, "unicode-key-123456");
        provider.CallerIdentityProvider = _ => "caller\uD83D\uDE80";
        var supplementary = provider.CreateKey(request.HttpContext, "unicode-key-123456");
        supplementary.Should().NotBe(replacement);

        provider.CallerIdentityProvider = _ => "caller\uD800";
        Action customCaller = () => provider.CreateKey(request.HttpContext, "unicode-key-123456");
        customCaller.Should().Throw<ArgumentException>();

        provider.CallerIdentityProvider = null;
        request.HttpContext.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
            new[] { new System.Security.Claims.Claim("sub", "caller\uD801") }, "test"));
        Action subject = () => provider.CreateKey(request.HttpContext, "unicode-key-123456");
        subject.Should().Throw<ArgumentException>();

        request.HttpContext.User = new System.Security.Claims.ClaimsPrincipal();
        Action key = () => provider.CreateKey(request.HttpContext, "unicode\uDC00");
        key.Should().Throw<ArgumentException>();
    }

    [Test]
    public void CreateKey_DoesNotExposeTheLogicalKeyOrCallerIdentity()
    {
        using var services = CreateServices();
        using var scope = services.CreateScope();
        var provider = (IdempotencyProvider)scope.ServiceProvider.GetRequiredService<IIdempotencyProvider>();
        var request = CreateRequest("visible-key-123456");
        provider.CallerIdentityProvider = _ => "private-caller";

        var key = provider.CreateKey(request.HttpContext, "visible-key-123456");
        key.ToCanonicalString().Should().NotContain("visible-key").And.NotContain("private-caller");
        provider.CreateKey(request.HttpContext, "visible-key-123456").Should().Be(key);
        IdempotencyProvider.OperationName.Should().Be("http-idempotency");
    }

    [Test]
    public async Task AmbientTenant_SeparatesReplayAcrossTenantsAndDomainWide()
    {
        using var services = CreateServices();
        using var scope = services.CreateScope();
        var provider = (IdempotencyProvider)scope.ServiceProvider.GetRequiredService<IIdempotencyProvider>();
        var calls = 0;

        async Task<string> Invoke(string? tenant)
        {
            SetTenant(tenant);
            var current = CreateRequest("tenant-default-123");
            await provider.OnInvokeAsync(new IdempotencyKeyAttribute(), current.HttpContext, context => context.Response.WriteAsync((++calls).ToString()));
            return ReadBody(current);
        }

        (await Invoke("tenant-a")).Should().Be("1");
        (await Invoke("tenant-a")).Should().Be("1");
        (await Invoke("tenant-b")).Should().Be("2");
        (await Invoke(null)).Should().Be("3");
        (await Invoke(null)).Should().Be("3");
        calls.Should().Be(3);
    }

    [Test]
    public async Task OnInvokeAsync_KeysAreScopedByDomainTenantAndCaller_WithoutChangingFingerprintBehavior()
    {
        var state = new CoreEx.Coordination.InMemoryCoordinationState();
        using var orders = CreateServices("orders", state);
        using var products = CreateServices("products", state);
        using var ordersScope = orders.CreateScope();
        using var productsScope = products.CreateScope();
        var ordersProvider = (IdempotencyProvider)ordersScope.ServiceProvider.GetRequiredService<IIdempotencyProvider>();
        var productsProvider = (IdempotencyProvider)productsScope.ServiceProvider.GetRequiredService<IIdempotencyProvider>();
        ordersProvider.CallerIdentityProvider = context => context.Items["caller"]?.ToString();
        productsProvider.CallerIdentityProvider = context => context.Items["caller"]?.ToString();
        var calls = 0;

        async Task<HttpRequest> Invoke(IdempotencyProvider provider, string tenant, string caller, string body = "same")
        {
            SetTenant(tenant);
            var request = CreateRequest("scoped-key-123456", body);
            request.HttpContext.Items["caller"] = caller;
            await provider.OnInvokeAsync(new IdempotencyKeyAttribute(), request.HttpContext, context => context.Response.WriteAsync((++calls).ToString()));
            return request;
        }

        var first = await Invoke(ordersProvider, "tenant-a", "caller-a");
        var replay = await Invoke(ordersProvider, "tenant-a", "caller-a");
        ReadBody(first).Should().Be("1");
        ReadBody(replay).Should().Be("1");
        ReadBody(await Invoke(ordersProvider, "tenant-b", "caller-a")).Should().Be("2");
        ReadBody(await Invoke(productsProvider, "tenant-a", "caller-a")).Should().Be("3");
        ReadBody(await Invoke(ordersProvider, "tenant-a", "caller-b")).Should().Be("4");
        calls.Should().Be(4);
        ordersProvider.CreateKey(first.HttpContext, "scoped-key-123456").Should().Be(ordersProvider.CreateKey(replay.HttpContext, "scoped-key-123456"));

        Func<Task> changed = () => Invoke(ordersProvider, "tenant-a", "caller-a", "different");
        (await changed.Should().ThrowAsync<ConflictException>()).Which.StatusCode.Should().Be(HttpStatusCode.UnprocessableContent);
        calls.Should().Be(4);
    }

    [Test]
    public async Task HttpIdentity_CanBeReconstructedForRevisionCheckedReconciliation()
    {
        using var services = CreateServices();
        using var scope = services.CreateScope();
        var provider = (IdempotencyProvider)scope.ServiceProvider.GetRequiredService<IIdempotencyProvider>();
        SetTenant("tenant-a");
        var request = CreateRequest("reconcile-key-123");
        var key = provider.CreateKey(request.HttpContext, "reconcile-key-123");
        Func<Task> first = () => provider.OnInvokeAsync(new IdempotencyKeyAttribute(), request.HttpContext, _ => throw new InvalidOperationException("Unknown outcome."));
        await first.Should().ThrowAsync<InvalidOperationException>().WithMessage("Unknown outcome.");

        var fingerprint = await IdempotencyKey.CreateFromHttpRequestAsync(request.HttpContext, provider.HttpRequestHeadersToIncludeInFingerprint);
        var store = scope.ServiceProvider.GetRequiredService<CoreEx.Idempotency.IIdempotencyStore>();
        var decision = await store.TryBeginAsync(IdempotencyProvider.OperationName, key, fingerprint.Fingerprint ?? throw new InvalidOperationException("Missing fingerprint."));
        decision.Outcome.Should().Be(CoreEx.Idempotency.IdempotencyOutcome.Indeterminate);
        (await store.TryResolveAsync(IdempotencyProvider.OperationName, key, decision.Revision!, null, true)).Should().BeTrue();
        var retry = CreateRequest("reconcile-key-123");
        await provider.OnInvokeAsync(new IdempotencyKeyAttribute(), retry.HttpContext, context => context.Response.WriteAsync("reconciled"));
        ReadBody(retry).Should().Be("reconciled");
    }

    [Test]
    public async Task OnInvokeAsync_CaptureLimitDoesNotExposePartialResponseAndRetainsIndeterminate()
    {
        using var services = CreateServices();
        using var scope = services.CreateScope();
        var provider = (IdempotencyProvider)scope.ServiceProvider.GetRequiredService<IIdempotencyProvider>();
        provider.MaxResponseBodySize = 4;
        var request = CreateRequest("capture-limit-1234");

        Func<Task> execute = () => provider.OnInvokeAsync(new IdempotencyKeyAttribute(), request.HttpContext, context => context.Response.WriteAsync("too large"));
        await execute.Should().ThrowAsync<IOException>();
        ReadBody(request).Should().BeEmpty();

        var retry = CreateRequest("capture-limit-1234");
        Func<Task> repeat = () => provider.OnInvokeAsync(new IdempotencyKeyAttribute(), retry.HttpContext, _ => throw new InvalidOperationException("Indeterminate work must not be retried."));
        (await repeat.Should().ThrowAsync<ConflictException>()).Which.ErrorCode.Should().Be("outcome-indeterminate");
    }

    [Test]
    public async Task OnInvokeAsync_RejectsDuplicateHeaderValuesBeforeStoreAccess()
    {
        using var services = CreateServices();
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IIdempotencyProvider>();
        var request = CreateRequest(null);
        request.Headers[HttpNames.IdempotencyKeyHeaderName] = new StringValues(new[] { "test-key-12345678", "other-key-12345678" });
        var calls = 0;

        Func<Task> invoke = () => provider.OnInvokeAsync(new IdempotencyKeyAttribute(), request.HttpContext, _ =>
        {
            calls++;
            return Task.CompletedTask;
        });

        await invoke.Should().ThrowAsync<ValidationException>();
        calls.Should().Be(0);
    }

    [Test]
    public async Task OnInvokeAsync_OversizedReplaySpillsAndReturnsOriginal_WithoutPermittingRetry()
    {
        using var services = CreateServices();
        using var scope = services.CreateScope();
        var provider = (IdempotencyProvider)scope.ServiceProvider.GetRequiredService<IIdempotencyProvider>();
        provider.MaxCachedResponseBodySize = 8;
        var request = CreateRequest("large-original-123");
        var originalBody = (MemoryStream)request.HttpContext.Response.Body;
        var content = new string('x', 100_000);
        await provider.OnInvokeAsync(new IdempotencyKeyAttribute(), request.HttpContext, async context =>
        {
            await context.Response.WriteAsync(content);
            originalBody.Length.Should().Be(0);
        });

        ReadBody(request).Should().Be(content);
        var retry = CreateRequest("large-original-123");
        Func<Task> repeat = () => provider.OnInvokeAsync(new IdempotencyKeyAttribute(), retry.HttpContext, _ => throw new InvalidOperationException("The terminal response cannot be executed again."));
        (await repeat.Should().ThrowAsync<ConflictException>()).Which.ErrorCode.Should().Be("response-unavailable");
    }

    [Test]
    public async Task OnInvokeAsync_StorePayloadLimitReturnsOriginalWithoutPersistingReplay()
    {
        var attempt = new StubAttempt(CancellationToken.None);
        var store = new StubStore(attempt) { MaxPayloadSize = 64 };
        var provider = new IdempotencyProvider(store);
        var request = CreateRequest("store-limit-12345");

        await provider.OnInvokeAsync(new IdempotencyKeyAttribute(), request.HttpContext, context => context.Response.WriteAsync("response"));

        attempt.Completed.Should().BeTrue();
        attempt.CompletedResponse.Should().BeNull();
        ReadBody(request).Should().Be("response");
    }

    [Test]
    public async Task OnInvokeAsync_ReplayableBodyStaysInMemoryThroughConfiguredReplayLimit()
    {
        using var services = CreateServices();
        using var scope = services.CreateScope();
        var provider = (IdempotencyProvider)scope.ServiceProvider.GetRequiredService<IIdempotencyProvider>();
        provider.MaxCachedResponseBodySize = 64 * 1024;
        provider.MaxResponseBodySize = 128 * 1024;
        provider.TempFileDirectoryAccessor = () => throw new InvalidOperationException("A replayable response should not spill to disk.");
        var request = CreateRequest("memory-buffer-123");
        var content = new string('x', 40 * 1024);

        await provider.OnInvokeAsync(new IdempotencyKeyAttribute(), request.HttpContext, context => context.Response.WriteAsync(content));

        ReadBody(request).Should().Be(content);
    }

    [Test]
    public async Task OnInvokeAsync_ThrownApplicationFailureRemainsIndeterminate()
    {
        using var services = CreateServices();
        var provider = services.GetRequiredService<IIdempotencyProvider>();
        var request = CreateRequest("thrown-error-1234");
        Func<Task> first = () => provider.OnInvokeAsync(new IdempotencyKeyAttribute(), request.HttpContext, _ => throw new InvalidOperationException("application failure"));
        await first.Should().ThrowAsync<InvalidOperationException>().WithMessage("application failure");
        var retry = CreateRequest("thrown-error-1234");
        Func<Task> repeat = () => provider.OnInvokeAsync(new IdempotencyKeyAttribute(), retry.HttpContext, _ => throw new InvalidOperationException("Must not execute."));
        (await repeat.Should().ThrowAsync<ConflictException>()).Which.ErrorCode.Should().Be("outcome-indeterminate");
        ReadBody(request).Should().BeEmpty();
    }

    [Test]
    public async Task OnInvokeAsync_StrictModeRejectsBareKeyWithoutConsumingTheQuotedKey()
    {
        using var services = CreateServices();
        var provider = (IdempotencyProvider)services.GetRequiredService<IIdempotencyProvider>();
        provider.StrictHeaderValidation = true;
        provider.DocumentationUri = new Uri("https://example.test/idempotency");
        var request = CreateRequest("strict-header-123");
        Func<Task> bare = () => provider.OnInvokeAsync(new IdempotencyKeyAttribute(), request.HttpContext, _ => throw new InvalidOperationException("Must reject before execution."));
        await bare.Should().ThrowAsync<ValidationException>();
        request.HttpContext.Response.Headers[HeaderNames.Link].ToString().Should().Contain("<https://example.test/idempotency>; rel=\"describedby\"");
        var quoted = CreateRequest("\"strict-header-123\"");
        await provider.OnInvokeAsync(new IdempotencyKeyAttribute(), quoted.HttpContext, context => context.Response.WriteAsync("accepted"));
        ReadBody(quoted).Should().Be("accepted");
    }

    [Test]
    public async Task OnInvokeAsync_AuthenticatedSubjectIsScopedByIssuer()
    {
        using var services = CreateServices();
        var provider = services.GetRequiredService<IIdempotencyProvider>();
        foreach (var issuer in new[] { "issuer-a", "issuer-b" })
        {
            var request = CreateRequest("issuer-scoped-123");
            request.HttpContext.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
                new[] { new System.Security.Claims.Claim("sub", "same-subject", System.Security.Claims.ClaimValueTypes.String, issuer) }, "test"));
            await provider.OnInvokeAsync(new IdempotencyKeyAttribute(), request.HttpContext, context => context.Response.WriteAsync(issuer));
            ReadBody(request).Should().Be(issuer);
        }
    }

    [Test]
    public async Task OnInvokeAsync_AlreadyLostOwnershipDoesNotEnterApplication()
    {
        using var loss = new CancellationTokenSource();
        loss.Cancel();
        var attempt = new StubAttempt(loss.Token);
        var provider = new IdempotencyProvider(new StubStore(attempt));
        var request = CreateRequest("lost-owner-12345");
        var calls = 0;
        Func<Task> invoke = () => provider.OnInvokeAsync(new IdempotencyKeyAttribute(), request.HttpContext, _ => { calls++; return Task.CompletedTask; });
        await invoke.Should().ThrowAsync<OperationCanceledException>();
        calls.Should().Be(0);
        attempt.Disposed.Should().BeTrue();
        attempt.Completed.Should().BeFalse();
        ReadBody(request).Should().BeEmpty();
    }

    [Test]
    public async Task OnInvokeAsync_OwnershipLossCancelsExecutionWithoutExposingPartialBody()
    {
        using var loss = new CancellationTokenSource();
        var attempt = new StubAttempt(loss.Token);
        var provider = new IdempotencyProvider(new StubStore(attempt));
        var request = CreateRequest("mid-flight-12345");
        Func<Task> invoke = () => provider.OnInvokeAsync(new IdempotencyKeyAttribute(), request.HttpContext, async context =>
        {
            await context.Response.WriteAsync("partial");
            loss.Cancel();
            await Task.Delay(Timeout.InfiniteTimeSpan, context.RequestAborted);
        });
        await invoke.Should().ThrowAsync<OperationCanceledException>();
        attempt.Disposed.Should().BeTrue();
        attempt.Completed.Should().BeFalse();
        request.HttpContext.RequestAborted.Should().Be(CancellationToken.None);
        ReadBody(request).Should().BeEmpty();
    }

    [Test]
    public async Task OnInvokeAsync_TerminalRecordingFailureDoesNotExposeApplicationResponse()
    {
        var attempt = new StubAttempt(default) { FailCompletion = true };
        var provider = new IdempotencyProvider(new StubStore(attempt));
        var request = CreateRequest("record-failure-123");
        Func<Task> invoke = () => provider.OnInvokeAsync(new IdempotencyKeyAttribute(), request.HttpContext, context => context.Response.WriteAsync("business succeeded"));
        (await invoke.Should().ThrowAsync<TransientException>()).Which.ErrorCode.Should().Be("store-unavailable");
        attempt.Disposed.Should().BeTrue();
        ReadBody(request).Should().BeEmpty();
    }

    [Test]
    public async Task WriteToHttpResponseAsync_PreservesHeadersNotInCachedSet()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Response.Headers["X-Correlation-Id"] = "abc123";

        var cached = new IdempotencyKey
        {
            StatusCode = 200,
            Headers = new Dictionary<string, string?[]> { ["Content-Type"] = ["application/json"] },
            Body = new BinaryData("""{"ok":true}""")
        };

        await cached.WriteToHttpResponseAsync(context);

        context.Response.Headers["X-Correlation-Id"].ToString().Should().Be("abc123");
        context.Response.Headers["Content-Type"].ToString().Should().Be("application/json");
    }

    private static ServiceProvider CreateServices(string domainName = "orders", CoreEx.Coordination.InMemoryCoordinationState? state = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TestHostSettings(domainName));
        if (state is not null)
            services.AddSingleton(state);

        services.AddScoped(_ => new CoreEx.ExecutionContext());
        services.AddInMemoryCoordination();
        services.AddIdempotencyProvider();
        return services.BuildServiceProvider();
    }

    private static CoreEx.Hosting.IHostSettings TestHostSettings(string domainName = "orders")
        => new CoreEx.Hosting.HostSettings { SolutionName = "Tests", DomainName = domainName, EnvironmentName = "Test" };

    private static HttpRequest CreateRequest(string? key, string body = "")
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Method = "POST";
        context.Request.Path = "/idempotency";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Request.ContentLength = context.Request.Body.Length;
        if (key is not null)
            context.Request.Headers[HttpNames.IdempotencyKeyHeaderName] = key;

        return context.Request;
    }

    private static string ReadBody(HttpRequest request)
        => Encoding.UTF8.GetString(((MemoryStream)request.HttpContext.Response.Body).ToArray());

    private sealed class StubStore(CoreEx.Idempotency.IIdempotencyAttempt attempt) : CoreEx.Idempotency.IIdempotencyStore
    {
        public int? MaxPayloadSize { get; init; }

        public Task<CoreEx.Idempotency.IdempotencyBeginResult> TryBeginAsync(string name, CoreEx.Entities.CompositeKey key, string fingerprint, CancellationToken cancellationToken = default)
            => Task.FromResult(new CoreEx.Idempotency.IdempotencyBeginResult(CoreEx.Idempotency.IdempotencyOutcome.Acquired, attempt));

        public Task<bool> TryResolveAsync(string name, CoreEx.Entities.CompositeKey key, string revision, BinaryData? response, bool allowRetry = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubAttempt(CancellationToken ownershipLost) : CoreEx.Idempotency.IIdempotencyAttempt
    {
        public CancellationToken OwnershipLost => ownershipLost;

        public bool Disposed { get; private set; }

        public bool Completed { get; private set; }

        public BinaryData? CompletedResponse { get; private set; }

        public bool FailCompletion { get; init; }

        public Task CompleteAsync(BinaryData? response, CancellationToken cancellationToken = default)
        {
            Completed = true;
            CompletedResponse = response;
            return FailCompletion ? Task.FromException(new IOException("The terminal response acknowledgement was lost.")) : Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
