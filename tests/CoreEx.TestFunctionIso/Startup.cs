using CoreEx.Azure.ServiceBus;
using CoreEx.Hosting;
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
                .AddScoped<ServiceBusSubscriber>();
        }
    }
}