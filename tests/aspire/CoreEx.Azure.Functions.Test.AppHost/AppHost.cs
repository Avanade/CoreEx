var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { Args = args, EnableResourceLogging = true });
if (!builder.ExecutionContext.IsRunMode)
    throw new NotSupportedException("This AppHost is for local runtime tests, not deployment.");

const string sqlPassword = "CoreEx_Emulator!2026";
const string serviceBusConnection = "Endpoint=sb://localhost:5682;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

var storage = builder.AddContainer("functions-storage", "mcr.microsoft.com/azure-storage/azurite", "3.35.0")
    .WithArgs("azurite", "-l", "/data", "--blobHost", "0.0.0.0", "--queueHost", "0.0.0.0", "--tableHost", "0.0.0.0")
    .WithEndpoint(port: 10000, targetPort: 10000, scheme: "http", name: "blob", isProxied: false)
    .WithEndpoint(port: 10001, targetPort: 10001, scheme: "http", name: "queue", isProxied: false)
    .WithEndpoint(port: 10002, targetPort: 10002, scheme: "http", name: "table", isProxied: false);

var sql = builder.AddContainer("functions-sql", "mcr.microsoft.com/mssql/server", "2022-latest")
    .WithEnvironment("ACCEPT_EULA", "Y")
    .WithEnvironment("MSSQL_SA_PASSWORD", sqlPassword);

var serviceBus = builder.AddContainer("functions-servicebus", "mcr.microsoft.com/azure-messaging/servicebus-emulator", "latest")
    .WithEnvironment("ACCEPT_EULA", "Y")
    .WithEnvironment("SQL_SERVER", sql.Resource.Name)
    .WithEnvironment("MSSQL_SA_PASSWORD", sqlPassword)
    .WithEnvironment("EMULATOR_HTTP_PORT", "5300")
    .WithEnvironment("SQL_WAIT_INTERVAL", "30")
    .WithBindMount(Path.Combine(builder.AppHostDirectory, "ServiceBus", "Config.json"), "/ServiceBus_Emulator/ConfigFiles/Config.json")
    .WithEndpoint(port: 5682, targetPort: 5672, scheme: "tcp", name: "amqp", isProxied: false)
    .WithEndpoint(port: 5310, targetPort: 5300, scheme: "http", name: "management", isProxied: false)
    .WithHttpHealthCheck("/health", endpointName: "management")
    .WaitFor(sql);

var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent?.Name ?? throw new InvalidOperationException("Could not determine the AppHost build configuration.");
var functionDirectory = Path.Combine(builder.AppHostDirectory, "bin", configuration, "net10.0", "functions");
var arguments = new[] { "start", "--no-build", "--port", "7099", "--functions", "ReadContext", "ReadTraceObservation", "Create", "Failure", "Messages", "ProcessMessage", "ProcessNativeMessage", "ImportBlob",
    "CreateIdempotent", "CreateIdempotentRequired", "ReadIdempotencyCount", "ReleaseIdempotencyOperation" };
var functions = OperatingSystem.IsWindows()
    ? builder.AddExecutable("functions", "cmd.exe", functionDirectory, ["/c", "func", .. arguments])
    : builder.AddExecutable("functions", "func", functionDirectory, arguments);

functions
    .WithHttpEndpoint(port: 7099, targetPort: 7099, name: "http", isProxied: false)
    .WithHttpHealthCheck("/admin/host/status", endpointName: "http")
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", "dotnet-isolated")
    .WithEnvironment("FUNCTIONS_CORE_TOOLS_TELEMETRY_OPTOUT", "1")
    .WithEnvironment("AzureWebJobsSecretStorageType", "files")
    .WithEnvironment("AzureWebJobsStorage", "UseDevelopmentStorage=true")
    .WithEnvironment("ImportStorage", "UseDevelopmentStorage=true")
    .WithEnvironment("ServiceBus", serviceBusConnection)
    .WaitFor(serviceBus)
    .WaitFor(storage);

builder.Build().Run();
