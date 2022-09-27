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
COPY "src/CoreEx.EntityFrameworkCore/CoreEx.EntityFrameworkCore.csproj" "src/CoreEx.EntityFrameworkCore/CoreEx.EntityFrameworkCore.csproj"
COPY "src/CoreEx.FluentValidation/CoreEx.FluentValidation.csproj" "src/CoreEx.FluentValidation/CoreEx.FluentValidation.csproj"
COPY "src/CoreEx.Newtonsoft/CoreEx.Newtonsoft.csproj" "src/CoreEx.Newtonsoft/CoreEx.Newtonsoft.csproj"
COPY "src/CoreEx.Validation/CoreEx.Validation.csproj" "src/CoreEx.Validation/CoreEx.Validation.csproj"

RUN dotnet restore "samples/My.Hr/My.Hr.sln"

COPY . .
WORKDIR /src/samples/My.Hr/My.Hr.Api
RUN dotnet publish --no-restore -c Release -o /app

FROM build as unittest
WORKDIR /src/samples/My.Hr/My.Hr.Test
# can run tests here on buils

FROM build AS publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "My.Hr.Api.dll"]
