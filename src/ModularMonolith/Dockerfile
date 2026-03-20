FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/ModularMonolith/ModularMonolith.csproj", "src/ModularMonolith/"]
COPY ["src/Modules/ModularMonolith.Modules.Loyalty/ModularMonolith.Modules.Loyalty.csproj", "src/Modules/ModularMonolith.Modules.Loyalty/"]
COPY ["src/Shared/SharedBus/SharedBus.csproj", "src/Shared/SharedBus/"]
COPY ["src/Shared/SharedInfrastructure/SharedInfrastructure.csproj", "src/Shared/SharedInfrastructure/"]
COPY ["src/Shared/SharedKernel/SharedKernel.csproj", "src/Shared/SharedKernel/"]
COPY ["src/Modules/ModularMonolith.Modules.Notification/ModularMonolith.Modules.Notification.csproj", "src/Modules/ModularMonolith.Modules.Notification/"]
COPY ["src/Modules/ModularMonolith.Modules.Payment/ModularMonolith.Modules.Payment.csproj", "src/Modules/ModularMonolith.Modules.Payment/"]
RUN dotnet restore "./src/ModularMonolith/ModularMonolith.csproj"
COPY . .
WORKDIR "/src/src/ModularMonolith"
RUN dotnet build "./ModularMonolith.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ModularMonolith.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ModularMonolith.dll"]