namespace CoreEx.Test.Unit;

[TestFixture]
public class ExecutionContextScopedTests
{
    [Test]
    public async Task RunScopedAsync_PreservesParentAndUsesFreshRegisteredContextPerUnit()
    {
        var created = new List<TrackingContext>();
        await using var provider = CreateProvider(created);
        using var parent = new TrackingContext { ServiceProvider = provider, TenantId = "parent", OperationType = OperationType.Update };
        parent.AddInfoMessage("Parent message.");
        parent.Attributes["parent"] = new object();
        var parentTimestamp = new DateTimeOffset(2001, 1, 1, 0, 0, 0, TimeSpan.Zero);
        parent.Timestamp = parentTimestamp;
        ExecutionContext.SetCurrent(parent);
        ScopedDependency? previous = null;
        try
        {
            for (var index = 0; index < 2; index++)
            {
                var result = await ExecutionContext.RunScopedAsync(provider, async (context, ct) =>
                {
                    context.Should().BeOfType<TrackingContext>();
                    context.Should().NotBeSameAs(parent);
                    context.ServiceProvider.Should().NotBeSameAs(provider);
                    ExecutionContext.Current.Should().BeSameAs(context);
                    context.HasMessages.Should().BeFalse();
                    context.Attributes.Should().BeEmpty();
                    context.OperationType.Should().Be(OperationType.Unspecified);
                    context.Timestamp.Should().NotBe(parentTimestamp);
                    var dependency = ExecutionContext.GetRequiredService<ScopedDependency>();
                    dependency.TenantId.Should().Be("trusted-unit");
                    dependency.Should().NotBeSameAs(previous);
                    if (previous is not null)
                        previous.Disposed.Should().BeTrue();

                    previous = dependency;
                    context.AddWarningMessage("Unit message.");
                    context.Attributes["unit"] = index;
                    await Task.Yield();
                    ExecutionContext.Current.Should().BeSameAs(context);
                    return index;
                }, (outer, inner) =>
                {
                    outer.Should().BeSameAs(parent);
                    parent.DisposeCalls.Should().Be(0);
                    inner.TenantId = "trusted-unit";
                    inner.User = parent.User;
                    inner.UICulture = parent.UICulture;
                    ((TrackingContext)inner).CustomValue = "configured";
                }).ConfigureAwait(false);

                result.Should().Be(index);
                ExecutionContext.Current.Should().BeSameAs(parent);
                parent.ServiceProvider.Should().BeSameAs(provider);
                parent.DisposeCalls.Should().Be(0);
            }

            created.Should().HaveCount(2);
            created.Should().OnlyContain(x => x.DisposeCalls == 1 && x.ServiceProvider == null && x.CustomValue == "configured");
            previous.Should().NotBeNull();
            previous.Disposed.Should().BeTrue();
            parent.Messages.Should().ContainSingle();
            parent.Attributes.Keys.Should().Equal("parent");
            parent.Timestamp.Should().Be(parentTimestamp);
        }
        finally
        {
            ExecutionContext.Reset();
        }
    }

    [Test]
    public async Task RunScopedAsync_ReturnsOriginalResultFailureAndRestoresParent()
    {
        var created = new List<TrackingContext>();
        await using var provider = CreateProvider(created);
        using var parent = new TrackingContext { ServiceProvider = provider };
        ExecutionContext.SetCurrent(parent);
        var failure = Result.Fail(new BusinessException("Execution rejected."));
        try
        {
            var actual = await ExecutionContext.RunScopedAsync(provider, async (context, ct) =>
            {
                await Task.Yield();
                return failure;
            }).ConfigureAwait(false);

            actual.Should().Be(failure);
            actual.Error.Should().BeSameAs(failure.Error);
            ExecutionContext.Current.Should().BeSameAs(parent);
            parent.ServiceProvider.Should().BeSameAs(provider);
            created.Single().DisposeCalls.Should().Be(1);
        }
        finally
        {
            ExecutionContext.Reset();
        }
    }

