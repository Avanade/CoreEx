# Temp readme file

## How to create templates

* [Samples](https://github.com/dotnet/samples/tree/main/core/tutorials/cli-templates-create-item-template)
* [Wiki](https://github.com/dotnet/templating/wiki)
* [Docs](https://learn.microsoft.com/en-us/dotnet/core/tools/custom-templates)
* [Tutorial](https://learn.microsoft.com/en-us/dotnet/core/tutorials/cli-templates-create-item-template)
* [NTangle template](https://github.com/Avanade/NTangle/tree/main/tools/NTangle.Template)
* [Template Analyzer](https://github.com/sayedihashimi/template-sample#template-analyzer)

## Dev container

Add [Dev Container](https://code.visualstudio.com/docs/remote/create-dev-container#_use-docker-compose) with docker-compose support that would run the solution.

Extensions required:

* azure functions
* function tools
* az CLI
* Pulumi CLI
* dotnet SDK
* Pulumi VS Extension
* Azurite Extension
* REST Client

--> DONE

Expose ports for function, app service and sql server

## Update readme to use REST Client
        Create: [POST] http://localhost:7071/api/api/employees

        Delete: [DELETE] http://localhost:7071/api/api/employees/{id}

        Get: [GET] http://localhost:7071/api/api/employees/{id}

        GetAll: [GET] http://localhost:7071/api/api/employees

        HealthInfo: [GET] http://localhost:7071/api/health

        HttpTriggerQueueVerificationFunction: [POST] http://localhost:7071/api/employee/verify

        Patch: [PATCH] http://localhost:7071/api/api/employees/{id}

        RenderOAuth2Redirect: [GET] http://localhost:7071/api/oauth2-redirect.html

        RenderOpenApiDocument: [GET] http://localhost:7071/api/openapi/{version}.{extension}

        RenderSwaggerDocument: [GET] http://localhost:7071/api/swagger.{extension}

        RenderSwaggerUI: [GET] http://localhost:7071/api/swagger/ui

        Update: [PUT] http://localhost:7071/api/api/employees/{id}

## Add file that contains recommended VS CODE extensions

DONE

## Readme on configuring ADO

https://github.com/Azure-Samples/todo-csharp-sql/tree/main/.azdo/pipelines