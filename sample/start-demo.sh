#!/bin/bash

# OfX Telemetry Demo - Quick Start Script
# This script starts the complete observability stack and provides helpful links

set -e

echo "═══════════════════════════════════════════════════════════════"
echo "  OfX Framework - Telemetry Demo Quick Start"
echo "═══════════════════════════════════════════════════════════════"
echo ""

# Check if docker is available
if ! command -v docker &> /dev/null; then
    echo "❌ Error: Docker is not installed or not in PATH"
    echo "   Please install Docker Desktop: https://www.docker.com/products/docker-desktop"
    exit 1
fi

# Check if docker-compose is available
if ! command -v docker-compose &> /dev/null; then
    echo "❌ Error: docker-compose is not installed or not in PATH"
    echo "   Please install docker-compose: https://docs.docker.com/compose/install/"
    exit 1
fi

# Check if dotnet is available
if ! command -v dotnet &> /dev/null; then
    echo "❌ Error: .NET SDK is not installed or not in PATH"
    echo "   Please install .NET 9.0 SDK: https://dotnet.microsoft.com/download"
    exit 1
fi

echo "✅ Prerequisites check passed"
echo ""

# Start infrastructure
echo "🚀 Starting observability infrastructure..."
echo "   (NATS, Jaeger, Prometheus, Grafana, PostgreSQL, MongoDB)"
echo ""

docker-compose up -d

# Wait for services to be ready
echo ""
echo "⏳ Waiting for services to be ready..."
sleep 5

# Check if all containers are running
RUNNING=$(docker-compose ps --services --filter "status=running" | wc -l | tr -d ' ')
TOTAL=$(docker-compose ps --services | wc -l | tr -d ' ')

if [ "$RUNNING" -eq "$TOTAL" ]; then
    echo "✅ All infrastructure services are running ($RUNNING/$TOTAL)"
else
    echo "⚠️  Warning: Some services may not be ready yet ($RUNNING/$TOTAL running)"
    echo "   You can check status with: docker-compose ps"
fi

echo ""
echo "═══════════════════════════════════════════════════════════════"
echo "  Infrastructure is ready!"
echo "═══════════════════════════════════════════════════════════════"
echo ""
echo "📊 Observability Stack URLs:"
echo ""
echo "  Jaeger (Distributed Tracing):"
echo "    🔗 http://localhost:16686"
echo ""
echo "  Prometheus (Metrics):"
echo "    🔗 http://localhost:9090"
echo ""
echo "  Grafana (Dashboards):"
echo "    🔗 http://localhost:3000"
echo "    👤 Username: admin"
echo "    🔑 Password: admin"
echo ""
echo "  NATS Monitoring:"
echo "    🔗 http://localhost:8222/varz"
echo ""
echo "💾 Database Connections:"
echo ""
echo "  PostgreSQL:"
echo "    Host: localhost:5432"
echo "    User: postgres"
echo "    Password: Abcd@2021"
echo "    Databases: OfXTestService1, OfXTestService2, OfXTestService3, OfXTestOtherService1"
echo ""
echo "  MongoDB:"
echo "    Connection: mongodb://localhost:27017"
echo "    Database: Service1MongoDb"
echo ""
echo "═══════════════════════════════════════════════════════════════"
echo "  Next Steps"
echo "═══════════════════════════════════════════════════════════════"
echo ""
echo "1️⃣  Start the services in separate terminals:"
echo ""
echo "   Terminal 1 - Service1 (GraphQL API):"
echo "   $ cd Service1 && dotnet run"
echo ""
echo "   Terminal 2 - Service2 (Worker):"
echo "   $ cd Service2 && dotnet run"
echo ""
echo "   Terminal 3 - Service3 (Worker):"
echo "   $ cd Service3 && dotnet run"
echo ""
echo "2️⃣  Make a GraphQL request:"
echo "   🔗 Open: https://localhost:5001/graphql"
echo ""
echo "3️⃣  View distributed traces:"
echo "   🔗 Open: http://localhost:16686"
echo "   📝 Select 'Service1' and click 'Find Traces'"
echo ""
echo "4️⃣  View metrics dashboard:"
echo "   🔗 Open: http://localhost:3000"
echo "   📝 Navigate to: Dashboards → OfX → OfX Framework Overview"
echo ""
echo "═══════════════════════════════════════════════════════════════"
echo "  Helpful Commands"
echo "═══════════════════════════════════════════════════════════════"
echo ""
echo "  View infrastructure logs:"
echo "  $ docker-compose logs -f"
echo ""
echo "  Check infrastructure status:"
echo "  $ docker-compose ps"
echo ""
echo "  Stop infrastructure:"
echo "  $ docker-compose down"
echo ""
echo "  Stop and clean everything:"
echo "  $ docker-compose down -v"
echo ""
echo "═══════════════════════════════════════════════════════════════"
echo "  For detailed documentation, see:"
echo "  📖 README-TELEMETRY-DEMO.md"
echo "  📖 SETUP-OBSERVABILITY.md"
echo "═══════════════════════════════════════════════════════════════"
echo ""
