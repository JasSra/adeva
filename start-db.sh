#!/bin/bash

# Start SQL Server for DebtManager application
# This script starts the SQL Server database container required for local development

echo "🚀 Starting SQL Server database container..."
cd "$(dirname "$0")/deploy"

docker compose up -d

if [ $? -eq 0 ]; then
    echo "✅ SQL Server started successfully on localhost:1433"
    echo ""
    echo "Database credentials (from appsettings.json):"
    echo "  Server: localhost,1433"
    echo "  Username: sa"
    echo "  Password: Your_strong_password123"
    echo ""
    echo "To check if it's running: docker ps | grep sql"
    echo "To view logs: docker logs debtmanager-sql"
    echo "To stop: cd deploy && docker compose down"
    echo ""
    echo "Now you can run the application and the Scenarios page will work!"
else
    echo "❌ Failed to start SQL Server"
    echo "Make sure Docker is installed and running"
    exit 1
fi