    [TestCase("configure")]
    [TestCase("action")]
    [TestCase("dispose")]
    public async Task RunScopedAsync_RestoresParentOnFault(string stage)
    {
        var error = new InvalidOperationException(stage);
        var created = new List<TrackingContext>();
        await using var provider = CreateProvider(created);
        using var parent = new TrackingContext { ServiceProvider = provider };
        ExecutionContext.SetCurrent(parent);
        ScopedDependency? dependency = null;
        var actionCalled = false;
        try
        {
            Func<Task> run = () => ExecutionContext.RunScopedAsync(provider, async (context, ct) =>
            {
                actionCalled = true;
                dependency = ExecutionContext.GetRequiredService<ScopedDependency>();
                if (stage == "dispose")
                    dependency.DisposalError = error;

                await Task.Yield();
                if (stage == "action")
                    throw error;
            }, (_, _) =>
            {
                if (stage == "configure")
                    throw error;
            });

            var assertion = await run.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(false);
            assertion.Which.Should().BeSameAs(error);
            ExecutionContext.Current.Should().BeSameAs(parent);
            parent.ServiceProvider.Should().BeSameAs(provider);
            parent.DisposeCalls.Should().Be(0);
            created.Single().DisposeCalls.Should().Be(1);
            actionCalled.Should().Be(stage != "configure");
            if (dependency is not null)
                dependency.Disposed.Should().BeTrue();
        }
        finally
        {
            ExecutionContext.Reset();
        }
    }

