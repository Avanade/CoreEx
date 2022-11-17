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
COPY "samples/My.Hr/My.Hr.UnitTest/My.Hr.UnitTest.csproj" "samples/My.Hr/My.Hr.UnitTest/My.Hr.UnitTest.csproj" 
COPY "samples/My.Hr/My.Hr.Infra/My.Hr.Infra.csproj" "samples/My.Hr/My.Hr.Infra/My.Hr.Infra.csproj"
COPY "samples/My.Hr/My.Hr.Infra.Tests/My.Hr.Infra.Tests.csproj" "samples/My.Hr/My.Hr.Infra.Tests/My.Hr.Infra.Tests.csproj"

COPY "src/CoreEx/CoreEx.csproj" "src/CoreEx/CoreEx.csproj"
COPY "src/CoreEx.AutoMapper/CoreEx.AutoMapper.csproj" "src/CoreEx.AutoMapper/CoreEx.AutoMapper.csproj"
COPY "src/CoreEx.Azure/CoreEx.Azure.csproj" "src/CoreEx.Azure/CoreEx.Azure.csproj"
COPY "src/CoreEx.Cosmos/CoreEx.Cosmos.csproj" "src/CoreEx.Cosmos/CoreEx.Cosmos.csproj"
COPY "src/CoreEx.Database/CoreEx.Database.csproj" "src/CoreEx.Database/CoreEx.Database.csproj"
COPY "src/CoreEx.Database.SqlServer/CoreEx.Database.SqlServer.csproj" "src/CoreEx.Database.SqlServer/CoreEx.Database.SqlServer.csproj"
COPY "src/CoreEx.EntityFrameworkCore/CoreEx.EntityFrameworkCore.csproj" "src/CoreEx.EntityFrameworkCore/CoreEx.EntityFrameworkCore.csproj"
COPY "src/CoreEx.FluentValidation/CoreEx.FluentValidation.csproj" "src/CoreEx.FluentValidation/CoreEx.FluentValidation.csproj"
COPY "src/CoreEx.Newtonsoft/CoreEx.Newtonsoft.csproj" "src/CoreEx.Newtonsoft/CoreEx.Newtonsoft.csproj"
COPY "src/CoreEx.Validation/CoreEx.Validation.csproj" "src/CoreEx.Validation/CoreEx.Validation.csproj"

RUN dotnet restore "samples/My.Hr/My.Hr.sln"

COPY . .

WORKDIR /src/samples/My.Hr/My.Hr.Functions

RUN mkdir -p /home/site/wwwroot && \
    dotnet publish *.csproj --no-restore -c Debug --output /home/site/wwwroot && \
    echo LOCAL is "$LOCAL" && \
    echo $(if [ "$LOCAL" = "true" ] ; then find / \( -type f -name .git -prune \) -o -type f -name "function.json" -print0 | xargs -0 sed -i 's/authLevel\": \"function/authLevel\": \"anonymous/g' ; fi)

# To enable ssh & remote debugging on app service change the base image to the one below
# FROM mcr.microsoft.com/azure-functions/dotnet:4-appservice
# FROM mcr.microsoft.com/azure-functions/dotnet:4
FROM mcr.microsoft.com/azure-functions/dotnet:4-appservice
ARG LOCAL

ENV AzureWebJobsScriptRoot=/home/site/wwwroot
ENV AzureFunctionsJobHost__Logging__Console__IsEnabled=true
ENV AzureFunctionsJobHost__Logging__LogLevel__CoreEx=Debug
ENV AzureFunctionsJobHost__Logging__LogToConsole=true
ENV AzureFunctionsJobHost__Logging__LogToConsoleColor=true

COPY --from=installer-env ["/home/site/wwwroot", "/home/site/wwwroot"]