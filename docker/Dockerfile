# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY src/src.sln ./src/
COPY src/JunoBank.Core/JunoBank.Core.csproj ./src/JunoBank.Core/
COPY src/JunoBank.Web/JunoBank.Web.csproj ./src/JunoBank.Web/

# Restore dependencies
WORKDIR /src/src
RUN dotnet restore

# Copy source code
WORKDIR /src
COPY src/ ./src/

# Build and publish
WORKDIR /src/src/JunoBank.Web
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN groupadd -r junobank && useradd -r -g junobank junobank

# Create data and keys directories
RUN mkdir -p /app/data /app/keys && chown -R junobank:junobank /app/data /app/keys

# Copy published app
COPY --from=build /app/publish .

# Set ownership
RUN chown -R junobank:junobank /app

# Switch to non-root user
USER junobank

# Expose port
EXPOSE 5050

# Configure ASP.NET Core
ENV ASPNETCORE_URLS=http://+:5050
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "JunoBank.Web.dll"]