    [Test]
    public async Task RunScopedAsync_RestoresParentAfterFactoryFailure()
    {
        var services = new ServiceCollection();
        var error = new InvalidOperationException("Factory failed.");
        services.AddExecutionContext((Func<IServiceProvider, ExecutionContext>)(_ => throw error));
        await using var provider = services.BuildServiceProvider();
        using var parent = new TrackingContext { ServiceProvider = provider };
        ExecutionContext.SetCurrent(parent);
        try
        {
            Func<Task> run = () => ExecutionContext.RunScopedAsync(provider, (_, _) => Task.CompletedTask);
            var assertion = await run.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(false);
            assertion.Which.Should().BeSameAs(error);
            ExecutionContext.Current.Should().BeSameAs(parent);
            parent.DisposeCalls.Should().Be(0);
            parent.ServiceProvider.Should().BeSameAs(provider);
        }
        finally
        {
            ExecutionContext.Reset();
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task RunScopedAsync_CancellationPropagatesWithoutDisposingParent(bool beforeStart)
    {
        var created = new List<TrackingContext>();
        await using var provider = CreateProvider(created);
        using var parent = new TrackingContext { ServiceProvider = provider };
        ExecutionContext.SetCurrent(parent);
        using var cancellation = new CancellationTokenSource();
        if (beforeStart)
            cancellation.Cancel();

        try
        {
            Func<Task> run = () => ExecutionContext.RunScopedAsync(provider, async (context, ct) =>
            {
                ct.Should().Be(cancellation.Token);
                await Task.Yield();
                cancellation.Cancel();
                ct.ThrowIfCancellationRequested();
            }, cancellationToken: cancellation.Token);

            var assertion = await run.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
            assertion.Which.CancellationToken.Should().Be(cancellation.Token);
            ExecutionContext.Current.Should().BeSameAs(parent);
            parent.ServiceProvider.Should().BeSameAs(provider);
            parent.DisposeCalls.Should().Be(0);
            created.Should().HaveCount(beforeStart ? 0 : 1);
            created.Should().OnlyContain(x => x.DisposeCalls == 1);
        }
        finally
        {
            ExecutionContext.Reset();
        }
    }

    [Test]
    public async Task RunScopedAsync_NestedScopesRestoreImmediateParentAndNoParentRemainsAbsent()
    {
        ExecutionContext.HasCurrent.Should().BeFalse();
        var created = new List<TrackingContext>();
        await using var provider = CreateProvider(created);
        await ExecutionContext.RunScopedAsync(provider, async (outer, ct) =>
        {
            await ExecutionContext.RunScopedAsync(outer.ServiceProvider.ThrowIfNull(), async (inner, innerToken) =>
            {
                inner.Should().NotBeSameAs(outer);
                inner.ServiceProvider.Should().NotBeSameAs(outer.ServiceProvider);
                await Task.Yield();
                ExecutionContext.Current.Should().BeSameAs(inner);
            }, (parent, _) => parent.Should().BeSameAs(outer), ct).ConfigureAwait(false);

            ExecutionContext.Current.Should().BeSameAs(outer);
            ((TrackingContext)outer).DisposeCalls.Should().Be(0);
        }).ConfigureAwait(false);

        ExecutionContext.HasCurrent.Should().BeFalse();
        created.Should().HaveCount(2);
        created.Should().OnlyContain(x => x.DisposeCalls == 1);
    }

    [Test]
    public async Task RunScopedAsync_ConcurrentUnitsRemainIsolatedWhileCallerKeepsItsContext()
    {
        var services = new ServiceCollection();
        services.AddExecutionContext(_ => new TrackingContext());
        await using var provider = services.BuildServiceProvider();
        using var parent = new TrackingContext { ServiceProvider = provider, TenantId = "parent" };
        ExecutionContext.SetCurrent(parent);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var count = 0;
        try
        {
            var tasks = Enumerable.Range(0, 2).Select(index => ExecutionContext.RunScopedAsync(provider, async (context, ct) =>
            {
                context.Attributes["index"] = index;
                if (Interlocked.Increment(ref count) == 2)
                    entered.SetResult();

                await release.Task.ConfigureAwait(false);
                ExecutionContext.Current.Should().BeSameAs(context);
                context.TenantId.Should().Be($"tenant-{index}");
                context.Attributes["index"].Should().Be(index);
                return context;
            }, (_, inner) => inner.TenantId = $"tenant-{index}")).ToArray();

            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            ExecutionContext.Current.Should().BeSameAs(parent);
            parent.ServiceProvider.Should().BeSameAs(provider);
            release.SetResult();
            var contexts = await Task.WhenAll(tasks).ConfigureAwait(false);
            contexts[0].Should().NotBeSameAs(contexts[1]);
            contexts.Should().OnlyContain(x => ((TrackingContext)x).DisposeCalls == 1);
            ExecutionContext.Current.Should().BeSameAs(parent);
            parent.TenantId.Should().Be("parent");
            parent.Attributes.Should().BeEmpty();
        }
        finally
        {
            release.TrySetResult();
            ExecutionContext.Reset();
        }
    }

    [Test]
    public async Task RunScopedAsync_MissingRegistrationFailsWithoutCreatingFallbackContext()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        Func<Task> run = () => ExecutionContext.RunScopedAsync(provider, (_, _) => Task.CompletedTask);
        await run.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(false);
        ExecutionContext.HasCurrent.Should().BeFalse();
    }

    [Test]
    public async Task RunScopedAsync_UsesRegisteredConfigurationBeforeUnitConfiguration()
    {
        var services = new ServiceCollection();
        services.AddExecutionContext((_, context) => context.TenantId = "registered");
        services.AddScoped<ScopedDependency>();
        await using var provider = services.BuildServiceProvider();
        await ExecutionContext.RunScopedAsync(provider, (context, ct) =>
        {
            ExecutionContext.Current.Should().BeSameAs(context);
            ExecutionContext.GetRequiredService<ScopedDependency>().TenantId.Should().Be("unit");
            return Task.CompletedTask;
        }, (parent, child) =>
        {
            parent.Should().BeNull();
            child.TenantId.Should().Be("registered");
            child.TenantId = "unit";
        }).ConfigureAwait(false);

        ExecutionContext.HasCurrent.Should().BeFalse();
    }

    [Test]
    public async Task RunScopedAsync_CancellationDuringConfigurationPreventsBusinessResolution()
    {
        var created = new List<TrackingContext>();
        await using var provider = CreateProvider(created);
        using var cancellation = new CancellationTokenSource();
        var invoked = false;
        Func<Task> run = () => ExecutionContext.RunScopedAsync(provider, (_, _) =>
        {
            invoked = true;
            return Task.CompletedTask;
        }, (_, _) => cancellation.Cancel(), cancellation.Token);

        await run.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
        invoked.Should().BeFalse();
        created.Single().DisposeCalls.Should().Be(1);
        ExecutionContext.HasCurrent.Should().BeFalse();
    }

    [Test]
    public async Task RunScopedAsync_ProviderScopeCreationFailureRestoresParent()
    {
        var provider = new ServiceCollection().BuildServiceProvider();
        await provider.DisposeAsync().ConfigureAwait(false);
        using var parent = new TrackingContext();
        ExecutionContext.SetCurrent(parent);
        try
        {
            Func<Task> run = () => ExecutionContext.RunScopedAsync(provider, (_, _) => Task.CompletedTask);
            await run.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
            ExecutionContext.Current.Should().BeSameAs(parent);
            parent.DisposeCalls.Should().Be(0);
        }
        finally
        {
            ExecutionContext.Reset();
        }
    }

    [Test]
    public async Task RunScopedAsync_ContextDisposalFaultStillRestoresParent()
    {
        var error = new InvalidOperationException("Context disposal failed.");
        var services = new ServiceCollection();
        services.AddExecutionContext(_ => new FaultingContext(error));
        await using var provider = services.BuildServiceProvider();
        using var parent = new TrackingContext { ServiceProvider = provider };
        ExecutionContext.SetCurrent(parent);
        try
        {
            Func<Task> run = () => ExecutionContext.RunScopedAsync(provider, (_, _) => Task.CompletedTask);
            var assertion = await run.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(false);
            assertion.Which.Should().BeSameAs(error);
            ExecutionContext.Current.Should().BeSameAs(parent);
            parent.ServiceProvider.Should().BeSameAs(provider);
            parent.DisposeCalls.Should().Be(0);
        }
        finally
        {
            ExecutionContext.Reset();
        }
    }

    [Test]
    public async Task RunScopedAsync_RejectsMissingArgumentsWithoutChangingAmbientState()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        Func<Task> noProvider = () => ExecutionContext.RunScopedAsync(null!, (_, _) => Task.CompletedTask);
        Func<Task> noAction = () => ExecutionContext.RunScopedAsync(provider, (Func<ExecutionContext, CancellationToken, Task>)null!);
        Func<Task> noGenericAction = () => ExecutionContext.RunScopedAsync<int>(provider, null!);

        await noProvider.Should().ThrowAsync<ArgumentNullException>().ConfigureAwait(false);
        await noAction.Should().ThrowAsync<ArgumentNullException>().ConfigureAwait(false);
        await noGenericAction.Should().ThrowAsync<ArgumentNullException>().ConfigureAwait(false);
        ExecutionContext.HasCurrent.Should().BeFalse();
    }

