#!/bin/bash
# Run Database Migrations for GBL-AX2012-MCP
# This script creates and applies EF Core migrations

set -e

CONNECTION_STRING="${CONNECTION_STRING:-Server=localhost;Database=MCP_Audit;Trusted_Connection=True;TrustServerCertificate=True}"

echo "=== GBL-AX2012-MCP Database Migration Script ==="
echo ""

# Check if EF Core tools are installed
echo "Checking EF Core tools..."
if ! dotnet tool list -g | grep -q "dotnet-ef"; then
    echo "Installing EF Core tools..."
    dotnet tool install --global dotnet-ef
    if [ $? -ne 0 ]; then
        echo "Failed to install EF Core tools!"
        exit 1
    fi
fi

echo "EF Core tools: OK"
echo ""

# Get script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_PATH="$SCRIPT_DIR/../src/GBL.AX2012.MCP.Audit"
STARTUP_PROJECT="$SCRIPT_DIR/../src/GBL.AX2012.MCP.Server"

echo "Project path: $PROJECT_PATH"
echo "Startup project: $STARTUP_PROJECT"
echo ""

# Check if migration already exists
MIGRATION_PATH="$PROJECT_PATH/Migrations/20251206000000_AddWebhookTables.cs"
if [ -f "$MIGRATION_PATH" ]; then
    echo "Migration file exists: $MIGRATION_PATH"
else
    echo "Creating migration..."
    cd "$PROJECT_PATH"
    dotnet ef migrations add AddWebhookTables --startup-project "$STARTUP_PROJECT" --context WebhookDbContext
    if [ $? -ne 0 ]; then
        echo "Failed to create migration!"
        exit 1
    fi
    echo "Migration created successfully!"
fi

echo ""
echo "Applying migration to database..."
cd "$PROJECT_PATH"
dotnet ef database update --startup-project "$STARTUP_PROJECT" --context WebhookDbContext
if [ $? -ne 0 ]; then
    echo "Failed to apply migration!"
    exit 1
fi

echo ""
echo "=== Migration completed successfully! ==="
echo ""
echo "Next steps:"
echo "1. Verify tables created: WebhookSubscriptions, WebhookDeliveries"
echo "2. Test webhook subscription: ax_subscribe_webhook"
echo "3. Check database indexes are created"

