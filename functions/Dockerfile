# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY functions/Functions.sln ./
# ソリューションは src/ および tests/ 配下を参照するため同じ構造で配置
COPY functions/src ./src
COPY functions/tests ./tests
RUN dotnet restore Functions.sln
RUN dotnet publish src/Kopitra.ManagementApi/Kopitra.ManagementApi.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true
WORKDIR /home/site/wwwroot
COPY --from=build /app/publish .
EXPOSE 80
