# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER root
RUN apt-get update && apt-get install -y libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 8080

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy Directory.Build.props first (build optimization)
COPY services/payments/Directory.Build.props ./

# Copy shared common projects (required for ProjectReferences)
COPY ["shared/common/src/TC.CloudGames.Messaging/TC.CloudGames.Messaging.csproj", "shared/common/src/TC.CloudGames.Messaging/"]
COPY ["shared/common/src/TC.CloudGames.Contracts/TC.CloudGames.Contracts.csproj", "shared/common/src/TC.CloudGames.Contracts/"]
COPY ["shared/common/src/TC.CloudGames.SharedKernel/TC.CloudGames.SharedKernel.csproj", "shared/common/src/TC.CloudGames.SharedKernel/"]

# Copy project files maintaining directory structure (required for ProjectReferences)
COPY ["services/payments/src/Adapters/Inbound/TC.CloudGames.Payments.Api/TC.CloudGames.Payments.Api.csproj", "services/payments/src/Adapters/Inbound/TC.CloudGames.Payments.Api/"]
COPY ["services/payments/src/Core/TC.CloudGames.Payments.Application/TC.CloudGames.Payments.Application.csproj", "services/payments/src/Core/TC.CloudGames.Payments.Application/"]
COPY ["services/payments/src/Core/TC.CloudGames.Payments.Domain/TC.CloudGames.Payments.Domain.csproj", "services/payments/src/Core/TC.CloudGames.Payments.Domain/"]
COPY ["services/payments/src/Adapters/Outbound/TC.CloudGames.Payments.Infrastructure/TC.CloudGames.Payments.Infrastructure.csproj", "services/payments/src/Adapters/Outbound/TC.CloudGames.Payments.Infrastructure/"]

# Restore packages
RUN dotnet restore "services/payments/src/Adapters/Inbound/TC.CloudGames.Payments.Api/TC.CloudGames.Payments.Api.csproj"

# Copy source code maintaining structure
COPY . .

# Build the application
WORKDIR "/src/services/payments/src/Adapters/Inbound/TC.CloudGames.Payments.Api"
RUN dotnet build "TC.CloudGames.Payments.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "TC.CloudGames.Payments.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TC.CloudGames.Payments.Api.dll"]