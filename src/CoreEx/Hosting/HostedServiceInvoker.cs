namespace CoreEx.Hosting;

/// <summary>
/// Provides the <see cref="IHostedService"/> invoker.
/// </summary>
[InvokerName("CoreEx.Hosting.HostedService")]
public class HostedServiceInvoker : InvokerBase<IHostedService>
{
    /// <inheritdoc/>
    /// <remarks>Tracing is disabled for this invoker as the <see cref="IHostedService"/> is typically used for background processing and therefore may be long-running, or high-frequency, and therefore not ideal for tracing.</remarks>
    public override bool IsTracingDisabled => true;
}