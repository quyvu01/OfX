# OfX Telemetry & Observability

OfX framework provides comprehensive observability support through OpenTelemetry-compatible distributed tracing, metrics, and diagnostic events.

## Overview

OfX telemetry consists of three main components:

1. **Distributed Tracing** - Track requests across services
2. **Metrics** - Monitor performance and health
3. **Diagnostic Events** - Custom event streaming

## Quick Start

### 1. Install OpenTelemetry Packages

```bash
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Exporter.Console
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

### 2. Configure Tracing & Metrics

```csharp
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("OfX")  // Subscribe to OfX traces
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://jaeger:4317");
        }))
    .WithMetrics(metrics => metrics
        .AddMeter("OfX")  // Subscribe to OfX metrics
        .AddAspNetCoreInstrumentation()
        .AddPrometheusExporter());

var app = builder.Build();
```

### 3. View Traces

Run with Jaeger:

```bash
docker run -d --name jaeger \
  -p 16686:16686 \
  -p 4317:4317 \
  jaegertracing/all-in-one:latest

# Open http://localhost:16686 in browser
```

---

## Distributed Tracing

### Activity Hierarchy

OfX creates hierarchical activities for request processing:

```
Trace: order-processing
├─ [OfX.Request] Client: OrderAttribute (200ms)
│  ├─ [kafka.send] Kafka Producer (5ms)
│  ├─ [OfX.Process] Server: OrderAttribute (180ms)
│  │  ├─ [OfX.EFCore.Query] Database Query (120ms)
│  │  │  └─ [db.command] SELECT * FROM Orders (118ms)
│  │  └─ [OfX.Request] Nested: UserAttribute (50ms)
│  │     └─ [grpc.call] gRPC GetUser (45ms)
│  └─ [kafka.receive] Kafka Consumer (10ms)
└─ Total: 200ms
```

### Available Tags

#### Client-side Activities

```csharp
Activity: OfX.Request
Tags:
  - ofx.attribute: "OrderAttribute"
  - ofx.transport: "kafka" | "grpc" | "rabbitmq" | "nats" | "azureservicebus"
  - ofx.version: "8.3.0"
  - ofx.expression: "{Id, Name, Items}"
  - ofx.selector_count: 5
  - ofx.selector_ids: "id1,id2,id3,id4,id5"
  - ofx.item_count: 42
```

#### Server-side Activities

```csharp
Activity: OfX.Process
Tags:
  - ofx.attribute: "OrderAttribute"
  - ofx.version: "8.3.0"
  - messaging.system: "kafka"
  - messaging.destination: "orders.topic"
  - messaging.consumer_id: "consumer-1"
```

#### Database Activities

```csharp
Activity: OfX.Database.Query
Tags:
  - db.system: "postgresql" | "mongodb"
  - db.name: "ecommerce"
  - db.statement: "SELECT * FROM orders WHERE id = ANY(@ids)"
```

### Trace Context Propagation

OfX automatically propagates trace context using W3C TraceContext standard:

```
Request Headers:
  traceparent: 00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01
  tracestate: congo=t61rcWkgMzE

OfX automatically:
1. Extracts parent context from incoming messages
2. Creates child activity
3. Propagates context to outgoing messages
```

### Baggage Support

Add correlation data that propagates across services:

```csharp
// Set baggage (propagates automatically)
Baggage.SetBaggage("tenant.id", "tenant-123");
Baggage.SetBaggage("user.id", "user-456");

// Access in downstream services
var tenantId = Baggage.GetBaggage("tenant.id");

// Also added as activity tags for filtering
activity?.SetTag("tenant.id", tenantId);
```

---

## Metrics

### Available Metrics

#### Counters

| Metric | Description | Dimensions |
|--------|-------------|------------|
| `ofx.request.count` | Total requests | attribute, transport, status |
| `ofx.request.errors` | Total errors | attribute, transport, error_type |
| `ofx.items.returned` | Total items returned | attribute, transport |
| `ofx.messages.sent` | Messages sent | transport, destination |
| `ofx.messages.received` | Messages received | transport, source |

#### Histograms

| Metric | Unit | Description | Dimensions |
|--------|------|-------------|------------|
| `ofx.request.duration` | ms | Request duration | attribute, transport, status |
| `ofx.items.per_request` | count | Items per request | attribute, transport |
| `ofx.message.size` | bytes | Message size | transport, direction |
| `ofx.database.query.duration` | ms | Query duration | db_system, operation |
| `ofx.expression.parsing.duration` | ms | Parsing duration | complexity |

#### Gauges

| Metric | Description |
|--------|-------------|
| `ofx.requests.active` | Current active requests |

### Prometheus Metrics Endpoint

```csharp
app.MapPrometheusScrapingEndpoint();  // /metrics
```

Example output:

```
# HELP ofx_request_count Total number of OfX requests
# TYPE ofx_request_count counter
ofx_request_count{attribute="OrderAttribute",transport="kafka",status="success"} 1234

