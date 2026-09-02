namespace CoreEx.Hosting;

/// <summary>
/// Provides the means to flow an owning <typeparamref name="TOwner"/> instance through a <see cref="ResilienceContext"/> so it can be resolved from within a <see cref="ResiliencePipeline{T}"/>'s callbacks
/// (see <see cref="CircuitBreakerResiliency{TOwner}"/> and <see cref="RetryResiliency{TOwner}"/>).
/// </summary>
/// <typeparam name="TOwner">The owning <see cref="Type"/>.</typeparam>
public static class ResilienceOwner<TOwner>
{
    /// <summary>
    /// Gets the <see cref="ResiliencePropertyKey{TOwner}"/> used to flow the owning <typeparamref name="TOwner"/> instance through a <see cref="ResilienceContext"/>. The caller executing a pipeline is
    /// responsible for setting it, e.g. <c>context.Properties.Set(ResilienceOwner&lt;TOwner&gt;.PropertyKey, owner)</c>, before executing the pipeline.
    /// </summary>
    public static ResiliencePropertyKey<TOwner> PropertyKey { get; } = new(typeof(TOwner).FullName ?? typeof(TOwner).Name);

    /// <summary>
    /// Gets the owning <typeparamref name="TOwner"/> instance from the <paramref name="context"/> (previously set via <see cref="PropertyKey"/>).
    /// </summary>
    /// <param name="context">The <see cref="ResilienceContext"/>.</param>
    public static TOwner GetOwner(ResilienceContext context) => context.Properties.GetValue(PropertyKey, default!);
}
