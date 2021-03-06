using System;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

namespace My.Hr.Functions;

/// <summary> Configuration options for <see cref="OpenApiDocumentGenerator"/>. </summary>
public class MyOpenApiConfigurationOptions : DefaultOpenApiConfigurationOptions
{
    public override OpenApiInfo Info { get; set; } = new OpenApiInfo()
    {
        Version = "1.0.1",
        Title = "CoreEx My HR Sample",
        Description = "A serverless Azure Function which demonstrates the use of CoreEx.",
        TermsOfService = new Uri("https://github.com/Avanade/CoreEx"),

        License = new OpenApiLicense()
        {
            Name = "MIT",
            Url = new Uri("http://opensource.org/licenses/MIT"),
        }
    };

    public override OpenApiVersionType OpenApiVersion { get; set; } = OpenApiVersionType.V3;

}