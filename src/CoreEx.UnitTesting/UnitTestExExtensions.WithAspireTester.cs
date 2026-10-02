#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace UnitTestEx;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides a shared <see cref="AspireTester{TAppHost}"/> to enable usage of the same underlying <see cref="AspireTesterBase{TAppHost, TSelf}"/> instance across multiple tests.
/// </summary>
/// <typeparam name="TAppHost">The AppHost <see cref="Type"/> (a <c>Projects.*</c> type generated for the AppHost's <c>ProjectReference</c>).</typeparam>
public abstract class WithAspireTester<TAppHost> : IAsyncDisposable where TAppHost : class
{
    private AspireTester<TAppHost>? _aspireTester;

    /// <summary>
    /// Initializes a new instance of the <see cref="WithAspireTester{TAppHost}"/> class.
    /// </summary>
    /// <param name="createFactory">The optional function to create the <see cref="TestFrameworkImplementor"/> instance.</param>
    public WithAspireTester(Func<TestFrameworkImplementor>? createFactory = null)
        => _aspireTester ??= AspireTester.Create<TAppHost>(createFactory).BeforeStart(app => OnBeforeStartAsync(app)).AfterStart(app => OnAfterStartAsync(app));

    /// <summary>
    /// Gets the underlying <see cref="AspireTester{TAppHost}"/> for testing.
    /// </summary>
    public AspireTester<TAppHost> Test => _aspireTester ?? throw new ObjectDisposedException(nameof(Test));

    /// <summary>
    /// Provides an opportunity to perform any pre-start logic before the <see cref="DistributedApplication"/> is started.
    /// </summary>
    /// <param name="app">The <see cref="DistributedApplication"/> instance that is being started.</param>
    protected virtual Task OnBeforeStartAsync(DistributedApplication app) => Task.CompletedTask;

    /// <summary>
    /// Provides an opportunity to perform any post-start logic after the <see cref="DistributedApplication"/> has been started.
    /// </summary>
    /// <param name="app">The <see cref="DistributedApplication"/> instance that has been started.</param>
    protected virtual Task OnAfterStartAsync(DistributedApplication app) => Task.CompletedTask;

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await DisposeCoreAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Performs the core asynchronous disposal logic; override to extend disposal behavior in derived classes.
    /// </summary>
    protected virtual async ValueTask DisposeCoreAsync()
    {
        if (_aspireTester is null)
            return;

        await _aspireTester.DisposeAsync().ConfigureAwait(false);
        _aspireTester = null;
    }
}