    [Test]
    public async Task AddExecutionContext_OrdinaryResolutionStillOwnsAndReplacesAmbientContext()
    {
        var created = new List<TrackingContext>();
        await using var provider = CreateProvider(created);
        using var scope = provider.CreateScope();
        using var parent = new TrackingContext();
        ExecutionContext.SetCurrent(parent);
        try
        {
            var child = scope.ServiceProvider.GetRequiredService<ExecutionContext>();
            ExecutionContext.Current.Should().BeSameAs(child);
            parent.DisposeCalls.Should().Be(1);
        }
        finally
        {
            ExecutionContext.Reset();
        }
    }

    private static ServiceProvider CreateProvider(List<TrackingContext> created)
    {
        var services = new ServiceCollection();
        services.AddExecutionContext(_ =>
        {
            var context = new TrackingContext();
            created.Add(context);
            return context;
        });
        services.AddScoped<ScopedDependency>();
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private sealed class TrackingContext : ExecutionContext
    {
        public int DisposeCalls { get; private set; }
        public string? CustomValue { get; set; }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                DisposeCalls++;

            base.Dispose(disposing);
        }
    }

    private sealed class FaultingContext(Exception error) : ExecutionContext
    {
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                throw error;
        }
    }

    private sealed class ScopedDependency(ExecutionContext context) : IAsyncDisposable
    {
        public string? TenantId { get; } = context.TenantId;
        public bool Disposed { get; private set; }
        public Exception? DisposalError { get; set; }

        public async ValueTask DisposeAsync()
        {
            ExecutionContext.Current.Should().BeSameAs(context);
            await Task.Yield();
            ExecutionContext.Current.Should().BeSameAs(context);
            Disposed = true;
            if (DisposalError is not null)
                throw DisposalError;
        }
    }
}
