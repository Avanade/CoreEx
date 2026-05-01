namespace Contoso.E2E.Runner.Infrastructure;

/// <summary>
/// Provides the possible results of executing a choice in the <see cref="ChoiceManager"/>.
/// </summary>
public enum ChoiceResult
{
    Continue,
    ContinueWithPrompt,
    Stop,
    RequiresApi,
    RetryApi
}