#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace UnitTestEx.Expectations;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static partial class UnitTestExExpectations
{
    /// <summary>
    /// Expects the <see cref="IReadOnlyETag"/> to be implemented and have non-default <see cref="IReadOnlyETag.ETag"/>.
    /// </summary>
    /// <typeparam name="TSelf">The expectations <see cref="Type"/>.</typeparam>
    /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
    /// <param name="tester">The <see cref="IValueExpectations{TValue, TSelf}"/> tester.</param>
    /// <param name="previousETag">The optional previous ETag to compare <b>not</b> equal to; i.e. it must be different.</param>
    /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
    public static TSelf ExpectETag<TValue, TSelf>(this IValueExpectations<TValue, TSelf> tester, string? previousETag = null) where TSelf : IValueExpectations<TValue, TSelf>
    {
        VerifyImplements<TValue, IReadOnlyETag>();
        IgnoreETag(tester);
        var pn = $"{nameof(IReadOnlyETag)}.{nameof(IReadOnlyETag.ETag)}";

        Task<bool> extension(AssertArgs args)
        {
            var etag = args.Value as IETag;
            if (etag is null || etag.ETag is null)
                args.Tester.Implementor.AssertFail($"Expected {pn} to have a non-null value.");

            if (previousETag is not null && previousETag == etag!.ETag)
                args.Tester.Implementor.AssertFail($"Expected {pn} value of '{previousETag}' to be different to actual.");

            return Task.FromResult(true);
        }

        return SetValueExpectationExtension(tester, extension);
    }

    /// <summary>
    /// Ignores the <see cref="IReadOnlyETag.ETag"/> JSON path.
    /// </summary>
    /// <typeparam name="TSelf">The expectations <see cref="Type"/>.</typeparam>
    /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
    /// <param name="tester">The <see cref="IValueExpectations{TValue, TSelf}"/> tester.</param>
    /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
    public static TSelf IgnoreETag<TValue, TSelf>(this IValueExpectations<TValue, TSelf> tester) where TSelf : IValueExpectations<TValue, TSelf> => IgnorePaths(tester, nameof(IReadOnlyETag.ETag));
}