# OfX-Aws-Sqs

Amazon SQS transport implementation for OfX framework.

## Installation

```bash
dotnet add package OfX-Aws-Sqs
```

## Usage

### Configuration

```csharp
builder.Services.AddOfX(register =>
{
    register.AddSqs(sqs =>
    {
        sqs.Region(RegionEndpoint.USEast1, credential =>
        {
            credential.AccessKeyId("your-access-key-id");
            credential.SecretAccessKey("your-secret-access-key");
        });
    });
});
```

### LocalStack Support (for testing)

```csharp
builder.Services.AddOfX(register =>
{
    register.AddSqs(sqs =>
    {
        sqs.Region(RegionEndpoint.USEast1, credential =>
        {
            credential.ServiceUrl("http://localhost:4566"); // LocalStack endpoint
            credential.AccessKeyId("test");
            credential.SecretAccessKey("test");
        });
    });
});
```

### Using IAM Roles (recommended for AWS environments)

```csharp
builder.Services.AddOfX(register =>
{
    register.AddSqs(sqs =>
    {
        sqs.Region(RegionEndpoint.USEast1); // No credentials needed, uses IAM role
    });
});
```

## Features

- ✅ Request/Reply pattern using SQS queues
- ✅ Long polling for efficient message receiving
- ✅ Automatic queue creation and management
- ✅ OpenTelemetry tracing integration (W3C Trace Context)
- ✅ Circuit breaker and supervision pattern
- ✅ Backpressure control
- ✅ Message correlation with unique IDs
- ✅ Persistent response queues per client
- ✅ Automatic cleanup on disposal

## How It Works

### Request/Reply Pattern

1. **Client**: Creates a persistent response queue and sends requests to server queues
2. **Server**: Listens on request queues, processes messages, and sends responses back
3. **Correlation**: Uses `CorrelationId` message attribute to match requests with responses
4. **Tracing**: W3C Trace Context propagation via `traceparent` and `tracestate` attributes

### Queue Naming Convention

- Request queues: `ofx-{namespace}-{attributename}` (e.g., `ofx-myapp-attributes-countryofattribute`)
- Response queues: `ofx-response-{machinename}-{guid}`

### Supervisor Pattern

The SQS transport includes automatic supervision with:
- Circuit breaker for fault tolerance
- Health monitoring (Healthy, Degraded, Unhealthy, CircuitOpen, Stopped)
- Automatic restart on failures
- Exponential backoff

## Configuration Options

| Option | Description | Default |
|--------|-------------|---------|
| VisibilityTimeout | Time message is invisible after being received | 30 seconds |
| WaitTimeSeconds | Long polling wait time | 20 seconds |
| MessageRetentionPeriod | TTL for unprocessed messages | 300 seconds (5 min) |
| MaxNumberOfMessages | Max messages per receive call | 10 |

## Requirements

- AWS Account with SQS access
- IAM permissions for SQS operations (CreateQueue, SendMessage, ReceiveMessage, DeleteMessage, DeleteQueue)
- .NET 8.0 or .NET 9.0

## License

MIT License - see LICENSE file for details
