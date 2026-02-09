# OfX-Azure-ServiceBus

OfX-Azure-ServiceBus is an extension package for **OfX** that leverages **Azure Service Bus** for reliable and scalable
message transportation.  
This package provides a **strongly-typed**, **cloud-native** communication layer for OfX’s **Attribute-based Data
Mapping**, enabling seamless data transfer across distributed systems using Microsoft Azure infrastructure.

> [!WARNING]  
> The Azure Service Bus transport only supports Standard and Premium tiers of the Microsoft Azure Service Bus service. Premium tier is recommended for production environments.

[Demo Project!](https://github.com/quyvu01/TestOfX-Demo)

---

## Introduction

**Azure Service Bus-based Transport:**  
Implements Azure Service Bus to handle data communication between distributed OfX services, providing an
enterprise-grade, secure, and scalable messaging backbone with features like topics, queues, and session management.

---

## Installation

To install the **OfX-Azure-ServiceBus** package, use the following NuGet command:

```csharp
dotnet add package OfX.Azure.ServiceBus
```

Or via the NuGet Package Manager:

```csharp
Install-Package OfX.Azure.ServiceBus
```

## How to Use

### 1. Register OfX-Azure-ServiceBus

Add OfX-Azure-ServiceBus to your service configuration during application startup:

Example

```csharp
builder.Services.AddOfX(cfg =>
    {
        cfg.AddAttributesContainNamespaces(typeof(IKernelAssemblyMarker).Assembly);
        cfg.AddModelConfigurationsFromNamespaceContaining<IAssemblyMarker>();
        cfg.AddAzureServiceBus(c => c.Host("SensitiveConnectionString"));
    });
...

var app = builder.Build();

...

app.Run();
```

`Note:` OfX-Azure-ServiceBus uses message subjects that start with ofx-request-[OfXAttribute metadata].
You should avoid using other queues or topics with the same naming pattern.

The package supports both queue-based and topic-based messaging models.

When RequiresSession is enabled, all messages will be processed in a sessionful mode, ensuring ordered delivery.

That’s all — enjoy building your distributed system with OfX!

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
| [OfX-Aws.Sqs][OfX-Aws.Sqs.nuget]                   | OfX-Aws-Sqs is an extension package for OfX that leverages Amazon SQS for efficient data transportation.                | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.Aws.Sqs/README.md)             |
| [OfX-Azure.ServiceBus][OfX-Azure.ServiceBus.nuget] | OfX.Azure.ServiceBus is an extension package for OfX that leverages Azure ServiceBus for efficient data transportation. | 8.0, 9.0     | This Document                                                                            |
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