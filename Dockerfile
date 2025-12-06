# GBL-AX2012-MCP Server Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY GBL.AX2012.MCP.sln .
COPY src/GBL.AX2012.MCP.Core/GBL.AX2012.MCP.Core.csproj src/GBL.AX2012.MCP.Core/
COPY src/GBL.AX2012.MCP.Server/GBL.AX2012.MCP.Server.csproj src/GBL.AX2012.MCP.Server/
COPY src/GBL.AX2012.MCP.AxConnector/GBL.AX2012.MCP.AxConnector.csproj src/GBL.AX2012.MCP.AxConnector/
COPY src/GBL.AX2012.MCP.Audit/GBL.AX2012.MCP.Audit.csproj src/GBL.AX2012.MCP.Audit/

# Restore
RUN dotnet restore

# Copy source
COPY src/ src/

# Build
RUN dotnet publish src/GBL.AX2012.MCP.Server/GBL.AX2012.MCP.Server.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user
RUN adduser --disabled-password --gecos '' appuser

# Copy published app
COPY --from=build /app/publish .

# Create logs directory
RUN mkdir -p /app/logs && chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose ports
EXPOSE 8080 9090

# Environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "GBL.AX2012.MCP.Server.dll"]
