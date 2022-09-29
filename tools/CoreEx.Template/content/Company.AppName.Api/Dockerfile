FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
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
WORKDIR /src/Company.AppName.Api
RUN dotnet publish --no-restore -c Release -o /app

FROM build as unittest
WORKDIR /src/Company.AppName.Test
# can run tests here on buils

FROM build AS publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Company.AppName.Api.dll"]
