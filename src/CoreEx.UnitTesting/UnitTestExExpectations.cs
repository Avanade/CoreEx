#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace UnitTestEx.Expectations;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides <see cref="CoreEx"/>-specific extension methods to <see cref="UnitTestEx"/>.
/// </summary>
public static partial class UnitTestExExpectations
{
    /// <summary>
    /// Sets the configured expectation logic.
    /// </summary>
    private static TSelf SetValueExpectationExtension<TValue, TSelf>(this IValueExpectations<TValue, TSelf> tester, Func<AssertArgs, Task<bool>> extension) where TSelf : IValueExpectations<TValue, TSelf>
    {
        tester.ExpectationsArranger.GetOrAdd(() => new ValueExpectations<TSelf>(tester.ExpectationsArranger.Owner, (TSelf)tester)).AddExtension(extension);
        return (TSelf)tester;
    }

    /// <summary>
    /// Verifies that the <typeparamref name="TValue"/> implements <typeparamref name="TInterface"/>.
    /// </summary>
    private static void VerifyImplements<TValue, TInterface>()
    {
        if (typeof(TValue).GetInterface(typeof(TInterface).FullName ?? typeof(TInterface).Name) == null)
            throw new InvalidOperationException($"{typeof(TValue).Name} must implement the interface {typeof(TInterface).Name}.");
    }

    /// <summary>
    /// Adds <paramref name="paths"/> to ignore from the JSON value comparison.
    /// </summary>
    /// <typeparam name="TSelf">The expectations <see cref="Type"/>.</typeparam>
    /// <param name="tester">The <see cref="IExpectations{TSelf}"/> tester.</param>
    /// <param name="paths">The JSON paths to ignore.</param>
    /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
    public static TSelf IgnorePaths<TSelf>(IExpectations<TSelf> tester, params string[] paths) where TSelf : IExpectations<TSelf>
    {
        tester.ExpectationsArranger.PathsToIgnore.AddRange(paths);
        return (TSelf)tester;
    }
}