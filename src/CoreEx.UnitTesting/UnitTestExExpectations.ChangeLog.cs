#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace UnitTestEx.Expectations;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static partial class UnitTestExExpectations
{
    /// <summary>
    /// Expects the <see cref="IReadOnlyChangeLog"/> to be implemented and have non-default <see cref="IReadOnlyChangeLog.ChangeLog"/> <see cref="IChangeLogEx.CreatedBy"/> and <see cref="IChangeLogEx.CreatedOn"/> values.
    /// </summary>
    /// <typeparam name="TSelf">The expectations <see cref="Type"/>.</typeparam>
    /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
    /// <param name="tester">The <see cref="IValueExpectations{TValue, TSelf}"/> tester.</param>
    /// <param name="createdBy">The specific <see cref="IChangeLogEx.CreatedBy"/> value where specified (can include wildcards); otherwise, indicates to check for user running the test (see <see cref="Abstractions.TesterBase.UserName"/>).</param>
    /// <param name="createdOn">The <see cref="DateTimeOffset"/> in which the <see cref="IChangeLogEx.CreatedOn"/> should be greater than or equal to; where <c>null</c> it will default to <see cref="DateTimeOffset.UtcNow"/>.</param>
    /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
    public static TSelf ExpectChangeLogCreated<TValue, TSelf>(this IValueExpectations<TValue, TSelf> tester, string? createdBy = null, DateTimeOffset? createdOn = null) where TSelf : IValueExpectations<TValue, TSelf>
    {
        string pn;
        if (typeof(TValue).GetInterface(typeof(IReadOnlyChangeLogEx).FullName ?? typeof(IReadOnlyChangeLogEx).Name) is null)
        {
            VerifyImplements<TValue, IReadOnlyChangeLog>();
            pn = $"{nameof(IReadOnlyChangeLog)}.{nameof(IReadOnlyChangeLog.ChangeLog)}";
        }
        else
            pn = $"{nameof(IReadOnlyChangeLogEx)}";

        IgnoreChangeLog(tester);

        createdBy ??= tester.ExpectationsArranger.Owner.UserName;
        createdOn ??= DateTimeOffset.UtcNow.Subtract(new TimeSpan(0, 0, 1));

        Task<bool> extension(AssertArgs args)
        {
            var cl = args.Value is IReadOnlyChangeLog icl ? icl.ChangeLog : args.Value as IReadOnlyChangeLogEx;
            if (cl is null)
            {
                args.Tester.Implementor.AssertFail($"Expected {pn} to have a non-null value.");
                return Task.FromResult(true);
            }

            if (cl.CreatedOn == null)
                args.Tester.Implementor.AssertFail($"Expected {pn}.{nameof(IReadOnlyChangeLogEx.CreatedBy)} value of '{createdBy}'; actual was null.");
            else
            {
                if (!SubscribedBase.IsMatch(createdBy, cl.CreatedBy))
                    args.Tester.Implementor.AssertFail($"Expected {pn}.{nameof(ChangeLog.CreatedBy)} value of '{createdBy}'; actual '{cl.CreatedBy}'.");
            }

            if (!cl.CreatedOn.HasValue)
                args.Tester.Implementor.AssertFail($"Expected {pn}.{nameof(IReadOnlyChangeLogEx.CreatedOn)} to have a non-null value.");
            else if (cl.CreatedOn.Value < createdOn.Value)
                args.Tester.Implementor.AssertFail($"Expected {pn}.{nameof(IReadOnlyChangeLogEx.CreatedOn)} value of '{createdOn.Value}'; actual '{cl.CreatedOn.Value}' must be greater than or equal to expected.");

            return Task.FromResult(true);
        }

        return SetValueExpectationExtension(tester, extension);
    }

    /// <summary>
    /// Expects the <see cref="IReadOnlyChangeLog"/> to be implemented and have non-default <see cref="IReadOnlyChangeLog.ChangeLog"/> <see cref="IChangeLogEx.UpdatedBy"/> and <see cref="IChangeLogEx.UpdatedOn"/> values.
    /// </summary>
    /// <typeparam name="TSelf">The expectations <see cref="Type"/>.</typeparam>
    /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
    /// <param name="tester">The <see cref="IValueExpectations{TValue, TSelf}"/> tester.</param>
    /// <param name="updatedBy">The specific <see cref="IChangeLogEx.UpdatedBy"/> value where specified (can include wildcards); otherwise, indicates to check for user running the test (see <see cref="Abstractions.TesterBase.UserName"/>).</param>
    /// <param name="updatedOn">The <see cref="DateTimeOffset"/> in which the <see cref="IChangeLogEx.UpdatedOn"/> should be greater than or equal to; where <c>null</c> it will default to <see cref="DateTimeOffset.UtcNow"/>.</param>
    /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
    public static TSelf ExpectChangeLogUpdated<TValue, TSelf>(this IValueExpectations<TValue, TSelf> tester, string? updatedBy = null, DateTimeOffset? updatedOn = null) where TSelf : IValueExpectations<TValue, TSelf>
    {
        string pn;
        if (typeof(TValue).GetInterface(typeof(IReadOnlyChangeLogEx).FullName ?? typeof(IReadOnlyChangeLogEx).Name) is null)
        {
            VerifyImplements<TValue, IReadOnlyChangeLog>();
            pn = $"{nameof(IReadOnlyChangeLog)}.{nameof(IReadOnlyChangeLog.ChangeLog)}";
        }
        else
            pn = $"{nameof(IReadOnlyChangeLogEx)}";

        IgnoreChangeLog(tester);

        updatedBy ??= tester.ExpectationsArranger.Owner.UserName;
        updatedOn ??= DateTimeOffset.UtcNow.Subtract(new TimeSpan(0, 0, 1));

        Task<bool> extension(AssertArgs args)
        {
            var cl = args.Value is IReadOnlyChangeLog icl ? icl.ChangeLog : args.Value as IReadOnlyChangeLogEx;
            if (cl is null)
            {
                args.Tester.Implementor.AssertFail($"Expected {pn} to have a non-null value.");
                return Task.FromResult(true);
            }

            if (cl.UpdatedOn == null)
                args.Tester.Implementor.AssertFail($"Expected {pn}.{nameof(IReadOnlyChangeLogEx.UpdatedBy)} value of '{updatedBy}'; actual was null.");
            else
            {
                if (!SubscribedBase.IsMatch(updatedBy, cl.UpdatedBy))
                    args.Tester.Implementor.AssertFail($"Expected {pn}.{nameof(ChangeLog.UpdatedBy)} value of '{updatedBy}'; actual '{cl.UpdatedBy}'.");
            }

            if (!cl.UpdatedOn.HasValue)
                args.Tester.Implementor.AssertFail($"Expected {pn}.{nameof(IReadOnlyChangeLogEx.UpdatedOn)} to have a non-null value.");
            else if (cl.UpdatedOn.Value < updatedOn.Value)
                args.Tester.Implementor.AssertFail($"Expected {pn}.{nameof(IReadOnlyChangeLogEx.UpdatedOn)} value of '{updatedOn.Value}'; actual '{cl.UpdatedOn.Value}' must be greater than or equal to expected.");

            return Task.FromResult(true);
        }

        return SetValueExpectationExtension(tester, extension);
    }

    /// <summary>
    /// Ignores the <see cref="IChangeLog.ChangeLog"/> JSON path (and <see cref="IChangeLogEx.CreatedBy"/>, <see cref="IChangeLogEx.CreatedOn"/>, <see cref="IChangeLogEx.UpdatedBy"/> and <see cref="IChangeLogEx.UpdatedOn"/>.
    /// </summary>
    /// <typeparam name="TSelf">The expectations <see cref="Type"/>.</typeparam>
    /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
    /// <param name="tester">The <see cref="IValueExpectations{TValue, TSelf}"/> tester.</param>
    /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
    public static TSelf IgnoreChangeLog<TValue, TSelf>(this IValueExpectations<TValue, TSelf> tester) where TSelf : IValueExpectations<TValue, TSelf>
        => IgnorePaths(tester, nameof(IChangeLog.ChangeLog), nameof(IChangeLogEx.CreatedBy), nameof(IChangeLogEx.CreatedOn), nameof(IChangeLogEx.UpdatedBy), nameof(IChangeLogEx.UpdatedOn));
}