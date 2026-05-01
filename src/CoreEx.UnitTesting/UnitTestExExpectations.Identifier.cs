#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace UnitTestEx.Expectations;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static partial class UnitTestExExpectations
{
    /// <summary>
    /// Expects the <see cref="IReadOnlyIdentifier"/> to be implemented and have non-default <see cref="IIdentifierCore.Id"/>.
    /// </summary>
    /// <typeparam name="TSelf">The expectations <see cref="Type"/>.</typeparam>
    /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
    /// <param name="tester">The <see cref="IValueExpectations{TValue, TSelf}"/> tester.</param>
    /// <param name="identifier">The optional expected identifier to compare to.</param>
    /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
    public static TSelf ExpectIdentifier<TValue, TSelf>(this IValueExpectations<TValue, TSelf> tester, object? identifier = null) where TSelf : IValueExpectations<TValue, TSelf>
    {
        VerifyImplements<TValue, IIdentifierCore>();
        IgnoreIdentifier(tester);
        var pn = $"{nameof(IReadOnlyIdentifier)}.{nameof(IIdentifier.Id)}";

        Task<bool> extension(AssertArgs args)
        {
            var id = args.Value as IIdentifier;
            if (id is null || id.Id is null)
                args.Tester.Implementor.AssertFail($"Expected {pn} to have a non-null value.");

            if (identifier is null)
            {
                if (System.Collections.Comparer.Default.Compare(id!.Id, id!.GetType().IsClass ? null! : Activator.CreateInstance(id!.GetType())) == 0)
                    args.Tester.Implementor.AssertFail($"Expected {pn} to have a non-default value.");
            }
            else
                args.Tester.Implementor.AssertAreEqual(identifier, id!.Id, $"Expected {pn} value of '{identifier}'; actual '{id.Id}'.");

            return Task.FromResult(true);
        }

        return SetValueExpectationExtension(tester, extension);
    }

    /// <summary>
    /// Ignores the <see cref="IReadOnlyIdentifier"/> <see cref="IIdentifierCore.Id"/> JSON path.
    /// </summary>
    /// <typeparam name="TSelf">The expectations <see cref="Type"/>.</typeparam>
    /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
    /// <param name="tester">The <see cref="IValueExpectations{TValue, TSelf}"/> tester.</param>
    /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
    public static TSelf IgnoreIdentifier<TValue, TSelf>(this IValueExpectations<TValue, TSelf> tester) where TSelf : IValueExpectations<TValue, TSelf> => IgnorePaths(tester, nameof(IIdentifierCore.Id));
}