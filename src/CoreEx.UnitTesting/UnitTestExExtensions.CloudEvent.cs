#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace UnitTestEx;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides extensions for <see cref="TesterBase"/> to support common testing scenarios.
/// </summary>
public static partial class UnitTestExExtensions
{
    /// <summary>
    /// Create a <see cref="CloudEvent"/> from the specified <see cref="EventData"/>.
    /// </summary>
    /// <param name="tester">The <see cref="TesterBase"/>.</param>
    /// <param name="event">The <see cref="EventData"/>.</param>
    /// <returns>The <see cref="CloudEvent"/>.</returns>
    /// <remarks>This will use the configured <see cref="IEventFormatter"/> service to format and convert.</remarks>
    public static CloudEvent CreateCloudEventFrom(this TesterBase tester, EventData @event)
    {
        tester.ThrowIfNull();
        var formatter = tester.Services.GetService<IEventFormatter>() ?? ActivatorUtilities.CreateInstance<EventFormatter>(tester.Services);
        return formatter.ConvertToCloudEvent(formatter.Format(@event));
    }

    /// <summary>
    /// Create a <see cref="CloudEvent"/> from the specified JSON resource.
    /// </summary>
    /// <param name="tester">The <see cref="TesterBase"/>.</param>
    /// <param name="resourceName">The embedded resource name (matches to the end of the fully qualified resource name).</param>
    /// <param name="updater">An optional action to update the <see cref="CloudEvent"/> before use.</param>
    /// <param name="assembly">The <see cref="Assembly"/> that contains the embedded resource; defaults to <see cref="Assembly.GetCallingAssembly"/>.</param>
    /// <returns>The <see cref="CloudEvent"/>.</returns>
    public static CloudEvent CreateCloudEventFromJsonResource(this TesterBase tester, string resourceName, Action<CloudEvent>? updater = null, Assembly? assembly = null)
    {
        tester.ThrowIfNull();

        var json = Resource.GetJson(resourceName, assembly ?? Assembly.GetCallingAssembly());
        var jr = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        var je = JsonElement.ParseValue(ref jr);
        return je.DecodeToCloudEvent().Adjust(ce => updater?.Invoke(ce));
    }

    /// <summary>
    /// Create a <see cref="CloudEvent"/> from the specified JSON resource.
    /// </summary>
    /// <typeparam name="TAssembly">The <see cref="Type"/> to infer the <see cref="Assembly"/> for the embedded resource.</typeparam>
    /// <param name="tester">The <see cref="TesterBase"/>.</param>
    /// <param name="resourceName">The embedded resource name (matches to the end of the fully qualified resource name).</param>
    /// <param name="updater">An optional action to update the <see cref="CloudEvent"/> before use.</param>
    /// <returns>The <see cref="CloudEvent"/>.</returns>
    public static CloudEvent CreateCloudEventFromJsonResource<TAssembly>(this TesterBase tester, string resourceName, Action<CloudEvent>? updater = null)
        => tester.ThrowIfNull().CreateCloudEventFromJsonResource(resourceName, updater, typeof(TAssembly).Assembly);
}