FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# It's important to keep lines from here down to "COPY . ." identical in all Dockerfiles
# to take advantage of Docker's build cache, to speed up local container builds
COPY "samples/My.Hr/My.Hr.sln" "samples/My.Hr/My.Hr.sln"

COPY "samples/My.Hr/My.Hr.Api/My.Hr.Api.csproj" "samples/My.Hr/My.Hr.Api/My.Hr.Api.csproj"
COPY "samples/My.Hr/My.Hr.Business/My.Hr.Business.csproj" "samples/My.Hr/My.Hr.Business/My.Hr.Business.csproj"
# COPY "samples/My.Hr/My.Hr.CodeGen/My.Hr.CodeGen.csproj" "samples/My.Hr/My.Hr.CodeGen/My.Hr.CodeGen.csproj"
# COPY "samples/My.Hr/My.Hr.Common/My.Hr.Common.csproj" "samples/My.Hr/My.Hr.Common/My.Hr.Common.csproj"
COPY "samples/My.Hr/My.Hr.Database/My.Hr.Database.csproj" "samples/My.Hr/My.Hr.Database/My.Hr.Database.csproj"
# COPY "samples/My.Hr/My.Hr.Test/My.Hr.Test.csproj" "samples/My.Hr/My.Hr.Test/My.Hr.Test.csproj"

COPY "src/CoreEx/CoreEx.csproj" "src/CoreEx/CoreEx.csproj"
COPY "src/CoreEx.FluentValidation/CoreEx.FluentValidation.csproj" "src/CoreEx.FluentValidation/CoreEx.FluentValidation.csproj"
COPY "src/CoreEx.HealthChecks/CoreEx.HealthChecks.csproj" "src/CoreEx.HealthChecks/CoreEx.HealthChecks.csproj"
COPY "src/CoreEx.Azure/CoreEx.Azure.csproj" "src/CoreEx.Azure/CoreEx.Azure.csproj"
COPY "src/CoreEx.Newtonsoft/CoreEx.Newtonsoft.csproj" "src/CoreEx.Newtonsoft/CoreEx.Newtonsoft.csproj"

RUN dotnet restore "samples/My.Hr/My.Hr.sln"

COPY . .
WORKDIR /src/samples/My.Hr/My.Hr.Api
RUN dotnet publish --no-restore -c Release -o /app

FROM build as unittest
WORKDIR /src/samples/My.Hr/My.Hr.Test

FROM build AS publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "My.Hr.Api.dll"]
