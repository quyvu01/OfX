# OfX Observability Stack Setup Guide

This guide walks you through setting up the complete observability stack for OfX framework using Docker Compose.

## What's Included

The `docker-compose.yml` provides a complete observability infrastructure:

- **NATS** - Message broker for inter-service communication
- **Jaeger** - Distributed tracing backend with UI
- **Prometheus** - Metrics collection and storage
- **Grafana** - Visualization and dashboards
- **PostgreSQL** - Relational database for services (with pre-created databases)
- **MongoDB** - NoSQL database for Service1

## Quick Start

```bash
# Start all infrastructure
cd sample
docker-compose up -d

# Check status
docker-compose ps

# View logs
docker-compose logs -f

# Stop all
docker-compose down

# Stop and remove volumes (clean slate)
docker-compose down -v
```

## Services and Ports

| Service    | Port(s)                  | URL/Access                          |
|------------|--------------------------|-------------------------------------|
| NATS       | 4222 (client), 8222 (http) | `nats://localhost:4222`            |
| Jaeger     | 16686 (UI), 4317 (OTLP)  | http://localhost:16686              |
| Prometheus | 9090                     | http://localhost:9090               |
| Grafana    | 3000                     | http://localhost:3000 (admin/admin) |
| PostgreSQL | 5432                     | `localhost:5432` (postgres/Abcd@2021) |
| MongoDB    | 27017                    | `localhost:27017`                   |

## Infrastructure Details

### NATS Message Broker

**Purpose**: Handles request-reply messaging between services with distributed tracing

**Monitoring**: http://localhost:8222/varz
```bash
# Check NATS stats
curl http://localhost:8222/varz | jq
```

**Configuration**: Default configuration, no authentication

### Jaeger Tracing

**Purpose**: Collects, stores, and visualizes distributed traces from all services

**UI**: http://localhost:16686

**How to use**:
1. Select service from dropdown (e.g., "Service1")
2. Click "Find Traces"
3. View trace waterfall showing:
   - Request flow across services
   - Timing of each operation
   - Parent-child span relationships
   - Tags and logs

**OTLP Endpoint**: `http://localhost:4317` (gRPC)

**Storage**: In-memory (traces cleared on restart)

### Prometheus Metrics

**Purpose**: Scrapes and stores time-series metrics from services

**UI**: http://localhost:9090

**Configuration**: See [prometheus.yml](./prometheus.yml)

**Scrape targets**:
- Service1: `host.docker.internal:5001/metrics`
- Service2: `host.docker.internal:5002/metrics`
- Service3: `host.docker.internal:5003/metrics`
- NATS: `nats:8222/metrics`

**Example queries**:
```promql
# Request rate per service
rate(ofx_request_count_total[1m])

# P95 request duration
histogram_quantile(0.95, rate(ofx_request_duration_milliseconds_bucket[1m]))

# Active requests
ofx_request_active

# Error rate
rate(ofx_request_errors_total[1m])

# Items returned per second
rate(ofx_items_returned_total[1m])
```

**Storage**: Persistent via Docker volume `prometheus_data`

### Grafana Dashboards

**Purpose**: Visualizes metrics from Prometheus and traces from Jaeger

**UI**: http://localhost:3000

