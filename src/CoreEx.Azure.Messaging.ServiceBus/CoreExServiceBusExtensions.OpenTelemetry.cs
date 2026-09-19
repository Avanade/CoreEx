#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace OpenTelemetry.Trace;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides standard extensions.
/// </summary>
public static class CoreExServiceBusExtensions
{
    /// <summary>
    /// Gets the <c>ServiceBusReceiver.RenewMessageLock</c> activity name, as emitted by the Azure SDK's <c>DiagnosticProperty.RenewMessageLockActivityName</c>.
    /// </summary>
    public const string RenewMessageLockActivityName = "ServiceBusReceiver.RenewMessageLock";

    /// <summary>
    /// Gets the <c>ServiceBusSessionReceiver.RenewSessionLock</c> activity name, as emitted by the Azure SDK's <c>DiagnosticProperty.RenewSessionLockActivityName</c>.
    /// </summary>
    public const string RenewSessionLockActivityName = "ServiceBusSessionReceiver.RenewSessionLock";

    /// <summary>
    /// Gets the <c>ServiceBusReceiver.Receive</c> activity name, as emitted by the Azure SDK's <c>DiagnosticProperty.ReceiveActivityName</c>.
    /// </summary>
    /// <remarks>This is a <c>CLIENT</c>-kind span wrapping every underlying <c>ServiceBusReceiver.ReceiveMessagesAsync</c> call the <c>ServiceBusProcessor</c>/<c>ServiceBusSessionProcessor</c>
    /// background pump makes - it fires unconditionally on every poll, whether or not a message is returned, and is not correlated to any specific message's trace context (that correlation is
    /// carried instead by <c>ServiceBusProcessor.ProcessMessage</c>/<c>ServiceBusSessionProcessor.ProcessSessionMessage</c>).</remarks>
    public const string ReceiveActivityName = "ServiceBusReceiver.Receive";

    /// <summary>
    /// Enables <i>CoreEx</i> OpenTelemetry instrumentation.
    /// </summary>
    /// <param name="builder">The <see cref="OpenTelemetryBuilder"/>.</param>
    /// <param name="includeBackgroundPollingTelemetry">Indicates whether to include the Azure SDK's own background polling spans (<see cref="RenewMessageLockActivityName"/>,
    /// <see cref="RenewSessionLockActivityName"/> and <see cref="ReceiveActivityName"/>); defaults to <see langword="false"/> (excluded).</param>
    /// <returns>The <paramref name="builder"/> to support fluent-style method-chaining.</returns>
    /// <remarks>The two lock-renewal activities fire on a timer for the lifetime of every held message/session lock (see <c>ServiceBusProcessorOptions.MaxAutoLockRenewalDuration</c>), and
    /// <see cref="ReceiveActivityName"/> fires on every underlying receive poll regardless of whether a message comes back - none of these carry business signal, they are pure volume, so they
    /// are dropped by default via a custom <see cref="Sampler"/>. Set <paramref name="includeBackgroundPollingTelemetry"/> to <see langword="true"/> to restore them, e.g. when actively debugging
    /// lock-expiry/session-timeout behaviour or receive-call latency/batch-size.
    /// <para>The dropping <see cref="Sampler"/> wraps <see cref="ParentBasedSampler"/>/<see cref="AlwaysOnSampler"/> (the OpenTelemetry SDK's own default) for every other activity, so no other
    /// sampling behaviour changes. Note this calls <see cref="TracerProviderBuilderExtensions.SetSampler(TracerProviderBuilder, Sampler)"/>: if another CoreEx (or user) OpenTelemetry extension
    /// also calls <c>SetSampler</c> after this one, it will silently override this filter - a known, accepted limitation since no other CoreEx extension currently sets a <see cref="Sampler"/>.</para></remarks>
    public static OpenTelemetryBuilder WithCoreExServiceBusTelemetry(this OpenTelemetryBuilder builder, bool includeBackgroundPollingTelemetry = false) => builder.ThrowIfNull()
        .WithCoreExEventsSources()
        .WithTracing(t =>
        {
            t.AddInvokerAsSource<CoreEx.Azure.Messaging.ServiceBus.ServiceBusReceiverInvoker>()
             .AddSource("Azure.Messaging.ServiceBus")
             .AddSource("Azure.Messaging.ServiceBus.*");

            if (!includeBackgroundPollingTelemetry)
                t.SetSampler(new ServiceBusBackgroundPollingFilteringSampler(new ParentBasedSampler(new AlwaysOnSampler())));
        })
        .WithMetrics(m => m.AddMeter(ServiceBusMetrics.Meter.Name));

    /// <summary>
    /// Gets a value indicating whether <paramref name="activityName"/> is one of the Azure SDK's own background polling activities (<see cref="RenewMessageLockActivityName"/>,
    /// <see cref="RenewSessionLockActivityName"/> or <see cref="ReceiveActivityName"/>) that <see cref="WithCoreExServiceBusTelemetry"/> drops by default.
    /// </summary>
    /// <param name="activityName">The activity name (see <see cref="System.Diagnostics.Activity.OperationName"/>) to check.</param>
    /// <returns><see langword="true"/> if <paramref name="activityName"/> is a background polling activity; otherwise, <see langword="false"/>.</returns>
    public static bool IsBackgroundPollingActivity(string? activityName) => activityName is RenewMessageLockActivityName or RenewSessionLockActivityName or ReceiveActivityName;

    /// <summary>
    /// A <see cref="Sampler"/> that drops the Azure SDK's own background polling activities (see <see cref="IsBackgroundPollingActivity(string?)"/>), delegating every other activity to an
    /// <paramref name="innerSampler"/>.
    /// </summary>
    /// <param name="innerSampler">The <see cref="Sampler"/> to delegate to for any activity that is not one of the filtered background polling names.</param>
    private sealed class ServiceBusBackgroundPollingFilteringSampler(Sampler innerSampler) : Sampler
    {
        /// <inheritdoc/>
        public override SamplingResult ShouldSample(in SamplingParameters samplingParameters) =>
            IsBackgroundPollingActivity(samplingParameters.Name) ? new SamplingResult(SamplingDecision.Drop) : innerSampler.ShouldSample(samplingParameters);
    }
}