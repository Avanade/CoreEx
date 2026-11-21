namespace CoreEx.AspNetCore.Publishing;

/// <summary>
/// Configures tracked acceptance for one publish operation.
/// </summary>
public sealed class WebApiPublishOperationOptions
{
    /// <summary>
    /// Gets or sets the maximum time the created operation may wait to start.
    /// </summary>
    public TimeSpan StartTimeout { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Gets or sets the maximum time the operation may remain unfinished.
    /// </summary>
    public TimeSpan? Expiry { get; set; }
}
