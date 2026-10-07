FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props WorkflowConfig.sln ./
COPY src/WorkflowConfig.Domain/WorkflowConfig.Domain.csproj src/WorkflowConfig.Domain/
COPY src/WorkflowConfig.Persistence/WorkflowConfig.Persistence.csproj src/WorkflowConfig.Persistence/
COPY src/WorkflowConfig.Application/WorkflowConfig.Application.csproj src/WorkflowConfig.Application/
COPY src/WorkflowConfig.Infrastructure/WorkflowConfig.Infrastructure.csproj src/WorkflowConfig.Infrastructure/
COPY src/WorkflowConfig.Api/WorkflowConfig.Api.csproj src/WorkflowConfig.Api/
RUN dotnet restore src/WorkflowConfig.Api/WorkflowConfig.Api.csproj
COPY src/ src/
RUN dotnet publish src/WorkflowConfig.Api/WorkflowConfig.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
# curl cho healthcheck của docker compose
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
# thư mục upload (ảnh workflow) + log ghi được bởi user không phải root
RUN mkdir -p /app/App_Data/uploads /app/logs && chown -R $APP_UID /app/App_Data /app/logs
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "WorkflowConfig.Api.dll"]
