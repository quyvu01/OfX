# OfX Framework - Sample Applications

This directory contains sample applications demonstrating the OfX framework's capabilities, with a focus on distributed tracing and observability.

## Sample Services

- **Service1** - GraphQL API service using HotChocolate, Entity Framework Core, and MongoDB
- **Service2** - Worker service demonstrating EF Core integration
- **Service3** - Worker service demonstrating EF Core integration
- **Shared** - Common models and utilities shared across services

## Quick Start Guides

Choose the guide that matches your setup:

### 🐋 Docker Users (Full Stack)
If you want to start everything from scratch using Docker:

📖 **[README-TELEMETRY-DEMO.md](./README-TELEMETRY-DEMO.md)**
- Complete demo walkthrough
- Starts all infrastructure (NATS, Jaeger, Prometheus, Grafana, PostgreSQL, MongoDB)
- Works with Docker or Docker Desktop
- Best for: First-time users, clean environment

```bash
cd sample
docker-compose up -d
```

---

### 🦭 Podman Users (MacBook ARM / Existing Infrastructure)
If you're using Podman on Apple Silicon with existing NATS, PostgreSQL, and MongoDB:

📖 **[README-PODMAN-SETUP.md](./README-PODMAN-SETUP.md)**
- Podman-specific instructions
- ARM64/Apple Silicon optimized
- Only starts observability stack (Jaeger, Prometheus, Grafana)
- Uses your existing databases
- Best for: MacBook M1/M2/M3 users, existing infrastructure

```bash
cd sample
./start-observability-podman.sh
```

---

### ⚙️ Infrastructure Setup Details
For detailed information about the observability stack:

📖 **[SETUP-OBSERVABILITY.md](./SETUP-OBSERVABILITY.md)**
- Complete infrastructure documentation
- Configuration files explained
- Troubleshooting guide
- Production considerations
- Resource requirements

---

## What's Included

### Observability Features

The samples demonstrate OfX's built-in telemetry capabilities:

**Distributed Tracing (OpenTelemetry)**
- W3C TraceContext propagation across NATS messages
- Client → Server activity hierarchy
- Span attributes: `ofx.attribute`, `ofx.transport`, `messaging.system`, etc.
- View traces in Jaeger UI

**Metrics (OpenTelemetry)**
- Counters: request count, error count, items returned
- Histograms: request duration, items per request
- Gauges: active requests
- View metrics in Prometheus/Grafana

**Diagnostic Events**
- `ofx.request.start`, `ofx.request.stop`, `ofx.request.error`
- `ofx.message.receive`
- Console output for debugging

### Architecture

```
┌──────────────┐         NATS          ┌──────────────┐
│   Service1   │ ───── Request ──────► │   Service2   │
│   GraphQL    │                       │   Worker     │
│ (Port 5001)  │ ◄──── Response ────── │              │
└──────────────┘                       └──────────────┘
       │                                       │
       │          NATS                         │
       └──────── Request ─────────────────────┘
                                        ┌──────────────┐
                                        │   Service3   │
                                        │   Worker     │
                                        └──────────────┘

         ┌───────────────────────────────────┐
         │     Observability Stack           │
         │  ┌──────────┐  ┌──────────┐      │
         │  │  Jaeger  │  │Prometheus│      │
         │  │  :16686  │  │  :9090   │      │
         │  └──────────┘  └──────────┘      │
         │         ┌──────────┐              │
         │         │ Grafana  │              │
         │         │  :3000   │              │
         │         └──────────┘              │
         └───────────────────────────────────┘
```

## Files and Configuration

### Docker Compose Files

- **`docker-compose.yml`** - Full stack (all services including databases)
- **`docker-compose-observability.yml`** - Observability only (Jaeger, Prometheus, Grafana)
  - ARM64/Apple Silicon compatible
  - For users with existing infrastructure

### Configuration Files

- **`prometheus.yml`** - Prometheus scrape configuration
  - Scrapes Service1, Service2, Service3
  - 15-second scrape interval

- **`grafana/provisioning/`** - Grafana auto-configuration
  - `datasources/datasources.yml` - Prometheus and Jaeger datasources
  - `dashboards/dashboards.yml` - Dashboard provisioning
  - `dashboards/json/ofx-overview.json` - Pre-built OfX metrics dashboard

- **`postgres-init/init-databases.sh`** - PostgreSQL database initialization
  - Creates: OfXTestService1, OfXTestOtherService1, OfXTestService2, OfXTestService3

### Scripts

- **`start-demo.sh`** - Quick start for Docker users
- **`start-observability-podman.sh`** - Quick start for Podman users
  - Checks prerequisites
  - Verifies local infrastructure
  - Starts observability stack
  - Provides helpful next steps

## Running the Samples

### Prerequisites

- **.NET 9.0 SDK** - https://dotnet.microsoft.com/download
- **Docker/Podman** - For running infrastructure
- **NATS** - Message broker (can use Docker or local)
- **PostgreSQL** - Relational database (can use Docker or local)
- **MongoDB** - NoSQL database for Service1 (can use Docker or local)

### Step-by-Step

