# OfX Telemetry Demo with NATS

This demo showcases OfX distributed tracing and metrics capabilities using NATS as the transport layer.

## Prerequisites

1. **NATS Server** - Install and run NATS:
   ```bash
   # Using Docker
   docker run -d --name nats -p 4222:4222 -p 8222:8222 nats:latest
   
   # Or using Homebrew (macOS)
   brew install nats-server
   nats-server
   ```

2. **PostgreSQL** - Ensure PostgreSQL is running on localhost:5432

3. **.NET 9.0 SDK** - Required to build and run the services

## Architecture

```
┌─────────────┐         NATS          ┌─────────────┐
│  Service1   │ ───── Request ──────► │  Service2   │
│  (Client)   │                       │  (Server)   │
│  GraphQL    │ ◄──── Response ────── │  Worker     │
└─────────────┘                       └─────────────┘
       │                                      │
       │          NATS                        │
       └──────── Request ────────────────────┘
                                       ┌─────────────┐
                                       │  Service3   │
                                       │  (Server)   │
                                       └─────────────┘
```

## Features Demonstrated

### 1. Distributed Tracing (OpenTelemetry)
- **W3C TraceContext Propagation**: Traces propagate across NATS messages via `traceparent` and `tracestate` headers
- **Activity Hierarchy**: Client → NATS → Server activity chain
- **Span Attributes**: 
  - OfX-specific: `ofx.attribute`, `ofx.transport`, `ofx.expression`, `ofx.selector_count`, `ofx.item_count`
  - Messaging: `messaging.system`, `messaging.destination`, `messaging.operation`
  - Database: `db.system`, `db.name` (when querying data)

### 2. Metrics (OpenTelemetry)
- **Counters**: `ofx.request.count`, `ofx.request.errors`, `ofx.items.returned`
- **Histograms**: `ofx.request.duration`, `ofx.items.per_request`
- **Gauges**: `ofx.request.active` (current in-flight requests)
- **Labels**: `ofx.attribute`, `ofx.transport`, `ofx.status`, `ofx.error_type`

### 3. Diagnostic Events
- `ofx.request.start` - Request initiated
- `ofx.request.stop` - Request completed successfully
- `ofx.request.error` - Request failed
- `ofx.message.receive` - Message received by server

## Running the Demo

### Option 1: Using Docker Compose (Recommended)

This option starts the complete observability stack including NATS, Jaeger, Prometheus, Grafana, PostgreSQL, and MongoDB.

```bash
cd sample
docker-compose up -d
```

The infrastructure will be available at:
- **NATS**: `nats://localhost:4222` (Client), `http://localhost:8222` (Monitoring)
- **Jaeger UI**: `http://localhost:16686`
- **Prometheus**: `http://localhost:9090`
- **Grafana**: `http://localhost:3000` (admin/admin)
- **PostgreSQL**: `localhost:5432` (postgres/Abcd@2021)
- **MongoDB**: `localhost:27017`

After infrastructure is running, start the services:

**Terminal 1 - Service1 (GraphQL API)**
```bash
cd sample/Service1
dotnet run
```
Service1 runs on https://localhost:5001

**Terminal 2 - Service2 (Worker)**
```bash
cd sample/Service2
dotnet run
```

**Terminal 3 - Service3 (Worker)**
```bash
cd sample/Service3
dotnet run
```

**To view traces and metrics:**
1. Execute GraphQL query (see Step 3 below)
2. Open Jaeger UI at http://localhost:16686
   - Select service "Service1"
   - Click "Find Traces" to see distributed traces
3. Open Grafana at http://localhost:3000 (login: admin/admin)
   - Navigate to "OfX Framework Overview" dashboard
   - View real-time metrics

**To stop infrastructure:**
```bash
docker-compose down
```

### Option 2: Manual Setup

#### Step 1: Start NATS Server
```bash
docker run -d --name nats -p 4222:4222 -p 8222:8222 nats:latest
```

#### Step 2: Start Services

Open 3 terminal windows:

**Terminal 1 - Service1 (GraphQL API)**
```bash
cd sample/Service1
dotnet run
```
Service1 runs on https://localhost:5001

**Terminal 2 - Service2 (Worker)**
```bash
cd sample/Service2
dotnet run
```

**Terminal 3 - Service3 (Worker)**
```bash
cd sample/Service3
dotnet run
```

### Step 3: Make GraphQL Request

Navigate to https://localhost:5001/graphql and execute:

```graphql
query {
  members {
    id
    name
    addresses {
      id
      provinceId
      province {
        id
        name
        country {
          id
          name
        }
      }
    }
    additionalData {
      id
      name
    }
  }
}
```

## Observing Telemetry

### Console Output

All three services will output telemetry to console:

**Traces Example:**
```
Activity.TraceId:            00-abc123-def456-01
Activity.SpanId:             789ghi
Activity.TraceFlags:         Recorded
Activity.ParentSpanId:       jkl012
Activity.ActivitySourceName: OfX
Activity.DisplayName:        ofx.request
Activity.Kind:               Client
Activity.StartTime:          2026-01-24T10:30:00.1234567Z
Activity.Duration:           00:00:00.1234567
Activity.Tags:
    ofx.attribute: MemberAttribute
    ofx.transport: nats
    ofx.version: 8.2.5
    ofx.expression: {Id, Name, Addresses}
    ofx.selector_count: 3
    ofx.item_count: 3
    messaging.system: nats
    messaging.destination: OfX.Members
    messaging.operation: publish
Status: Ok
```

