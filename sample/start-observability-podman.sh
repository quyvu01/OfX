#!/bin/bash

# OfX Telemetry Demo - Podman Quick Start Script
# For users who already have NATS, PostgreSQL, and MongoDB running locally
# This script only starts the observability stack (Jaeger, Prometheus, Grafana)

set -e

echo "═══════════════════════════════════════════════════════════════"
echo "  OfX Framework - Observability Stack (Podman)"
echo "═══════════════════════════════════════════════════════════════"
echo ""

# Check if podman is available
if ! command -v podman &> /dev/null; then
    echo "❌ Error: Podman is not installed or not in PATH"
    echo "   Please install Podman: https://podman.io/getting-started/installation"
    exit 1
fi

# Check if podman-compose is available
if command -v podman-compose &> /dev/null; then
    COMPOSE_CMD="podman-compose"
elif command -v docker-compose &> /dev/null; then
    # docker-compose works with podman when DOCKER_HOST is set
    export DOCKER_HOST=unix://$(podman info --format '{{.Host.RemoteSocket.Path}}')
    COMPOSE_CMD="docker-compose"
else
    echo "❌ Error: Neither podman-compose nor docker-compose is available"
    echo "   Please install one of them:"
    echo "   - podman-compose: pip install podman-compose"
    echo "   - docker-compose: https://docs.docker.com/compose/install/"
    exit 1
fi

# Check if dotnet is available
if ! command -v dotnet &> /dev/null; then
    echo "❌ Error: .NET SDK is not installed or not in PATH"
    echo "   Please install .NET 9.0 SDK: https://dotnet.microsoft.com/download"
    exit 1
fi

echo "✅ Prerequisites check passed"
echo "   Using: $COMPOSE_CMD"
echo ""

# Verify local infrastructure
echo "🔍 Checking local infrastructure..."
echo ""

# Check NATS
if nc -z localhost 4222 2>/dev/null; then
    echo "✅ NATS is running on localhost:4222"
else
    echo "⚠️  Warning: NATS doesn't appear to be running on localhost:4222"
    echo "   Services will fail to connect without NATS"
fi

# Check PostgreSQL
if nc -z localhost 5432 2>/dev/null; then
    echo "✅ PostgreSQL is running on localhost:5432"
else
    echo "⚠️  Warning: PostgreSQL doesn't appear to be running on localhost:5432"
    echo "   Services will fail to connect without PostgreSQL"
fi

# Check MongoDB
if nc -z localhost 27017 2>/dev/null; then
    echo "✅ MongoDB is running on localhost:27017"
else
    echo "⚠️  Warning: MongoDB doesn't appear to be running on localhost:27017"
    echo "   Service1 will fail to connect without MongoDB"
fi

echo ""

# Start observability stack
echo "🚀 Starting observability stack (Jaeger, Prometheus, Grafana)..."
echo ""

$COMPOSE_CMD -f docker-compose-observability.yml up -d

# Wait for services to be ready
echo ""
echo "⏳ Waiting for services to be ready..."
sleep 5

# Check if all containers are running
RUNNING=$($COMPOSE_CMD -f docker-compose-observability.yml ps --services --filter "status=running" 2>/dev/null | wc -l | tr -d ' ')
TOTAL=$($COMPOSE_CMD -f docker-compose-observability.yml ps --services 2>/dev/null | wc -l | tr -d ' ')

if [ "$RUNNING" -eq "$TOTAL" ]; then
    echo "✅ All observability services are running ($RUNNING/$TOTAL)"
else
    echo "⚠️  Warning: Some services may not be ready yet ($RUNNING/$TOTAL running)"
    echo "   You can check status with: $COMPOSE_CMD -f docker-compose-observability.yml ps"
fi

