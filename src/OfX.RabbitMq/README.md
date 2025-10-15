# OfX-RabbitMq

OfX-RabbitMq is an extension package for OfX that leverages RabbitMq for efficient data transportation. This package
provides a high-performance, strongly-typed communication layer for OfX’s Attribute-based Data Mapping, enabling
streamlined data retrieval across distributed systems.

[Demo Project!](https://github.com/quyvu01/TestOfX-Demo)

---

## Introduction

RabbitMq-based Transport: Implements RabbitMq to handle data communication between services, providing a fast, secure,
and scalable solution.

---

## Installation

To install the OfX-RabbitMq package, use the following NuGet command:

```bash
dotnet add package OfX-RabbitMq
```

Or via the NuGet Package Manager:

```bash
Install-Package OfX-RabbitMq
```

---

## How to Use

### 1. Register OfX-RabbitMq

Add OfX-RabbitMq to your service configuration during application startup:

```csharp
builder.Services.AddOfX(cfg =>
{
    cfg.AddContractsContainNamespaces(typeof(SomeContractAssemblyMarker).Assembly);
    cfg.AddRabbitMq(config => config.Host("localhost", "/")); // This is usally used for local test
    // Or fullly configuration as bellow:
    cfg.AddRabbitMq(config => config.Host("localhost", "/", 5672, c =>
    {
        c.UserName("SomeUserName");
        c.Password("SomePassword");
    }));
});

...

var app = builder.Build();

app.Run();

```

`Note:` OfX-RabbitMq uses exchanges that start with `OfX-[OfXAttribute metadata]`. Therefore, you should avoid using
other exchanges. Additionally, OfX-RabbitMq automatically creates the queue `ofx-rpc-queue-[application friendly name]`,
so you should avoid creating a queue with the same name in your application.

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
| [OfX-Azure.ServiceBus][OfX-Azure.ServiceBus.nuget] | OfX.Azure.ServiceBus is an extension package for OfX that leverages Azure ServiceBus for efficient data transportation. | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.Azure.ServiceBus/README.md)    |
| [OfX-gRPC][OfX-gRPC.nuget]                         | OfX.gRPC is an extension package for OfX that leverages gRPC for efficient data transportation.                         | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.Grpc/README.md)                |
| [OfX-Kafka][OfX-Kafka.nuget]                       | OfX-Kafka is an extension package for OfX that leverages Kafka for efficient data transportation.                       | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.Kafka/README.md)               |
| [OfX-Nats][OfX-Nats.nuget]                         | OfX-Nats is an extension package for OfX that leverages Nats for efficient data transportation.                         | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.Nats/README.md)                |
| [OfX-RabbitMq][OfX-RabbitMq.nuget]                 | OfX-RabbitMq is an extension package for OfX that leverages RabbitMq for efficient data transportation.                 | 8.0, 9.0     | This Document                                                                            |

---

[OfX.nuget]: https://www.nuget.org/packages/OfX/

[OfX-EFCore.nuget]: https://www.nuget.org/packages/OfX-EFCore/

[OfX-MongoDb.nuget]: https://www.nuget.org/packages/OfX-MongoDb/

[OfX-HotChocolate.nuget]: https://www.nuget.org/packages/OfX-HotChocolate/

[OfX-gRPC.nuget]: https://www.nuget.org/packages/OfX-gRPC/

[OfX-Nats.nuget]: https://www.nuget.org/packages/OfX-Nats/

[OfX-RabbitMq.nuget]: https://www.nuget.org/packages/OfX-RabbitMq/

[OfX-Kafka.nuget]: https://www.nuget.org/packages/OfX-Kafka/

[OfX-Azure.ServiceBus.nuget]: https://www.nuget.org/packages/OfX-Azure.ServiceBus/