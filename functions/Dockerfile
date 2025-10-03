# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY functions/Functions.sln ./
COPY functions/src ./functions/src
RUN dotnet restore functions/Functions.sln
RUN dotnet publish functions/src/Kopitra.ManagementApi/Kopitra.ManagementApi.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true
WORKDIR /home/site/wwwroot
COPY --from=build /app/publish .
EXPOSE 7071
CMD ["func", "start", "--port", "7071", "--verbose"]
