# Base Image for Running Services
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Install SQL Server Tools (sqlcmd)
RUN apt-get update && apt-get install -y curl gnupg2 && \
    curl -sSL https://packages.microsoft.com/keys/microsoft.asc | apt-key add - && \
    curl -sSL https://packages.microsoft.com/config/debian/10/prod.list | tee /etc/apt/sources.list.d/mssql-release.list && \
    apt-get update && ACCEPT_EULA=Y apt-get install -y mssql-tools unixodbc-dev && \
    echo 'export PATH="$PATH:/opt/mssql-tools/bin"' >> /etc/bash.bashrc && \
    apt-get clean

ENV PATH="${PATH}:/opt/mssql-tools/bin"

# Build Image for Compilation
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the entire source code
COPY src/ .

# Build Rating Service
WORKDIR /src/RatingService/RatingService.Api
RUN dotnet restore
RUN dotnet build -c Release -o /app/rating/build
RUN dotnet publish -c Release -o /app/rating/publish

# Build Notification Service
WORKDIR /src/NotificationService/NotificationService.Api
RUN dotnet restore
RUN dotnet build -c Release -o /app/notification/build
RUN dotnet publish -c Release -o /app/notification/publish

# Rating Service Final
FROM base AS rating-final
WORKDIR /app
COPY --from=build /app/rating/publish .
ENTRYPOINT ["dotnet", "RatingService.Api.dll"]

# Notification Service Final
FROM base AS notification-final
WORKDIR /app  
COPY --from=build /app/notification/publish .
ENTRYPOINT ["dotnet", "NotificationService.Api.dll"]