FROM mcr.microsoft.com/mssql/server:2022-latest AS base
USER root
# Install dotnet sdk
# RUN apt-get update; \
#   apt-get install -y apt-transport-https && \
#   apt-get update && \
#   apt-get install -y dotnet-runtime-6.0

RUN wget -q https://dot.net/v1/dotnet-install.sh \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh --runtime aspnetcore -c 6.0
ENV PATH="${PATH}:/root/.dotnet"

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

# todo: - remove this
COPY "NuGet.config" "NuGet.config"
COPY "nuget-publish" "nuget-publish"

RUN dotnet restore "Company.AppName.sln"

COPY . .
WORKDIR /src/Company.AppName.Database
RUN dotnet build -c Release -o /dbex/build

FROM base as final
USER root

ENV ACCEPT_EULA Y
ENV MSSQL_SA_PASSWORD sAPWD23.^0
ENV MSSQL_TCP_PORT 1433
ENV MSSQL_AGENT_ENABLED true
ENV ConnectionStrings__Database="Data Source=localhost, $MSSQL_TCP_PORT;Initial Catalog=Company.AppName;User id=sa;Password=$MSSQL_SA_PASSWORD;TrustServerCertificate=true"


# Copy setup scripts
WORKDIR /usr/local/
COPY --from=build /dbex/build /dbex
COPY  Company.AppName.Database/wait-for-it.sh Company.AppName.Database/entrypoint.sh ./

RUN chmod +x ./*.sh

ENTRYPOINT ["/usr/local/entrypoint.sh"]
CMD ["sql"]