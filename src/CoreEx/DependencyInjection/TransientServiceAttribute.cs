namespace CoreEx.DependencyInjection;

/// <summary>
/// Indicates that the underlying implementation <see cref="Type"/> should be registered as a <see cref="ServiceLifetime.Transient"/> service with an optional <see cref="ServiceLifetimeAttribute.Key"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class TransientServiceAttribute : ServiceLifetimeAttribute
{
    /// <inheritdoc/>
    public override ServiceLifetime Lifetime => ServiceLifetime.Transient;
}