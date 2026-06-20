#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace UnitTestEx;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static partial class UnitTestExExtensions
{
    /// <summary>
    /// Asserts that the response is a <see cref="ProblemDetails"/> and that the <see cref="ProblemDetails.Title"/> matches the expected <paramref name="title"/>.
    /// </summary>
    /// <typeparam name="TSelf">The <see cref="HttpResponseMessageAssertorBase{TSelf}"/> <see cref="Type"/>.</typeparam>
    /// <param name="assertor">The <see cref="HttpResponseMessageAssertorBase{TSelf}"/>.</param>
    /// <param name="title">The expected <see cref="ProblemDetails.Title"/>.</param>
    /// <returns>The <see cref="HttpResponseMessageAssertorBase{TSelf}"/> instance to support fluent-style method-chaining.</returns>
    public static TSelf AssertProblemDetailsTitle<TSelf>(this TSelf assertor, string title) where TSelf : HttpResponseMessageAssertorBase<TSelf>
    {
        var problemDetails = assertor.GetValue<ProblemDetails>(null);
        if (problemDetails is null)
            assertor.Owner.Implementor.AssertFail("Expected ProblemDetails response to be present but nothing was returned.");

        assertor.Owner.Implementor.AssertAreEqual(title, problemDetails!.Title, "ProblemDetails Title does not match expected value.");
        return assertor;
    }
}