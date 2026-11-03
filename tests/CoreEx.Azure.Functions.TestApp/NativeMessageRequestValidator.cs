namespace CoreEx.Azure.Functions.TestApp;

/// <summary>
/// Validates the uniform native message request.
/// </summary>
public sealed class NativeMessageRequestValidator : Validator<NativeMessageRequest, NativeMessageRequestValidator>
{
    /// <summary>
    /// Initializes the required outcome rule.
    /// </summary>
    public NativeMessageRequestValidator() => Property(x => x.Outcome).Mandatory();
}
