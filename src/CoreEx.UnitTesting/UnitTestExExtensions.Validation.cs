#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace UnitTestEx;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static partial class UnitTestExExtensions
{
    /// <summary>
    /// Executes the validation and asserts that it is successful (i.e. no errors).
    /// </summary>
    /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
    /// <param name="validator">The <see cref="IValidator{T}"/>.</param>
    /// <param name="value">The value to validate.</param>
    /// <returns>The <see cref="IValidationResult{T}"/>.</returns>
    /// <remarks>This is using <see cref="AwesomeAssertions"/> to assert that the validation is successful (i.e. <see cref="IValidationResult.HasErrors"/> is <see langword="false"/>).</remarks>
    public static void AssertSuccess<TValue>(this IValidator<TValue> validator, TValue value)
        => AssertSuccessAsync(validator, value).GetAwaiter().GetResult();

    /// <summary>
    /// Executes the validation and asserts that it is successful (i.e. no errors).
    /// </summary>
    /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
    /// <param name="validator">The <see cref="IValidator{T}"/>.</param>
    /// <param name="value">The value to validate.</param>
    /// <returns>The <see cref="IValidationResult{T}"/>.</returns>
    /// <remarks>This is using <see cref="AwesomeAssertions"/> to assert that the validation is successful (i.e. <see cref="IValidationResult.HasErrors"/> is <see langword="false"/>).</remarks>
    public static async Task<IValidationResult<TValue>> AssertSuccessAsync<TValue>(this IValidator<TValue> validator, TValue value)
    {
        var vr = await validator.ValidateAsync(value).ConfigureAwait(false);
        vr.HasErrors.Should().BeFalse();
        return vr;
    }

    /// <summary>
    /// Executes the validation and asserts that it has errors and that the expected errors are present.
    /// </summary>
    /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
    /// <param name="validator">The <see cref="IValidator{T}"/>.</param>
    /// <param name="value">The value to validate.</param>
    /// <param name="expectedErrors">The expected errors.</param>
    /// <returns>The <see cref="IValidationResult{T}"/>.</returns>
    /// <remarks>This is using <see cref="AwesomeAssertions"/> to assert that the validation was unsuccessful (i.e. <see cref="IValidationResult.HasErrors"/> is <see langword="true"/>), then comparing the
    /// <paramref name="expectedErrors"/> with the <see cref="IValidationResult.Messages"/> using <see cref="Assertor.TryAreErrorsMatched(IEnumerable{ApiError}?, IEnumerable{ApiError}?, out string?)"/>.</remarks>
    public static IValidationResult<TValue> AssertErrors<TValue>(this IValidator<TValue> validator, TValue value, params IEnumerable<ApiError> expectedErrors)
        => AssertErrorsAsync(validator, value, expectedErrors).GetAwaiter().GetResult();

    /// <summary>
    /// Executes the validation and asserts that it has errors and that the expected errors are present.
    /// </summary>
    /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
    /// <param name="validator">The <see cref="IValidator{T}"/>.</param>
    /// <param name="value">The value to validate.</param>
    /// <param name="expectedErrors">The expected errors.</param>
    /// <returns>The <see cref="IValidationResult{T}"/>.</returns>
    /// <remarks>This is using <see cref="AwesomeAssertions"/> to assert that the validation was unsuccessful (i.e. <see cref="IValidationResult.HasErrors"/> is <see langword="true"/>), then comparing the
    /// <paramref name="expectedErrors"/> with the <see cref="IValidationResult.Messages"/> using <see cref="Assertor.TryAreErrorsMatched(IEnumerable{ApiError}?, IEnumerable{ApiError}?, out string?)"/>.</remarks>
    public static async Task<IValidationResult<TValue>> AssertErrorsAsync<TValue>(this IValidator<TValue> validator, TValue value, params IEnumerable<ApiError> expectedErrors)
    {
        if (expectedErrors == null || !expectedErrors.Any())
            throw new ArgumentException($"At least one expected error must be provided; alternatively, use {nameof(AssertSuccess)}/{nameof(AssertSuccessAsync)} where asserting a successful validation).", nameof(expectedErrors));

        var vr = await validator.ValidateAsync(value).ConfigureAwait(false);
        vr.HasErrors.Should().BeTrue();
        vr.Messages.Should().NotBeNull().And.HaveCountGreaterThan(0);

        var actualErrors = vr.Messages.Where(x => x.Type == CoreEx.Entities.MessageType.Error).Select(x => new ApiError(x.Property, x.Text?.ToString() ?? "none")).ToArray();
        if (!Assertor.TryAreErrorsMatched(expectedErrors, actualErrors, out var errorMessage))
            false.Should().BeTrue(because: errorMessage);

        return vr;
    }

    /// <summary>
    /// Asserts that the <see cref="ValidationException"/> has errors and that the expected errors are present.
    /// </summary>
    /// <param name="validationException">The <see cref="ValidationException"/> to assert.</param>
    /// <param name="expectedErrors">The expected errors.</param>
    /// <returns>The <see cref="ValidationException"/> to support fluent-style method-chaining.</returns>
    public static ValidationException AssertErrors(this ValidationException validationException, params ApiError[] expectedErrors)
    {
        if (expectedErrors == null || !expectedErrors.Any())
            throw new ArgumentException($"At least one expected error must be provided; alternatively, use {nameof(AssertSuccess)}/{nameof(AssertSuccessAsync)} where asserting a successful validation).", nameof(expectedErrors));

        validationException.ThrowIfNull();
        validationException.Messages.Should().NotBeNull().And.HaveCountGreaterThan(0);

        var actualErrors = validationException.Messages.Where(x => x.Type == CoreEx.Entities.MessageType.Error).Select(x => new ApiError(x.Property, x.Text?.ToString() ?? "none")).ToArray();
        if (!Assertor.TryAreErrorsMatched(expectedErrors, actualErrors, out var errorMessage))
            false.Should().BeTrue(because: errorMessage);

        return validationException;
    }
}
