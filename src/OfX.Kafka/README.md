# OfX-Kafka

OfX-Kafka is an extension package for OfX that leverages Kafka for efficient data transportation. This package provides a high-performance, strongly-typed communication layer for OfX’s Attribute-based Data Mapping, enabling streamlined data retrieval across distributed systems.

[Demo Project!](https://github.com/quyvu01/TestOfX-Demo)

---

## Introduction

Kafka-based Transport: Implements Kafka to handle data communication between services, providing a fast, secure, and scalable solution.

---

## Installation

To install the OfX-Kafka package, use the following NuGet command:

```bash
dotnet add package OfX-Kafka
```

Or via the NuGet Package Manager:

```bash
Install-Package OfX-Kafka
```

---

## How to Use

### 1. Register OfX-Kafka

Add OfX-Kafka to your service configuration during application startup:

```csharp
builder.Services.AddOfX(cfg =>
{
    cfg.AddContractsContainNamespaces(typeof(SomeContractAssemblyMarker).Assembly);
    cfg.AddKafka(c => c.Host("localhost:9092"));
});

...

var app = builder.Build();

app.Run();

```
`Note:` OfX-Kafka uses topics that start with `ofx-response-topic-[application.friendly.name]`. Therefore, you should avoid using other topics. Additionally, OfX-Kafka automatically creates the topic `ofx-request-topic-[ofx.attribute.metadata]`, so you should avoid creating a topic with the same name in your application.

That All, enjoy your moment!

| Package Name                       | Description                                                                                             | .NET Version | Document                                                                                 |
|------------------------------------|---------------------------------------------------------------------------------------------------------|--------------|------------------------------------------------------------------------------------------|
| **Core**                           |                                                                                                         |
| [OfX][OfX.nuget]                   | OfX core                                                                                                | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/README.md)                             |
| **Data Providers**                 |                                                                                                         |
| [OfX-EFCore][OfX-EFCore.nuget]     | This is the OfX extension package using EntityFramework to fetch data                                   | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.EntityFrameworkCore/README.md) |
| **Transports**                     |                                                                                                         |
| [OfX-gRPC][OfX-gRPC.nuget]         | OfX.gRPC is an extension package for OfX that leverages gRPC for efficient data transportation.         | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.Grpc/README.md)                |
| [OfX-Kafka][OfX-Kafka.nuget]       | OfX-Kafka is an extension package for OfX that leverages Kafka for efficient data transportation.       | 8.0, 9.0     | This Document                                                                            |
| [OfX-Nats][OfX-Nats.nuget]         | OfX-Nats is an extension package for OfX that leverages Nats for efficient data transportation.         | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.Nats/README.md)                |
| [OfX-RabbitMq][OfX-RabbitMq.nuget] | OfX-RabbitMq is an extension package for OfX that leverages RabbitMq for efficient data transportation. | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.RabbitMq/README.md)            |

---

[OfX.nuget]: https://www.nuget.org/packages/OfX/
[OfX-EFCore.nuget]: https://www.nuget.org/packages/OfX-EFCore/
[OfX-gRPC.nuget]: https://www.nuget.org/packages/OfX-gRPC/
[OfX-Nats.nuget]: https://www.nuget.org/packages/OfX-Nats/
[OfX-RabbitMq.nuget]: https://www.nuget.org/packages/OfX-RabbitMq/
[OfX-Kafka.nuget]: https://www.nuget.org/packages/OfX-Kafka/