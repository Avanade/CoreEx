using CoreEx.Hosting;
using CoreEx.Hosting.Work;
using Microsoft.Extensions.DependencyInjection;

namespace CoreEx.TestFunctionIso
{
    public class Startup : HostStartup
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services
                .AddDefaultSettings()
                .AddExecutionContext()
                .AddJsonSerializer()
                .AddWebApi()
                .AddEventDataSerializer()
                .AddAzureServiceBusSubscriber((sp, s) =>
                {
                    s.WorkStateAlreadyFinishedHandling = Events.Subscribing.ErrorHandling.CompleteWithWarning;
                    s.WorkStateOrchestrator = sp.GetRequiredService<WorkStateOrchestrator>();
                });

            services
                .AddSingleton<IWorkStatePersistence, InMemoryWorkStatePersistence>()
                .AddSingleton<WorkStateOrchestrator>();
        }
    }
}