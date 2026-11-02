namespace CoreEx.Azure.Functions.TestApp;

/// <summary>
/// Represents the uniform JSON payload accepted by the native integration trigger.
/// </summary>
/// <param name="Outcome">The requested example outcome.</param>
public sealed record NativeMessageRequest(string? Outcome);
