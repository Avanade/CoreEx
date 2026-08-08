using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CoreEx.UnitTesting.Test.Unit;

public class EntryPoint
{
    public const string EventsServiceKey = "unittest";

    public static void ConfigureApplication(IHostApplicationBuilder builder)
    {
        // Add CoreEx host settings (provides the CloudEvent Source).
        builder.AddHostSettings("CoreEx.UnitTesting", "UnitTest", new Uri("urn:unit-test"));

        // Add CoreEx services.
        builder.Services
            .AddExecutionContext()
            .AddEventFormatter();

        // Register a no-op root event publisher, then decorate it so published events can be captured/asserted via CoreEx.UnitTesting's EventExpectations.
        builder.Services
            .AddNoOpEventPublisher(EventsServiceKey)
            .UseExpectedEventPublisher(EventsServiceKey);
    }
}