**Metrics Example:**
```
Export ofx.request.count, Meter: OfX/8.2.5
(2026-01-24T10:30:00.1234567Z, 2026-01-24T10:30:10.1234567Z] 
ofx.attribute: MemberAttribute
ofx.transport: nats
ofx.status: success
LongSum
Value: 5

Export ofx.request.duration, Meter: OfX/8.2.5
(2026-01-24T10:30:00.1234567Z, 2026-01-24T10:30:10.1234567Z]
ofx.attribute: MemberAttribute
ofx.transport: nats
ofx.status: success
Histogram
Sum: 523.45
Count: 5
Buckets: (10,1), (50,2), (100,2), (250,0), (500,0), (1000,0)
```

### NATS Monitoring

Check NATS stats:
```bash
curl http://localhost:8222/varz
```

## Understanding the Flow

1. **Service1** receives GraphQL query
2. **Service1** creates client-side Activity and starts NATS request
   - Trace context added to NATS headers
   - Metrics: `UpdateActiveRequests(+1)`
   - Diagnostic: `RequestStart` event
3. **Service2/3** receives NATS message
   - Extracts parent trace context from headers
   - Creates server-side Activity with parent
   - Diagnostic: `MessageReceive` event
4. **Service2/3** processes request (queries database)
5. **Service2/3** sends response back
   - Metrics: `RecordRequest(duration, itemCount)`
   - Activity status: `Ok`
6. **Service1** receives response
   - Metrics: `RecordRequest(duration, itemCount)`, `UpdateActiveRequests(-1)`
   - Diagnostic: `RequestStop` event
   - Activity disposed (trace complete)

## Trace Propagation Example

```
Trace ID: 00-abc123def456ghi789-jkl012mno345-01

Service1 (Client Activity)
  SpanId: 111111
  ParentSpanId: (none)
  Tags: ofx.attribute=MemberAttribute, messaging.operation=publish
    │
    ├─► NATS Message Headers:
    │   traceparent: 00-abc123def456ghi789-111111-01
    │   tracestate: (optional)
    │
    └─► Service2 (Server Activity)
          SpanId: 222222
          ParentSpanId: 111111
          Tags: ofx.attribute=MemberAttribute, messaging.operation=process
            │
            └─► Database Query (Internal Activity)
                  SpanId: 333333
                  ParentSpanId: 222222
                  Tags: db.system=postgresql
```

## Observability Backends

### Jaeger (Distributed Tracing)

If using **docker-compose**, Jaeger is already running. If using manual setup:

1. Start Jaeger:
   ```bash
   docker run -d --name jaeger \
     -e COLLECTOR_OTLP_ENABLED=true \
     -p 16686:16686 \
     -p 4317:4317 \
     -p 4318:4318 \
     jaegertracing/all-in-one:latest
   ```

2. Uncomment OTLP exporter in each service's Program.cs:
   ```csharp
   .AddOtlpExporter(options =>
   {
       options.Endpoint = new Uri("http://localhost:4317");
   })
   ```

3. Open Jaeger UI: http://localhost:16686
4. Select service "Service1" and click "Find Traces"
5. View trace waterfall showing request propagation:
   - Service1 client span
   - NATS messaging
   - Service2/Service3 server spans
   - Database queries (if instrumented)

### Prometheus + Grafana (Metrics)

If using **docker-compose**, Prometheus and Grafana are already configured with pre-built dashboards.

**Prometheus** (http://localhost:9090):
- Scrapes metrics from services automatically
- Query examples:
  - `rate(ofx_request_count_total[1m])` - Request rate per second
  - `histogram_quantile(0.95, rate(ofx_request_duration_milliseconds_bucket[1m]))` - P95 latency
  - `ofx_request_active` - Active in-flight requests

**Grafana** (http://localhost:3000, admin/admin):
- Pre-configured datasources (Prometheus, Jaeger)
- Pre-built dashboard: "OfX Framework Overview"
  - Request rate by attribute and transport
  - Request duration (P50, P95)
  - Active requests gauge
  - Error rate
  - Items returned rate

#### Manual Prometheus + Grafana Setup

1. Add Prometheus exporter to each service:
   ```bash
   dotnet add package OpenTelemetry.Exporter.Prometheus.AspNetCore
   ```

2. Configure in Program.cs:
   ```csharp
   .WithMetrics(metrics => metrics
       .AddMeter("OfX")
       .AddPrometheusExporter())

   app.MapPrometheusScrapingEndpoint();  // Exposes /metrics
   ```

3. Start Prometheus with sample/prometheus.yml configuration
4. Start Grafana and add Prometheus datasource
5. Import dashboard from sample/grafana/provisioning/dashboards/json/ofx-overview.json

## Key Takeaways

- ✅ **Zero-allocation when disabled**: If no OpenTelemetry listener is configured, activities return null immediately
- ✅ **W3C Standard**: Uses industry-standard trace context propagation
- ✅ **MassTransit-style naming**: Consistent `ofx.*` prefix for all metrics and tags
- ✅ **Low overhead**: Metrics are lazy-initialized, activities are lightweight
- ✅ **Production-ready**: Supports sampling, batching, and various exporters (OTLP, Jaeger, Prometheus, etc.)

## Troubleshooting

### No traces appearing
- Ensure `.AddSource("OfX")` is present in tracing configuration
- Check that NATS server is running and accessible

### High overhead
- Enable sampling: `.SetSampler(new TraceIdRatioBasedSampler(0.1))` (10% sampling)
- Reduce tag cardinality (avoid high-cardinality IDs in tags)

### Missing child spans
- Ensure `Activity.Current` propagates in async code
- Use `await` instead of `Task.Run(() => ...)` to preserve context

## Next Steps

- Review [telemetry.md](../docs/telemetry.md) for comprehensive documentation
- Integrate with your preferred observability backend (Jaeger, Zipkin, Grafana, Application Insights)
- Add custom instrumentation using `OfXActivitySource.StartInternalActivity()`
- Configure production-grade OpenTelemetry Collector for batching and sampling
