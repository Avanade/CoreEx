FROM mcr.microsoft.com/dotnet/sdk:6.0 AS installer-env
# set to true for local runs - switches functions auth to anoymous
ARG LOCAL

# Build requires 3.1 SDK
COPY --from=mcr.microsoft.com/dotnet/core/sdk:3.1 /usr/share/dotnet /usr/share/dotnet

WORKDIR /src
# It's important to keep lines from here down to "COPY . ." identical in all Dockerfiles
# to take advantage of Docker's build cache, to speed up local container builds
COPY "Company.AppName.sln" "Company.AppName.sln"

COPY "Company.AppName.Api/Company.AppName.Api.csproj" "Company.AppName.Api/Company.AppName.Api.csproj"
COPY "Company.AppName.Business/Company.AppName.Business.csproj" "Company.AppName.Business/Company.AppName.Business.csproj"
COPY "Company.AppName.Database/Company.AppName.Database.csproj" "Company.AppName.Database/Company.AppName.Database.csproj"
COPY "Company.AppName.Functions/Company.AppName.Functions.csproj" "Company.AppName.Functions/Company.AppName.Functions.csproj"
COPY "Company.AppName.UnitTest/Company.AppName.UnitTest.csproj" "Company.AppName.UnitTest/Company.AppName.UnitTest.csproj"
COPY "Company.AppName.Infra/Company.AppName.Infra.csproj" "Company.AppName.Infra/Company.AppName.Infra.csproj"
COPY "Company.AppName.Infra.Tests/Company.AppName.Infra.Tests.csproj" "Company.AppName.Infra.Tests/Company.AppName.Infra.Tests.csproj"

RUN dotnet restore "Company.AppName.sln"

COPY . .

WORKDIR /src/Company.AppName.Functions

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