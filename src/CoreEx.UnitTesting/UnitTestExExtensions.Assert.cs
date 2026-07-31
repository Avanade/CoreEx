#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace UnitTestEx;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static partial class UnitTestExExtensions
{
    /// <summary>
    /// Asserts that the response is a <see cref="ProblemDetails"/> and allows for further assertions to be performed on the <see cref="ProblemDetails"/> instance.
    /// </summary>
    /// <typeparam name="TSelf">The <see cref="HttpResponseMessageAssertorBase{TSelf}"/> <see cref="Type"/>.</typeparam>
    /// <param name="assertor">The <see cref="HttpResponseMessageAssertorBase{TSelf}"/>.</param>
    /// <param name="assertAction">An optional action to perform additional assertions on the <see cref="ProblemDetails"/> instance.</param>
    /// <returns>The <see cref="HttpResponseMessageAssertorBase{TSelf}"/> instance to support fluent-style method-chaining.</returns>
    public static TSelf AssertProblemDetails<TSelf>(this TSelf assertor, Action<ProblemDetails>? assertAction = null) where TSelf : HttpResponseMessageAssertorBase<TSelf>
    {
        var problemDetails = assertor.GetValue<ProblemDetails>(null);
        if (problemDetails is null)
            assertor.Owner.Implementor.AssertFail("Expected ProblemDetails response to be present but nothing was returned.");

        assertAction?.Invoke(problemDetails!);
        return assertor;
    }
}
