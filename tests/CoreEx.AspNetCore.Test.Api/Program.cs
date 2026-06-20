using CoreEx.AspNetCore.Http;
using CoreEx.AspNetCore.NSwag;
using CoreEx.AspNetCore.Test.Api.Entities;
using CoreEx.AspNetCore.Test.Api.Services;
using CoreEx.Http;
using CoreEx.RefData;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;

namespace CoreEx.AspNetCore.Test.Api;

public class Program
{
    private static void Main(string[] args)
    {
        // Create the web builder.
        var builder = WebApplication.CreateBuilder(args);

        // Add CoreEx host settings.
        builder.AddHostSettings();

        // Add CoreEx services.
        builder.Services
            .AddExecutionContext()
            .AddReferenceDataOrchestrator<ReferenceDataService>()
            .AddMvcWebApi()
            .AddHttpWebApi();

        // Add all the dynamically registered services.
        builder.Services.AddDynamicServicesUsing<PersonService>();

        // Add in-memory based idempotency provider.
        builder.Services
            .AddMemoryCache()
            .AddMemoryOnlyHybridCache()
            .AddHybridCacheIdempotencyProvider();

        // Add the ASP.NET Core services.
        builder.Services.AddControllers();

        // Add the OpenAPI services.
        builder.Services.AddOpenApiDocument(s =>
        {
            s.Title = builder.Environment.ApplicationName;
            s.AddCoreExConfiguration();
        });

        // Add OpenTelemetry tracing.
        builder.WithCoreExTelemetry()
            .UseOtlpExporter();

        // Build the application.
        var app = builder.Build();

        // Configure the pipeline/middleware (order is important).
        app.UseCoreExExceptionHandler();
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.UseExecutionContext();
        app.UseIdempotencyKey();
        app.MapControllers();

        app.UseOpenApi();
        app.UseSwaggerUi();

        app.MapHealthChecks();

        // Minimal APIs.
        app.MapGet("api/persons/{id}",
            (HttpRequest request, WebApi webApi, PersonService2 service, string id)
                => webApi.GetWithResultAsync(request, (ro, _) => service.GetAsync(id)))
            .Produces<Person>().ProducesNotFoundProblem();

        app.MapGet("api/persons",
            (HttpRequest request, WebApi webApi, PersonService2 service)
                => webApi.GetWithResultAsync(request, (ro, _) => service.GetByQueryAsync(ro.QueryArgs, ro.PagingArgs)))
            .Produces<Person[]>().WithQuery(true, true).WithPaging(true);

        app.MapPost("api/persons",
            (HttpRequest request, WebApi webApi, PersonService2 service)
                => webApi.PostWithResultAsync<Person, Person>(request, async (ro, _) =>
                {
                    ro.WithLocationUri(p => new Uri($"api/persons/{p.Id}", UriKind.Relative));
                    return await service.CreateAsync(ro.Value).ConfigureAwait(false);
                }))
            .Accepts<Person>().ProducesCreated<Person>();

        app.MapPut("api/persons/{id}",
            (HttpRequest request, WebApi webApi, PersonService2 service, string id)
                => webApi.PutWithResultAsync<Person, Person>(request, (ro, _) => service.UpdateAsync(ro.Value.Adjust(p => p.Id = id))))
            .Accepts<Person>().Produces<Person>().ProducesNotFoundProblem();

        app.MapPatch("api/persons/{id}",
            (HttpRequest request, WebApi webApi, PersonService2 service, string id)
                => webApi.PatchWithResultAsync<Person>(request,
                    get: (ro, _) => service.GetAsync(id),
                    put: (ro, _) => service.UpdateAsync(ro.Value.Adjust(p => p.Id = id))))
            .Accepts<Person>(HttpNames.MergePatchJsonMediaTypeName).Produces<Person>().ProducesNotFoundProblem();

        app.MapDelete("api/persons/{id}",
            (HttpRequest request, WebApi webApi, PersonService2 service, string id)
                => webApi.DeleteWithResultAsync(request, (ro, _) => service.DeleteAsync(id)))
            .ProducesNoContent();

        app.MapGet("api/referencedata/genders",
            (HttpRequest request, WebApi webApi, [FromQuery] string[]? codes = default, string? text = default)
                => webApi.GetAsync(request, (ro, ct) => ReferenceDataOrchestrator.Current.GetWithFilterAsync<Gender>(codes, text, ro.IsIncludeInactive, ct)))
            .Produces<Gender[]>();

        app.MapPost("api/idempotency-key/test/{id}",
            (HttpRequest request, WebApi webApi, int id)
                => webApi.PostAsync<object>(request, (_, _) => Task.FromResult<object>(new { Id = id, Name = "Jen" })))
            .WithIdempotencyKey(isRequired: true);

        // Run the application.
        app.Run();
    }
}