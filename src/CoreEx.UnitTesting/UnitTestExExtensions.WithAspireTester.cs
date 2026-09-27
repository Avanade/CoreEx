#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace UnitTestEx;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides a shared <see cref="AspireTester{TAppHost}"/> to enable usage of the same underlying <see cref="AspireTesterBase{TAppHost, TSelf}"/> instance across multiple tests.
/// </summary>
/// <typeparam name="TAppHost">The AppHost <see cref="Type"/> (a <c>Projects.*</c> type generated for the AppHost's <c>ProjectReference</c>).</typeparam>
/// <param name="createFactory">The optional function to create the <see cref="TestFrameworkImplementor"/> instance.</param>
public abstract class WithAspireTester<TAppHost>(Func<TestFrameworkImplementor>? createFactory = null) : IAsyncDisposable where TAppHost : class
{
    private AspireTester<TAppHost>? _aspireTester = AspireTester.Create<TAppHost>(createFactory);

    /// <summary>
    /// Gets the underlying <see cref="AspireTester{TAppHost}"/> for testing.
    /// </summary>
    public AspireTester<TAppHost> Test => _aspireTester ?? throw new ObjectDisposedException(nameof(Test));

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
