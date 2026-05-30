# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Configure .NET for reliability and performance
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true \
    DOTNET_CLI_TELEMETRY_OPTOUT=true \
    NUGET_XMLDOC_MODE=skip

# Copy project files only for optimal layer caching
COPY *.csproj ./

# Restore dependencies with stability options
# Disable parallel processing to prevent NuGet EOF errors
RUN dotnet restore --disable-parallel --force

# Copy remaining source code
COPY . .

# Build and publish in Release mode
RUN dotnet publish invmgmt.web.csproj -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0

# Install system dependencies for Npgsql and Kerberos authentication
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    libgssapi-krb5-2 curl && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy published application from build stage
COPY --from=build /app/publish .

EXPOSE 5000

ENTRYPOINT ["dotnet", "invmgmt.web.dll"]