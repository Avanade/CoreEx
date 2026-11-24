namespace CoreEx.Hosting.Work;

/// <summary>
/// Represents the latest reported work progress.
/// </summary>
/// <param name="PercentComplete">The optional percentage from 0 through 100.</param>
/// <param name="Message">The optional progress message.</param>
/// <param name="LastUpdated">The provider time when progress was recorded.</param>
public sealed record WorkProgress(int? PercentComplete, string? Message, DateTimeOffset LastUpdated);
