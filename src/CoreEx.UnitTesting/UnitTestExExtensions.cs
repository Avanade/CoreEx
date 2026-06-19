#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace UnitTestEx;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides extensions for <see cref="TesterBase"/> to support common testing scenarios.
/// </summary>
public static partial class UnitTestExExtensions
{
    private static readonly ConcurrentDictionary<string, byte> _connectionStrings = [];

    /// <summary>
    /// Enables an <see cref="ExecutionContext"/> instance to be tested managed within a <see cref="TesterBase.Services"/> <see cref="ServiceProviderServiceExtensions.CreateScope(IServiceProvider)"/>.
    /// </summary>
    /// <typeparam name="TSelf">The <see cref="TesterBase{TSelf}"/> <see cref="Type"/>.</typeparam>
    /// <param name="tester">The <see cref="TesterBase{TSelf}"/>.</param>
    /// <param name="scopedTester">The <see cref="ScopedTypeTester{TService}"/> testing action.</param>
    /// <param name="configure">The optional pre-test <see cref="ExecutionContext"/> configuration action.</param>
    /// <returns>The <see cref="TesterBase{TSelf}"/> to support fluent-style method-chaining.</returns>
    /// <remarks>This is a convenience method for common testing with an <see cref="ExecutionContext"/> instance within a scoped service lifetime.</remarks>
    public static TSelf Scoped<TSelf>(this TesterBase<TSelf> tester, Action<ScopedTypeTester<ExecutionContext>> scopedTester, Action<ExecutionContext>? configure = null) where TSelf : TesterBase<TSelf>
    {
        tester.ThrowIfNull();
        scopedTester.ThrowIfNull();
        return tester.ScopedType<ExecutionContext>(test =>
        {
            configure?.Invoke(test.Service);
            scopedTester(test);
        });
    }

    /// <summary>
    /// Enables an <see cref="ExecutionContext"/> instance to be tested managed within a <see cref="TesterBase.Services"/> <see cref="ServiceProviderServiceExtensions.CreateScope(IServiceProvider)"/>.
    /// </summary>
    /// <typeparam name="TSelf">The <see cref="TesterBase{TSelf}"/> <see cref="Type"/>.</typeparam>
    /// <param name="tester">The <see cref="TesterBase{TSelf}"/>.</param>
    /// <param name="scopedTester">The <see cref="ScopedTypeTester{TService}"/> testing action.</param>
    /// <param name="configure">The optional pre-test <see cref="ExecutionContext"/> configuration action.</param>
    /// <returns>The <see cref="TesterBase{TSelf}"/> to support fluent-style method-chaining.</returns>
    /// <remarks>This is a convenience method for common testing with an <see cref="ExecutionContext"/> instance within a scoped service lifetime.</remarks>
    public static TSelf Scoped<TSelf>(this TesterBase<TSelf> tester, Func<ScopedTypeTester<ExecutionContext>, Task> scopedTester, Action<ExecutionContext>? configure = null) where TSelf : TesterBase<TSelf>
    {
        tester.ThrowIfNull();
        scopedTester.ThrowIfNull();
        return tester.ScopedType<ExecutionContext>(async test =>
        {
            configure?.Invoke(test.Service);
            await scopedTester(test).ConfigureAwait(false);
        });
    }

    /// <summary>
    /// Enables an <see cref="ExecutionContext"/> instance to be tested managed within a <see cref="TesterBase.Services"/> <see cref="ServiceProviderServiceExtensions.CreateScope(IServiceProvider)"/>.
    /// </summary>
    /// <typeparam name="TSelf">The <see cref="TesterBase{TSelf}"/> <see cref="Type"/>.</typeparam>
    /// <param name="tester">The <see cref="TesterBase{TSelf}"/>.</param>
    /// <param name="tenantId">The tenant identifier to set on the <see cref="ExecutionContext"/>.</param>
    /// <param name="scopedTester">The <see cref="ScopedTypeTester{TService}"/> testing action.</param>
    /// <param name="configure">The optional pre-test <see cref="ExecutionContext"/> configuration action.</param>
    /// <returns>The <see cref="TesterBase{TSelf}"/> to support fluent-style method-chaining.</returns>
    /// <remarks>This is a convenience method for common testing with an <see cref="ExecutionContext"/> instance within a scoped service lifetime.</remarks>
    public static TSelf Scoped<TSelf>(this TesterBase<TSelf> tester, string? tenantId, Action<ScopedTypeTester<ExecutionContext>> scopedTester, Action<ExecutionContext>? configure = null) where TSelf : TesterBase<TSelf>
        => Scoped(tester, scopedTester, ec =>
        {
            ec.TenantId = tenantId;
            configure?.Invoke(ec);
        });

    /// <summary>
    /// Enables an <see cref="ExecutionContext"/> instance to be tested managed within a <see cref="TesterBase.Services"/> <see cref="ServiceProviderServiceExtensions.CreateScope(IServiceProvider)"/>.
    /// </summary>
    /// <typeparam name="TSelf">The <see cref="TesterBase{TSelf}"/> <see cref="Type"/>.</typeparam>
    /// <param name="tester">The <see cref="TesterBase{TSelf}"/>.</param>
    /// <param name="tenantId">The tenant identifier to set on the <see cref="ExecutionContext"/>.</param>
    /// <param name="scopedTester">The <see cref="ScopedTypeTester{TService}"/> testing action.</param>
    /// <param name="configure">The optional pre-test <see cref="ExecutionContext"/> configuration action.</param>
    /// <returns>The <see cref="TesterBase{TSelf}"/> to support fluent-style method-chaining.</returns>
    /// <remarks>This is a convenience method for common testing with an <see cref="ExecutionContext"/> instance within a scoped service lifetime.</remarks>
    public static TSelf Scoped<TSelf>(this TesterBase<TSelf> tester, string? tenantId, Func<ScopedTypeTester<ExecutionContext>, Task> scopedTester, Action<ExecutionContext>? configure = null) where TSelf : TesterBase<TSelf>
    {
        tester.ThrowIfNull();
        scopedTester.ThrowIfNull();
        return tester.ScopedType<ExecutionContext>(async test =>
        {
            test.Service.TenantId = tenantId;
            configure?.Invoke(test.Service);
            await scopedTester(test).ConfigureAwait(false);
        });
    }
}