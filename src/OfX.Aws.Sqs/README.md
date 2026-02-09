# OfX-Aws.Sqs

OfX-Aws.Sqs is an extension package for OfX that leverages Amazon SQS for efficient data transportation. This package provides a high-performance, strongly-typed communication layer for OfX's Attribute-based Data Mapping, enabling streamlined data retrieval across distributed systems.

[Demo Project!](https://github.com/quyvu01/TestOfX-Demo)

---

## Introduction

Amazon SQS-based Transport: Implements Amazon SQS to handle data communication between services, providing a fast, secure, and scalable solution with features like long polling, batch processing, and automatic supervision.

---

## Installation

To install the OfX-Aws.Sqs package, use the following NuGet command:

```bash
dotnet add package OfX-Aws.Sqs
```

Or via the NuGet Package Manager:

```bash
Install-Package OfX-Aws.Sqs
```

---

## How to Use

### 1. Register OfX-Aws.Sqs

Add OfX-Aws.Sqs to your service configuration during application startup:

```csharp
builder.Services.AddOfX(cfg =>
{
    cfg.AddContractsContainNamespaces(typeof(SomeContractAssemblyMarker).Assembly);
    cfg.AddSqs(sqs =>
    {
        sqs.Region(RegionEndpoint.USEast1, credential =>
        {
            credential.AccessKeyId("your-access-key-id");
            credential.SecretAccessKey("your-secret-access-key");
        });
    });
});

...

var app = builder.Build();

app.Run();

```

### LocalStack Support (for testing)

```csharp
builder.Services.AddOfX(cfg =>
{
    cfg.AddContractsContainNamespaces(typeof(SomeContractAssemblyMarker).Assembly);
    cfg.AddSqs(sqs =>
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
builder.Services.AddOfX(cfg =>
{
    cfg.AddContractsContainNamespaces(typeof(SomeContractAssemblyMarker).Assembly);
    cfg.AddSqs(sqs =>
    {
        sqs.Region(RegionEndpoint.USEast1); // No credentials needed, uses IAM role
    });
});
```

`Note:` OfX-Aws.Sqs uses queues that start with `ofx-{namespace}-{attributename}`. Therefore, you should avoid using other queues. Additionally, OfX-Aws.Sqs automatically creates response queues `ofx-response-{machinename}-{guid}`, so you should avoid creating a queue with the same name in your application.

That All, enjoy your moment!

| Package Name                                       | Description                                                                                                             | .NET Version | Document                                                                                 |
|----------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------|--------------|------------------------------------------------------------------------------------------|
| **Core**                                           |                                                                                                                         |
| [OfX][OfX.nuget]                                   | OfX core                                                                                                                | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/README.md)                             |
| **Data Providers**                                 |                                                                                                                         |
| [OfX-EFCore][OfX-EFCore.nuget]                     | This is the OfX extension package using EntityFramework to fetch data                                                   | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.EntityFrameworkCore/README.md) |
| [OfX-MongoDb][OfX-MongoDb.nuget]                   | This is the OfX extension package using MongoDb to fetch data                                                           | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.MongoDb/README.md)             |
| **Integrations**                                   |                                                                                                                         |
| [OfX-HotChocolate][OfX-HotChocolate.nuget]         | OfX.HotChocolate is an integration package with HotChocolate for OfX.                                                   | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.HotChocolate/README.md)        |
| **Transports**                                     |                                                                                                                         |
| [OfX-Aws.Sqs][OfX-Aws.Sqs.nuget]                   | OfX-Aws.Sqs is an extension package for OfX that leverages Amazon SQS for efficient data transportation.                | 8.0, 9.0     | This Document                                                                            |
| [OfX-Azure.ServiceBus][OfX-Azure.ServiceBus.nuget] | OfX.Azure.ServiceBus is an extension package for OfX that leverages Azure ServiceBus for efficient data transportation. | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.Azure.ServiceBus/README.md)    |
| [OfX-gRPC][OfX-gRPC.nuget]                         | OfX.gRPC is an extension package for OfX that leverages gRPC for efficient data transportation.                         | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.Grpc/README.md)                |
| [OfX-Kafka][OfX-Kafka.nuget]                       | OfX-Kafka is an extension package for OfX that leverages Kafka for efficient data transportation.                       | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.Kafka/README.md)               |
| [OfX-Nats][OfX-Nats.nuget]                         | OfX-Nats is an extension package for OfX that leverages Nats for efficient data transportation.                         | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.Nats/README.md)                |
| [OfX-RabbitMq][OfX-RabbitMq.nuget]                 | OfX-RabbitMq is an extension package for OfX that leverages RabbitMq for efficient data transportation.                 | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.RabbitMq/README.md)            |

---

[OfX.nuget]: https://www.nuget.org/packages/OfX/

[OfX-EFCore.nuget]: https://www.nuget.org/packages/OfX-EFCore/

[OfX-MongoDb.nuget]: https://www.nuget.org/packages/OfX-MongoDb/

[OfX-HotChocolate.nuget]: https://www.nuget.org/packages/OfX-HotChocolate/

[OfX-Aws.Sqs.nuget]: https://www.nuget.org/packages/OfX-Aws.Sqs/

[OfX-gRPC.nuget]: https://www.nuget.org/packages/OfX-gRPC/

[OfX-Nats.nuget]: https://www.nuget.org/packages/OfX-Nats/

[OfX-RabbitMq.nuget]: https://www.nuget.org/packages/OfX-RabbitMq/

[OfX-Kafka.nuget]: https://www.nuget.org/packages/OfX-Kafka/

[OfX-Azure.ServiceBus.nuget]: https://www.nuget.org/packages/OfX-Azure.ServiceBus/
