using CoreEx.Hosting;
using CoreEx.TestFunctionIso;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureHostStartup<Startup>()
    .ConfigureFunctionsWebApplication()
    .Build();

host.Run();