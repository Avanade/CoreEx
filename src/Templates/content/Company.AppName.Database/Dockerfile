FROM mcr.microsoft.com/mssql/server:2019-latest AS base
USER root
# Install dotnet sdk
RUN apt-get update; \
  apt-get install -y apt-transport-https && \
  apt-get update && \
  apt-get install -y dotnet-runtime-6.0

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
WORKDIR /src/samples/Company.AppName/Company.AppName.Database
RUN dotnet build -c Release -o /dbex/build

FROM base as final
USER root

ENV ACCEPT_EULA Y
ENV MSSQL_SA_PASSWORD sAPWD23.^0
ENV MSSQL_TCP_PORT 1433
ENV MSSQL_AGENT_ENABLED true
ENV ConnectionStrings__sqlserver:MyHr Data Source=localhost, $MSSQL_TCP_PORT;Initial Catalog=Company.AppName;User id=sa;Password=$MSSQL_SA_PASSWORD;TrustServerCertificate=true


# Copy setup scripts
WORKDIR /usr/local/
COPY --from=build /dbex/build /dbex
COPY  samples/Company.AppName/Company.AppName.Database/wait-for-it.sh samples/Company.AppName/Company.AppName.Database/entrypoint.sh ./

RUN chmod +x ./*.sh

ENTRYPOINT ["/usr/local/entrypoint.sh"]
CMD ["sql"]