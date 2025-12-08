# Run Database Migrations for GBL-AX2012-MCP
# This script creates and applies EF Core migrations

param(
    [string]$ConnectionString = "Server=localhost;Database=MCP_Audit;Trusted_Connection=True;TrustServerCertificate=True"
)

Write-Host "=== GBL-AX2012-MCP Database Migration Script ===" -ForegroundColor Cyan
Write-Host ""

# Check if EF Core tools are installed
Write-Host "Checking EF Core tools..." -ForegroundColor Yellow
$efTools = dotnet tool list -g | Select-String "dotnet-ef"
if (-not $efTools) {
    Write-Host "Installing EF Core tools..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to install EF Core tools!" -ForegroundColor Red
        exit 1
    }
}

Write-Host "EF Core tools: OK" -ForegroundColor Green
Write-Host ""

# Set connection string in appsettings.json if provided
if ($ConnectionString) {
    Write-Host "Using connection string: $ConnectionString" -ForegroundColor Yellow
    # Note: In production, use environment variables or user secrets
}

# Navigate to project directory
$projectPath = Join-Path $PSScriptRoot "..\src\GBL.AX2012.MCP.Audit"
$startupProject = Join-Path $PSScriptRoot "..\src\GBL.AX2012.MCP.Server"

Write-Host "Project path: $projectPath" -ForegroundColor Yellow
Write-Host "Startup project: $startupProject" -ForegroundColor Yellow
Write-Host ""

# Check if migration already exists
$migrationPath = Join-Path $projectPath "Migrations\20251206000000_AddWebhookTables.cs"
if (Test-Path $migrationPath) {
    Write-Host "Migration file exists: $migrationPath" -ForegroundColor Green
} else {
    Write-Host "Creating migration..." -ForegroundColor Yellow
    Push-Location $projectPath
    dotnet ef migrations add AddWebhookTables --startup-project $startupProject --context WebhookDbContext
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to create migration!" -ForegroundColor Red
        Pop-Location
        exit 1
    }
    Pop-Location
    Write-Host "Migration created successfully!" -ForegroundColor Green
}

Write-Host ""
Write-Host "Applying migration to database..." -ForegroundColor Yellow
Push-Location $projectPath
dotnet ef database update --startup-project $startupProject --context WebhookDbContext
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to apply migration!" -ForegroundColor Red
    Pop-Location
    exit 1
}
Pop-Location

Write-Host ""
Write-Host "=== Migration completed successfully! ===" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Verify tables created: WebhookSubscriptions, WebhookDeliveries" -ForegroundColor White
Write-Host "2. Test webhook subscription: ax_subscribe_webhook" -ForegroundColor White
Write-Host "3. Check database indexes are created" -ForegroundColor White

