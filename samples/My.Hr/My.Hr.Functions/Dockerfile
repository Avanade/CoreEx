FROM mcr.microsoft.com/dotnet/sdk:6.0 AS installer-env
# set to true for local runs - switches functions auth to anoymous
ARG LOCAL

# Build requires 3.1 SDK
COPY --from=mcr.microsoft.com/dotnet/core/sdk:3.1 /usr/share/dotnet /usr/share/dotnet

WORKDIR /src
# It's important to keep lines from here down to "COPY . ." identical in all Dockerfiles
# to take advantage of Docker's build cache, to speed up local container builds
COPY "samples/My.Hr/My.Hr.sln" "samples/My.Hr/My.Hr.sln"

COPY "samples/My.Hr/My.Hr.Api/My.Hr.Api.csproj" "samples/My.Hr/My.Hr.Api/My.Hr.Api.csproj"
COPY "samples/My.Hr/My.Hr.Business/My.Hr.Business.csproj" "samples/My.Hr/My.Hr.Business/My.Hr.Business.csproj"
COPY "samples/My.Hr/My.Hr.Database/My.Hr.Database.csproj" "samples/My.Hr/My.Hr.Database/My.Hr.Database.csproj"
COPY "samples/My.Hr/My.Hr.Functions/My.Hr.Functions.csproj" "samples/My.Hr/My.Hr.Functions/My.Hr.Functions.csproj"

COPY "src/CoreEx/CoreEx.csproj" "src/CoreEx/CoreEx.csproj"
COPY "src/CoreEx.FluentValidation/CoreEx.FluentValidation.csproj" "src/CoreEx.FluentValidation/CoreEx.FluentValidation.csproj"
COPY "src/CoreEx.HealthChecks/CoreEx.HealthChecks.csproj" "src/CoreEx.HealthChecks/CoreEx.HealthChecks.csproj"
COPY "src/CoreEx.Messaging.Azure/CoreEx.Messaging.Azure.csproj" "src/CoreEx.Messaging.Azure/CoreEx.Messaging.Azure.csproj"
COPY "src/CoreEx.Newtonsoft/CoreEx.Newtonsoft.csproj" "src/CoreEx.Newtonsoft/CoreEx.Newtonsoft.csproj"

RUN dotnet restore "samples/My.Hr/My.Hr.sln"

COPY . .

WORKDIR /src/samples/My.Hr/My.Hr.Functions

RUN mkdir -p /home/site/wwwroot && \
    dotnet publish *.csproj --no-restore -c Debug --output /home/site/wwwroot && \
    if [ "$LOCAL" = "true" ] ; then find / \( -type f -name .git -prune \) -o -type f -name "function.json" -print0 | xargs -0 sed -i 's/authLevel\": \"function/authLevel\": \"anonymous/g' ; fi

# To enable ssh & remote debugging on app service change the base image to the one below
# FROM mcr.microsoft.com/azure-functions/dotnet:4-appservice
# FROM mcr.microsoft.com/azure-functions/dotnet:4
FROM mcr.microsoft.com/azure-functions/dotnet:4-appservice

ENV AzureWebJobsScriptRoot=/home/site/wwwroot
ENV AzureFunctionsJobHost__Logging__Console__IsEnabled=true
ENV AzureFunctionsJobHost__Logging__LogLevel__CoreEx=Debug
ENV AzureFunctionsJobHost__Logging__LogToConsole=true
ENV AzureFunctionsJobHost__Logging__LogToConsoleColor=true

COPY --from=installer-env ["/home/site/wwwroot", "/home/site/wwwroot"]