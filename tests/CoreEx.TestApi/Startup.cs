using CoreEx.Configuration;
using CoreEx.Events;
using CoreEx.Json;
using CoreEx.TestFunction;
using CoreEx.TestFunction.Services;
using CoreEx.AspNetCore.WebApis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CoreEx.TestApi.Validation;

namespace CoreEx.TestApi
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Register the core services.
            services
                .AddSettings<TestSettings>()
                .AddExecutionContext()
                .AddJsonSerializer()
                .AddEventDataSerializer()
                .AddNullEventPublisher()
                .AddValidationTextProvider()
                .AddScoped<WebApi, WebApi>();

            // Register the typed backend http client.
            services.AddTypedHttpClient<BackendHttpClient>("Backend", (sp, hc) =>
            {
                var settings = sp.GetService<TestSettings>();
                hc.BaseAddress = settings.BackendBaseAddress;
            });

            // Register the underlying function services.
            services
                .AddAutoMapper(typeof(ProductService).Assembly)
                .AddScoped<ProductService>()
                .AddScoped<ProductValidator>();

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
