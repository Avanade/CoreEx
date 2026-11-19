namespace CoreEx.Hosting.Work;

/// <summary>
/// Configures <see cref="WorkOrchestrator"/>.
/// </summary>
public sealed class WorkOrchestratorOptions
{
    /// <summary>Gets or sets the maximum time work may remain unfinished; defaults to one day.</summary>
    public TimeSpan Expiry { get; set; } = Internal.GetConfigurationValue<TimeSpan>("CoreEx:Hosting:Work:Expiry", TimeSpan.FromDays(1));

    /// <summary>Gets or sets the minimum retention after work finishes; defaults to one day.</summary>
    public TimeSpan Retention { get; set; } = Internal.GetConfigurationValue<TimeSpan>("CoreEx:Hosting:Work:Retention", TimeSpan.FromDays(1));

    /// <summary>Gets or sets the worker lease duration; defaults to one minute.</summary>
    public TimeSpan LeaseDuration { get; set; } = Internal.GetConfigurationValue<TimeSpan>("CoreEx:Hosting:Work:LeaseDuration", TimeSpan.FromMinutes(1));

    /// <summary>Gets or sets the maximum execution attempts; null means there is no attempt limit.</summary>
    public int? MaxExecutionAttempts { get; set; } = GetMaxExecutionAttempts();

    /// <summary>Gets or sets the maximum time a created operation may wait to start.</summary>
    public TimeSpan? StartTimeout { get; set; } = GetStartTimeout();

    /// <summary>Gets the configured maximum execution attempts, treating zero as unlimited.</summary>
    /// <returns>The configured maximum, or <see langword="null"/> when unlimited.</returns>
    private static int? GetMaxExecutionAttempts()
    {
        var value = Internal.GetConfigurationValue("CoreEx:Hosting:Work:MaxExecutionAttempts", 0);
        return value != 0 ? value : null;
    }

    /// <summary>Gets the configured start timeout, treating zero as unset.</summary>
    /// <returns>The configured timeout, or <see langword="null"/> when unset.</returns>
    private static TimeSpan? GetStartTimeout()
    {
        var value = Internal.GetConfigurationValue("CoreEx:Hosting:Work:StartTimeout", TimeSpan.Zero);
        return value != TimeSpan.Zero ? value : null;
    }

    /// <summary>Validates the options.</summary>
    public void Validate()
    {
        if (Expiry <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(Expiry));

        if (Retention <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(Retention));

        if (LeaseDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(LeaseDuration));

        if (MaxExecutionAttempts is <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaxExecutionAttempts));

        if (StartTimeout is { } startTimeout && (startTimeout <= TimeSpan.Zero || startTimeout > Expiry))
            throw new ArgumentOutOfRangeException(nameof(StartTimeout), "The start timeout must be greater than zero and no greater than the work expiry.");
    }
}