# HELP ofx_request_duration Duration of OfX requests
# TYPE ofx_request_duration histogram
ofx_request_duration_bucket{attribute="OrderAttribute",transport="kafka",le="10"} 100
ofx_request_duration_bucket{attribute="OrderAttribute",transport="kafka",le="50"} 450
ofx_request_duration_bucket{attribute="OrderAttribute",transport="kafka",le="100"} 800
```

### Grafana Dashboard

Import dashboard template from `docs/grafana/ofx-dashboard.json`:

- Request rate & latency
- Error rate
- Item throughput
- Transport breakdown
- Database query performance

---

## Diagnostic Events

For custom telemetry consumers that don't use OpenTelemetry:

### Subscribe to Events

```csharp
using System.Diagnostics;
using OfX.Telemetry;

DiagnosticListener.AllListeners.Subscribe(listener =>
{
    if (listener.Name == "OfX")
    {
        listener.Subscribe(evt =>
        {
            Console.WriteLine($"Event: {evt.Key}");
            Console.WriteLine($"Data: {evt.Value}");
        });
    }
});
```

### Available Events

| Event | Payload |
|-------|---------|
| `OfX.Request.Start` | Attribute, Transport, SelectorIds, Expression, Timestamp |
| `OfX.Request.Stop` | Attribute, Transport, ItemCount, Duration, Timestamp |
| `OfX.Request.Error` | Attribute, Transport, Exception, ErrorType, Duration |
| `OfX.Message.Send` | Transport, Destination, MessageId, SizeBytes |
| `OfX.Message.Receive` | Transport, Source, MessageId, SizeBytes |
| `OfX.Database.Query.Start` | DbSystem, Operation, Database |
| `OfX.Database.Query.Stop` | DbSystem, Operation, RowCount, Duration |
| `OfX.Expression.Parse` | Expression, Duration, Success |
| `OfX.Cache.Lookup` | CacheType, Key, Hit |

### Custom Event Consumer

```csharp
listener.Subscribe(evt =>
{
    if (evt.Key == OfXDiagnostics.RequestStartEvent)
    {
        var data = (dynamic)evt.Value!;
        _logger.LogInformation(
            "OfX Request Started: {Attribute} via {Transport}",
            data.Attribute,
            data.Transport);
    }
});
```

---

## Performance Considerations

### Zero-Allocation When Disabled

OfX telemetry has **zero overhead** when not observed:

```csharp
// If no OpenTelemetry listener configured:
var activity = OfXActivitySource.StartClientActivity<OrderAttribute>("kafka");
// Returns null immediately (no allocation)

if (activity != null)
{
    // This code never executes if tracing disabled
    activity.SetTag("expensive.tag", ComputeExpensiveValue());
}
```

### Sampling

Reduce overhead by sampling:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetSampler(new TraceIdRatioBasedSampler(0.1))  // Sample 10%
        .AddSource("OfX"));
```

### Head-based vs Tail-based Sampling

```csharp
// Head-based: Decision at start
.SetSampler(new TraceIdRatioBasedSampler(0.1))

// Tail-based: Decision after completion (requires collector)
// Configure in OpenTelemetry Collector config
```

---

## Production Deployment

### Recommended Architecture

```
┌──────────────┐
│ Application  │
│  (OfX SDK)   │
└──────┬───────┘
       │ OTLP (gRPC)
       ▼
┌──────────────────┐
│ OTel Collector   │
│ - Batching       │
│ - Sampling       │
│ - Enrichment     │
└──────┬───────────┘
       │
   ┌───┴────┬──────────┬─────────┐
   ▼        ▼          ▼         ▼
┌──────┐ ┌──────┐  ┌──────┐  ┌──────┐
│Jaeger│ │Prom  │  │Loki  │  │Tempo │
│      │ │etheus│  │      │  │      │
└──────┘ └──────┘  └──────┘  └──────┘
```

### Configuration Example

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("OfX")
        .SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddService("my-service", serviceVersion: "1.0.0")
            .AddTelemetrySdk())
        .SetSampler(new TraceIdRatioBasedSampler(0.1))
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://otel-collector:4317");
            options.Protocol = OtlpExportProtocol.Grpc;
        }))
    .WithMetrics(metrics => metrics
        .AddMeter("OfX")
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://otel-collector:4317");
        }));
```

---

## Troubleshooting

### No Traces Appearing

1. Check OpenTelemetry listener is configured:
   ```csharp
   .AddSource("OfX")  // Must be present!
   ```

2. Verify exporter endpoint:
   ```csharp
   options.Endpoint = new Uri("http://localhost:4317");
   ```

3. Check firewall/network connectivity

### High Overhead

1. Enable sampling:
   ```csharp
   .SetSampler(new TraceIdRatioBasedSampler(0.1))
   ```

2. Reduce tag cardinality (avoid high-cardinality tags like IDs)

3. Use tail-based sampling in collector

### Missing Child Spans

Ensure Activity.Current propagates:

```csharp
// ❌ Wrong
Task.Run(() => DoWork());  // Loses Activity.Current

// ✅ Correct
await Task.Run(() => DoWork());  // Preserves context
```

---

## Next Steps

- [Integration Examples](./telemetry-examples.md)
- [Grafana Dashboards](./grafana/)
- [OpenTelemetry Collector Config](./otel-collector.yaml)
- [Performance Benchmarks](./telemetry-benchmarks.md)