echo ""
echo "═══════════════════════════════════════════════════════════════"
echo "  Observability Stack is ready!"
echo "═══════════════════════════════════════════════════════════════"
echo ""
echo "📊 Observability URLs:"
echo ""
echo "  Jaeger (Distributed Tracing):"
echo "    🔗 http://localhost:16686"
echo "    📝 View traces, service dependencies, and performance"
echo ""
echo "  Prometheus (Metrics):"
echo "    🔗 http://localhost:9090"
echo "    📝 Query metrics, create alerts"
echo ""
echo "  Grafana (Dashboards):"
echo "    🔗 http://localhost:3000"
echo "    👤 Username: admin"
echo "    🔑 Password: admin"
echo "    📝 Pre-built dashboard: 'OfX Framework Overview'"
echo ""
echo "💾 Your Local Infrastructure (already running):"
echo ""
echo "  NATS:"
echo "    🔗 nats://localhost:4222"
echo "    📊 Monitoring: http://localhost:8222/varz"
echo ""
echo "  PostgreSQL:"
echo "    🔗 localhost:5432"
echo "    👤 Make sure your local PostgreSQL has the required databases:"
echo "       - OfXTestService1"
echo "       - OfXTestOtherService1"
echo "       - OfXTestService2"
echo "       - OfXTestService3"
echo ""
echo "  MongoDB:"
echo "    🔗 mongodb://localhost:27017"
echo "    📝 Service1MongoDb database will be created automatically"
echo ""
echo "═══════════════════════════════════════════════════════════════"
echo "  Next Steps"
echo "═══════════════════════════════════════════════════════════════"
echo ""
echo "1️⃣  Ensure your local databases have the required schemas:"
echo ""
echo "   PostgreSQL - Create databases if needed:"
echo "   $ podman exec -it <your-postgres-container> psql -U postgres"
echo "   > CREATE DATABASE \"OfXTestService1\";"
echo "   > CREATE DATABASE \"OfXTestOtherService1\";"
echo "   > CREATE DATABASE \"OfXTestService2\";"
echo "   > CREATE DATABASE \"OfXTestService3\";"
echo ""
echo "2️⃣  Start the OfX services in separate terminals:"
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
echo "3️⃣  Make a GraphQL request:"
echo "   🔗 Open: https://localhost:5001/graphql"
echo "   📝 Execute a query (see README-TELEMETRY-DEMO.md)"
echo ""
echo "4️⃣  View distributed traces:"
echo "   🔗 Open: http://localhost:16686"
echo "   📝 Select 'Service1' and click 'Find Traces'"
echo "   ✨ See the complete request flow across services!"
echo ""
echo "5️⃣  View metrics dashboard:"
echo "   🔗 Open: http://localhost:3000"
echo "   📝 Navigate to: Dashboards → OfX → OfX Framework Overview"
echo "   📊 See real-time metrics: request rate, latency, errors"
echo ""
echo "═══════════════════════════════════════════════════════════════"
echo "  Helpful Commands"
echo "═══════════════════════════════════════════════════════════════"
echo ""
echo "  View observability stack logs:"
echo "  $ $COMPOSE_CMD -f docker-compose-observability.yml logs -f"
echo ""
echo "  Check status:"
echo "  $ $COMPOSE_CMD -f docker-compose-observability.yml ps"
echo ""
echo "  Stop observability stack:"
echo "  $ $COMPOSE_CMD -f docker-compose-observability.yml down"
echo ""
echo "  Stop and clean volumes:"
echo "  $ $COMPOSE_CMD -f docker-compose-observability.yml down -v"
echo ""
echo "  Restart a specific service:"
echo "  $ $COMPOSE_CMD -f docker-compose-observability.yml restart [jaeger|prometheus|grafana]"
echo ""
echo "═══════════════════════════════════════════════════════════════"
echo "  Documentation"
echo "═══════════════════════════════════════════════════════════════"
echo ""
echo "  📖 README-TELEMETRY-DEMO.md     - Complete demo walkthrough"
echo "  📖 SETUP-OBSERVABILITY.md       - Detailed infrastructure guide"
echo "  📖 ../docs/telemetry.md         - Implementation documentation"
echo ""
echo "═══════════════════════════════════════════════════════════════"
echo ""