1. **Start Infrastructure**
   ```bash
   # Docker users
   docker-compose up -d

   # Podman users with existing infrastructure
   ./start-observability-podman.sh
   ```

2. **Start Services** (in 3 separate terminals)
   ```bash
   # Terminal 1
   cd Service1 && dotnet run

   # Terminal 2
   cd Service2 && dotnet run

   # Terminal 3
   cd Service3 && dotnet run
   ```

3. **Make Request**
   - Open https://localhost:5001/graphql
   - Execute GraphQL query (see README-TELEMETRY-DEMO.md)

4. **View Telemetry**
   - Traces: http://localhost:16686 (Jaeger)
   - Metrics: http://localhost:9090 (Prometheus)
   - Dashboards: http://localhost:3000 (Grafana - admin/admin)

## Key Concepts Demonstrated

### OfX Framework Features

✅ **Attribute-based Messaging**
- `[MemberAttribute]` on models
- Automatic NATS subject routing
- Type-safe request/response

✅ **Entity Framework Core Integration**
- Multiple DbContexts (Service1Context, OtherService1Context)
- Model configurations
- Async seeding

✅ **MongoDB Integration** (Service1)
- Collection registration
- Document models

✅ **HotChocolate GraphQL** (Service1)
- Query type integration
- OfX resolver pattern

✅ **Supervision & Circuit Breaker**
- OneForOne strategy
- Max restarts: 5
- Circuit breaker threshold: 3

✅ **Telemetry & Observability**
- OpenTelemetry integration
- Distributed tracing
- Metrics collection
- Diagnostic events

### OpenTelemetry Integration

✅ **Activity/Span Creation**
- Client-side: `OfXActivitySource.StartClientActivity<TAttribute>()`
- Server-side: `OfXActivitySource.StartServerActivity()`

✅ **Trace Context Propagation**
- W3C TraceContext standard
- `traceparent` and `tracestate` headers
- Parent-child span relationships

✅ **Metrics**
- Meter: `OfX`
- Counter, Histogram, ObservableGauge
- Labels: `ofx.attribute`, `ofx.transport`, `ofx.status`

✅ **Semantic Conventions**
- Messaging tags: `messaging.system`, `messaging.destination`
- Database tags: `db.system`, `db.name`
- OfX tags: `ofx.attribute`, `ofx.expression`, `ofx.item_count`

## Service Details

### Service1 (GraphQL API)
- **Port**: 5001 (HTTPS), 5000 (HTTP)
- **Endpoint**: `/graphql`
- **Features**:
  - GraphQL API with HotChocolate
  - Multiple data sources (EF Core + MongoDB)
  - Cross-service queries via NATS
  - OpenTelemetry tracing and metrics

### Service2 (Worker)
- **Role**: NATS message consumer
- **Features**:
  - Entity Framework Core
  - User entity management
  - Province relationship
  - Distributed tracing

### Service3 (Worker)
- **Role**: NATS message consumer
- **Features**:
  - Entity Framework Core
  - Country and Province entities
  - Hierarchical data
  - Distributed tracing

## Troubleshooting

### Build Issues

```bash
# Clean and rebuild
dotnet clean
dotnet build --no-incremental
```

### Connection Issues

**NATS not connecting**
```bash
# Check NATS is running
curl http://localhost:8222/varz
```

**PostgreSQL not connecting**
```bash
# Test connection
psql -h localhost -U postgres -c "SELECT version();"

# List databases
psql -h localhost -U postgres -c "\l"
```

**MongoDB not connecting**
```bash
# Test connection (if using Docker)
docker exec -it ofx-mongodb mongosh --eval "db.adminCommand('ping')"
```

### Telemetry Not Appearing

1. Ensure `.AddSource("OfX")` in tracing config
2. Ensure `.AddMeter("OfX")` in metrics config
3. Check OTLP exporter endpoint: `http://localhost:4317`
4. View service console output for Activity and Metric logs

## Next Steps

1. ✅ Run the demo following one of the guides above
2. 📖 Review [../docs/telemetry.md](../docs/telemetry.md) for implementation details
3. 🔍 Explore traces in Jaeger to understand request flow
4. 📊 Create custom Grafana dashboards
5. 🚨 Set up Prometheus alerting rules
6. 🔧 Customize the samples for your use case

## Documentation

- 📖 [README-TELEMETRY-DEMO.md](./README-TELEMETRY-DEMO.md) - Complete demo walkthrough
- 📖 [README-PODMAN-SETUP.md](./README-PODMAN-SETUP.md) - Podman/ARM setup guide
- 📖 [SETUP-OBSERVABILITY.md](./SETUP-OBSERVABILITY.md) - Infrastructure details
- 📖 [../docs/telemetry.md](../docs/telemetry.md) - Telemetry implementation guide

## Learning Resources

- **OpenTelemetry**: https://opentelemetry.io/docs/instrumentation/net/
- **Jaeger**: https://www.jaegertracing.io/docs/
- **Prometheus**: https://prometheus.io/docs/
- **Grafana**: https://grafana.com/docs/
- **NATS**: https://docs.nats.io/
- **HotChocolate**: https://chillicream.com/docs/hotchocolate

---

**Ready to get started?** Choose your setup guide above and follow the instructions!
