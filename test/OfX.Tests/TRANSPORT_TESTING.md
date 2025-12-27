# Transport Layer Testing Guide

## Overview

This document provides guidance on testing the OfX transport layer implementations (RabbitMQ, NATS, Kafka, gRPC, Azure Service Bus).

## Why Integration Tests Are Required

The transport layer tests were intentionally not included in the unit test suite because:

1. **Infrastructure Dependencies**: Each transport requires actual infrastructure (message brokers, service buses) to function
2. **Complex API Design**: Transport configuration uses a chained API (`OfXRegister` -> `OfXRegisterWrapped`) that's difficult to mock
3. **Network Operations**: Transport functionality is inherently I/O-bound and requires real network communication
4. **State Management**: Transports maintain connection state, correlation tracking, and async message handling that's hard to unit test

## Recommended Testing Approach

### 1. Integration Tests with Test Containers

Use **Testcontainers** library to spin up real infrastructure for integration tests:

```csharp
[Fact]
public async Task RabbitMq_Should_Handle_Request_Reply()
{
    // Arrange - Start RabbitMQ container
    await using var container = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-management")
        .Build();

    await container.StartAsync();

    // Configure services with real RabbitMQ
    var services = new ServiceCollection();
    services.AddOfX(options =>
    {
        options.AddAttributesContainNamespaces(typeof(UserOfAttribute).Assembly);
    }).AddRabbitMq(config =>
    {
        config.Host(container.Hostname, container.GetMappedPublicPort(5672));
    });

    // Act - Send request
    var handler = serviceProvider.GetService<IMappableRequestHandler<UserOfAttribute>>();
    var response = await handler.RequestAsync(new RequestContext<UserOfAttribute>(...));

    // Assert
    response.ShouldNotBeNull();
}
```

### 2. Manual Testing with Docker Compose

Create a `docker-compose.test.yml` file to start all transport infrastructure:

```yaml
version: '3.8'
services:
  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"
      - "15672:15672"

  nats:
    image: nats:latest
    ports:
      - "4222:4222"

  kafka:
    image: confluentinc/cp-kafka:latest
    ports:
      - "9092:9092"

  azurite:  # Azure Storage Emulator for Service Bus local testing
    image: mcr.microsoft.com/azure-storage/azurite
    ports:
      - "10000:10000"
      - "10001:10001"
```

Then run manual tests:
```bash
docker-compose -f docker-compose.test.yml up -d
dotnet test --filter "Category=Transport"
docker-compose -f docker-compose.test.yml down
```

### 3. Testing Scenarios

#### A. Configuration & Registration
- ✅ Verify transport can be registered with DI container
- ✅ Verify handlers are created for different attributes
- ✅ Verify configuration options are accepted

#### B. Request-Reply Flow
- ⚠️ **Requires Infrastructure**: Start server, send request from client, verify response
- ⚠️ **Requires Infrastructure**: Test correlation ID matching
- ⚠️ **Requires Infrastructure**: Test timeout behavior

#### C. Header Propagation
- ⚠️ **Requires Infrastructure**: Send custom headers, verify they arrive on server
- ⚠️ **Requires Infrastructure**: Test header serialization/deserialization

#### D. Error Handling
- ⚠️ **Requires Infrastructure**: Throw exception in handler, verify error returned to client
- ⚠️ **Requires Infrastructure**: Test connection failure recovery

#### E. Concurrency
- ⚠️ **Requires Infrastructure**: Send multiple concurrent requests
- ⚠️ **Requires Infrastructure**: Verify no correlation ID collisions

### 4. Performance Testing

For performance testing, use BenchmarkDotNet:

```csharp
[MemoryDiagnoser]
public class TransportBenchmarks
{
    [Benchmark]
    public async Task RabbitMq_RequestReply()
    {
        // Benchmark request-reply latency
    }

    [Benchmark]
    public async Task Nats_RequestReply()
    {
        // Compare with NATS
    }
}
```

## Test Infrastructure Setup

### Install Testcontainers Packages

```bash
dotnet add package Testcontainers.RabbitMq
dotnet add package Testcontainers.Kafka
dotnet add package Testcontainers.Nats
```

### Example Test Class Structure

```csharp
[Collection("Transport Tests")]  // Sequential execution
public class RabbitMqIntegrationTests : IAsyncLifetime
{
    private RabbitMqContainer _container;
    private IServiceProvider _serviceProvider;

    public async Task InitializeAsync()
    {
        _container = new RabbitMqBuilder().Build();
        await _container.StartAsync();

        // Setup DI container with real connection
        var services = new ServiceCollection();
        services.AddOfX(...)
                .AddRabbitMq(config => config.Host(_container.Hostname, ...));
        _serviceProvider = services.BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task Should_Handle_Request_Reply()
    {
        // Test implementation
    }
}
```

## Current Test Coverage

### Unit Tests (Included)
- ✅ Core OfX functionality (builders, helpers, accessors)
- ✅ Expression parsing and handling
- ✅ Attribute discovery and configuration
- ✅ EF Core integration (with in-memory database)
- ✅ Contract stability (serialization)
- ✅ Performance benchmarks (accessors, caching)

### Integration Tests (Not Included - Require Infrastructure)
- ❌ RabbitMQ message handling
- ❌ NATS pub/sub behavior
- ❌ Kafka producer/consumer
- ❌ Azure Service Bus queues
- ❌ gRPC service calls

## Why Transport Tests Are Skipped

Transport layer testing requires:
1. Real message brokers running (RabbitMQ, Kafka, etc.)
2. Network connectivity
3. Async message handling and correlation
4. Connection pooling and retry logic
5. Platform-specific behaviors (Windows/Linux/Mac)

These requirements make transport tests:
- **Slow**: Container startup + network I/O
- **Flaky**: Network timeouts, race conditions
- **Environment-Dependent**: Require Docker, specific ports, etc.
- **Resource-Intensive**: Multiple brokers running simultaneously

Therefore, transport testing should be done separately as:
- Manual integration tests during development
- CI/CD pipeline with containerized infrastructure
- Separate test project with `[Trait("Category", "Integration")]`

## Future Work

To add transport integration tests:

1. Create separate test project: `OfX.IntegrationTests`
2. Add Testcontainers dependencies
3. Implement test fixtures for each transport
4. Add CI/CD pipeline stage for integration tests
5. Configure test timeout and retry policies

## Example CI Pipeline

```yaml
name: Integration Tests

jobs:
  transport-tests:
    runs-on: ubuntu-latest
    services:
      rabbitmq:
        image: rabbitmq:3-management
        ports:
          - 5672:5672
      nats:
        image: nats:latest
        ports:
          - 4222:4222
      kafka:
        image: confluentinc/cp-kafka:latest
        ports:
          - 9092:9092

    steps:
      - uses: actions/checkout@v2
      - name: Run Integration Tests
        run: dotnet test --filter "Category=Integration"
```

## Conclusion

Transport layer testing is valuable but requires significant infrastructure setup. The current test suite focuses on core OfX functionality that can be tested in isolation. For production use, implement the integration test strategy outlined above.
