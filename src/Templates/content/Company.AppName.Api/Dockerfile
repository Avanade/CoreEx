FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# It's important to keep lines from here down to "COPY . ." identical in all Dockerfiles
# to take advantage of Docker's build cache, to speed up local container builds
COPY "samples/Company.AppName/Company.AppName.sln" "samples/Company.AppName/Company.AppName.sln"

COPY "samples/Company.AppName/Company.AppName.Api/Company.AppName.Api.csproj" "samples/Company.AppName/Company.AppName.Api/Company.AppName.Api.csproj"
COPY "samples/Company.AppName/Company.AppName.Business/Company.AppName.Business.csproj" "samples/Company.AppName/Company.AppName.Business/Company.AppName.Business.csproj"
COPY "samples/Company.AppName/Company.AppName.Database/Company.AppName.Database.csproj" "samples/Company.AppName/Company.AppName.Database/Company.AppName.Database.csproj"
COPY "samples/Company.AppName/Company.AppName.Functions/Company.AppName.Functions.csproj" "samples/Company.AppName/Company.AppName.Functions/Company.AppName.Functions.csproj"
COPY "samples/Company.AppName/Company.AppName.Infra/Company.AppName.Infra.csproj" "samples/Company.AppName/Company.AppName.Infra/Company.AppName.Infra.csproj"
COPY "samples/Company.AppName/Company.AppName.Infra.Tests/Company.AppName.Infra.Tests.csproj" "samples/Company.AppName/Company.AppName.Infra.Tests/Company.AppName.Infra.Tests.csproj"

COPY "src/CoreEx/CoreEx.csproj" "src/CoreEx/CoreEx.csproj"
COPY "src/CoreEx.AutoMapper/CoreEx.AutoMapper.csproj" "src/CoreEx.AutoMapper/CoreEx.AutoMapper.csproj"
COPY "src/CoreEx.Azure/CoreEx.Azure.csproj" "src/CoreEx.Azure/CoreEx.Azure.csproj"
COPY "src/CoreEx.Cosmos/CoreEx.Cosmos.csproj" "src/CoreEx.Cosmos/CoreEx.Cosmos.csproj"
COPY "src/CoreEx.Database/CoreEx.Database.csproj" "src/CoreEx.Database/CoreEx.Database.csproj"
COPY "src/CoreEx.EntityFrameworkCore/CoreEx.EntityFrameworkCore.csproj" "src/CoreEx.EntityFrameworkCore/CoreEx.EntityFrameworkCore.csproj"
COPY "src/CoreEx.FluentValidation/CoreEx.FluentValidation.csproj" "src/CoreEx.FluentValidation/CoreEx.FluentValidation.csproj"
COPY "src/CoreEx.Newtonsoft/CoreEx.Newtonsoft.csproj" "src/CoreEx.Newtonsoft/CoreEx.Newtonsoft.csproj"
COPY "src/CoreEx.Validation/CoreEx.Validation.csproj" "src/CoreEx.Validation/CoreEx.Validation.csproj"

RUN dotnet restore "samples/Company.AppName/Company.AppName.sln"

COPY . .
WORKDIR /src/samples/Company.AppName/Company.AppName.Api
RUN dotnet publish --no-restore -c Release -o /app

FROM build as unittest
WORKDIR /src/samples/Company.AppName/Company.AppName.Test
# can run tests here on buils

FROM build AS publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Company.AppName.Api.dll"]
