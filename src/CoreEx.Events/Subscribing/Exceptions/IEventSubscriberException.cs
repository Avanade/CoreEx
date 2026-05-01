namespace CoreEx.Events.Subscribing.Exceptions;

/// <summary>
/// Defines an <see cref="EventSubscriberBase"/>-specific <see cref="Exception"/>.
/// </summary>
public interface IEventSubscriberException
{
    /// <summary>
    /// Gets the specific <see cref="Subscribing.ErrorHandling"/> option that resulted in the exception.
    /// </summary>
    ErrorHandling ErrorHandling { get; } 
}