**Login**: admin / admin (you'll be prompted to change password)

**Pre-configured**:
- ✅ Prometheus datasource (default)
- ✅ Jaeger datasource
- ✅ "OfX Framework Overview" dashboard

**Dashboard panels**:
1. Request Rate - Requests per second by attribute and transport
2. Request Duration - P50 and P95 latency percentiles
3. Active Requests - Current in-flight requests (gauge)
4. Error Rate - Errors per second by type
5. Items Returned Rate - Data throughput

**Provisioning**: Auto-configured from `grafana/provisioning/`

**Storage**: Persistent via Docker volume `grafana_data`

### PostgreSQL Database

**Purpose**: Provides relational storage for all three services

**Connection**: `Host=localhost;Port=5432;Username=postgres;Password=Abcd@2021`

**Pre-created databases**:
- `OfXTestService1` - Service1 main context
- `OfXTestOtherService1` - Service1 secondary context
- `OfXTestService2` - Service2 context
- `OfXTestService3` - Service3 context

**Initialization**: Databases are created automatically via `postgres-init/init-databases.sh`

**Storage**: Persistent via Docker volume `postgres_data`

**Management**:
```bash
# Connect to PostgreSQL
docker-compose exec postgres psql -U postgres

# List databases
\l

# Connect to specific database
\c OfXTestService1

# List tables
\dt
```

### MongoDB Database

**Purpose**: Provides NoSQL storage for Service1 (MemberSocial collection)

**Connection**: `mongodb://localhost:27017`

**Database**: `Service1MongoDb`

**Collections**: Auto-created by Service1

**Storage**: Persistent via Docker volume `mongo_data`

**Management**:
```bash
# Connect to MongoDB
docker-compose exec mongodb mongosh

# Show databases
show dbs

# Use database
use Service1MongoDb

# Show collections
show collections

# Query documents
db.MemberSocials.find().pretty()
```

## Configuration Files

### prometheus.yml

Defines scrape targets and intervals for Prometheus:
- Global scrape interval: 15s
- Labels: service name, role, environment
- Targets: Service1, Service2, Service3, NATS, Prometheus itself

### grafana/provisioning/datasources/datasources.yml

Auto-configures Grafana datasources:
- Prometheus (default datasource)
- Jaeger (for trace correlation)

### grafana/provisioning/dashboards/dashboards.yml

Enables dashboard auto-provisioning from JSON files

### grafana/provisioning/dashboards/json/ofx-overview.json

Pre-built dashboard showing:
- Request rate (line chart)
- Request duration histograms (P50, P95)
- Active requests (gauge)
- Error rate (line chart)
- Items returned (line chart)

### postgres-init/init-databases.sh

PostgreSQL initialization script that creates all required databases

## Verifying the Stack

### 1. Check all containers are running
```bash
docker-compose ps
```
Expected output: All services with status "Up"

### 2. Test NATS connectivity
```bash
curl http://localhost:8222/varz
```
Expected: JSON response with NATS server stats

### 3. Access Jaeger UI
Open http://localhost:16686 in browser
Expected: Jaeger UI loads with service dropdown

### 4. Access Prometheus UI
Open http://localhost:9090 in browser
Query: `up`
Expected: Shows all scrape targets and their status

### 5. Access Grafana
Open http://localhost:3000 (admin/admin)
Navigate to: Dashboards → OfX → OfX Framework Overview
Expected: Dashboard loads (no data yet until services run)

### 6. Test PostgreSQL connection
```bash
docker-compose exec postgres psql -U postgres -c "\l"
```
Expected: List showing OfXTestService1, OfXTestService2, OfXTestService3, OfXTestOtherService1

### 7. Test MongoDB connection
```bash
docker-compose exec mongodb mongosh --eval "db.adminCommand('ping')"
```
Expected: `{ ok: 1 }`

## Running the Demo

After infrastructure is up:

1. **Start Service1**:
   ```bash
   cd sample/Service1
   dotnet run
   ```
   Wait for: "Now listening on: https://localhost:5001"

2. **Start Service2**:
   ```bash
   cd sample/Service2
   dotnet run
   ```

3. **Start Service3**:
   ```bash
   cd sample/Service3
   dotnet run
   ```

4. **Execute GraphQL query**:
   Open https://localhost:5001/graphql
   Run query from [README-TELEMETRY-DEMO.md](./README-TELEMETRY-DEMO.md)

5. **View traces in Jaeger**:
   - Open http://localhost:16686
   - Service dropdown → "Service1"
   - Click "Find Traces"
   - Click on a trace to see detailed waterfall

6. **View metrics in Grafana**:
   - Open http://localhost:3000
   - Navigate to "OfX Framework Overview" dashboard
   - Observe real-time metrics updating

## Troubleshooting

### Containers won't start
```bash
# Check logs
docker-compose logs [service-name]

# Restart specific service
docker-compose restart [service-name]

# Full restart
docker-compose down && docker-compose up -d
```

### Port conflicts
If ports are already in use, edit `docker-compose.yml` to change port mappings:
```yaml
ports:
  - "NEW_PORT:CONTAINER_PORT"
```

### Services can't connect to infrastructure
- **Windows/Mac**: Use `host.docker.internal` instead of `localhost` in connection strings
- **Linux**: Use `172.17.0.1` (Docker bridge IP) or add `--network host`

### Prometheus not scraping services
1. Check Prometheus targets: http://localhost:9090/targets
2. Ensure services expose `/metrics` endpoint
3. Verify services are running on expected ports
4. Check `prometheus.yml` configuration

### Grafana dashboard shows "No data"
1. Ensure Prometheus is scraping successfully
2. Check time range in Grafana (top right)
3. Verify services are generating metrics (check Prometheus)
4. Refresh dashboard

### Jaeger shows no traces
1. Ensure services have OTLP exporter configured:
   ```csharp
   .AddOtlpExporter(options => {
       options.Endpoint = new Uri("http://localhost:4317");
   })
   ```
2. Check Jaeger logs: `docker-compose logs jaeger`
3. Verify services are making requests

### PostgreSQL connection refused
1. Wait for PostgreSQL to fully start (can take 10-20 seconds)
2. Check logs: `docker-compose logs postgres`
3. Verify databases exist: `docker-compose exec postgres psql -U postgres -c "\l"`

### Data persists after `docker-compose down`
This is expected. Volumes are preserved. To clean:
```bash
docker-compose down -v  # Removes volumes
```

## Resource Usage

Expected resource consumption:
- **CPU**: ~2-4% per container (idle), ~10-20% under load
- **Memory**:
  - NATS: ~20 MB
  - Jaeger: ~100-200 MB
  - Prometheus: ~200-500 MB (grows with data)
  - Grafana: ~100-200 MB
  - PostgreSQL: ~100-300 MB
  - MongoDB: ~100-200 MB
- **Disk**:
  - Prometheus data: grows over time (~1 GB per day with default retention)
  - PostgreSQL data: depends on seeded data (~50-100 MB initially)
  - MongoDB data: depends on collections (~10-50 MB initially)

## Production Considerations

This docker-compose setup is for **development and demo purposes only**. For production:

### Jaeger
- Use persistent storage backend (Elasticsearch, Cassandra, Badger)
- Configure sampling strategies
- Set up retention policies
- Enable authentication

### Prometheus
- Increase retention period
- Configure remote storage
- Set up alerting rules
- Enable authentication
- Use service discovery instead of static targets

### Grafana
- Use external database (PostgreSQL, MySQL)
- Configure SMTP for alerts
- Set up authentication (OAuth, LDAP)
- Enable HTTPS
- Configure backup

### PostgreSQL
- Tune configuration for production workload
- Set up replication
- Configure backup and recovery
- Enable SSL connections
- Implement connection pooling (PgBouncer)

### MongoDB
- Enable authentication
- Configure replica set
- Set up regular backups
- Enable SSL/TLS

### NATS
- Enable authentication
- Configure clustering (for HA)
- Set up JetStream for persistence
- Configure TLS

## Next Steps

1. Explore the [README-TELEMETRY-DEMO.md](./README-TELEMETRY-DEMO.md) for detailed telemetry walkthrough
2. Review [../docs/telemetry.md](../docs/telemetry.md) for implementation details
3. Customize Grafana dashboards for your specific needs
4. Set up alerting rules in Prometheus
5. Configure Grafana notification channels
6. Experiment with sampling strategies in OpenTelemetry

## Useful Commands

```bash
# View all logs
docker-compose logs -f

# View specific service logs
docker-compose logs -f jaeger

# Restart all services
docker-compose restart

# Stop all services
docker-compose stop

# Remove all services and networks (keep volumes)
docker-compose down

# Remove everything including volumes (clean slate)
docker-compose down -v

# Rebuild and restart
docker-compose up -d --build

# Check resource usage
docker stats

# Execute command in container
docker-compose exec postgres psql -U postgres

# View container details
docker-compose ps
docker inspect sample_jaeger_1
```
