# OfX-HotChocolate

**OfX-HotChocolate** is an integration package that seamlessly connects **OfX** with the **HotChocolate** GraphQL library ([Hot Chocolate Docs](https://chillicream.com/docs/hotchocolate/v15)).

With **OfX-HotChocolate**, you get **high-performance, attribute-based data mapping**, making your **GraphQL queries lightning-fast** across distributed systems.

🔥 **Write Less Code. Fetch Data Smarter. Scale Effortlessly.** 🔥

[Demo Project!](https://github.com/quyvu01/TestOfX-Demo)

---

## 🎯 Why OfX-HotChocolate?

**Effortless Data Mapping** – Leverage OfX’s Attribute-based Data Mapping to simplify GraphQL queries.  
**Seamless Integration** – Works out-of-the-box with HotChocolate and OfX.  
**Blazing Fast Queries** – Optimized data retrieval for high-performance systems.  
**Scalable & Flexible** – Works across distributed environments with multiple transport layers.

---

## Installation

To install the OfX-HotChocolate package, use the following NuGet command:

```bash
dotnet add package OfX-HotChocolate
```

Or via the NuGet Package Manager:

```bash
Install-Package OfX-HotChocolate
```

---

## How to Use

### 1. Register OfX-HotChocolate

Add OfX-HotChocolate to your service configuration during application startup:

```csharp
var registerBuilder = builder.Services.AddGraphQLServer()
    .AddQueryType<Query>();
    
builder.Services.AddOfX(cfg =>
{
    cfg.AddContractsContainNamespaces(typeof(SomeContractAssemblyMarker).Assembly);
    cfg.AddNats(config => config.Url("nats://localhost:4222"));
})
.AddHotChocolate(cfg => cfg.AddRequestExecutorBuilder(registerBuilder));

...

var app = builder.Build();

app.Run();
```
`Note:` OfX-HotChocolate will dynamic create the `ObjectTypeExtension<T>` for **ResponseType**. So If you want to create **ObjectType** for some object e.g: `UserResponse`,
please use `ObjectTypeExtension<T>` instead of `ObjectType<T>`.

That All, enjoy your moment!

| Package Name                               | Description                                                                                             | .NET Version | Document                                                                                 |
|--------------------------------------------|---------------------------------------------------------------------------------------------------------|--------------|------------------------------------------------------------------------------------------|
| **Core**                                   |                                                                                                         |
| [OfX][OfX.nuget]                           | OfX core                                                                                                | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/README.md)                             |
| **Data Providers**                         |                                                                                                         |
| [OfX-EFCore][OfX-EFCore.nuget]             | This is the OfX extension package using EntityFramework to fetch data                                   | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.EntityFrameworkCore/README.md) |
| [OfX-MongoDb][OfX-MongoDb.nuget]           | This is the OfX extension package using MongoDb to fetch data                                           | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.MongoDb/README.md)             |
| **Integrations**                           |                                                                                                         |
| [OfX-HotChocolate][OfX-HotChocolate.nuget] | OfX.HotChocolate is an integration package with HotChocolate for OfX.                                   | 8.0, 9.0     | This Document                                                                            |
| **Transports**                             |                                                                                                         |
| [OfX-gRPC][OfX-gRPC.nuget]                 | OfX.gRPC is an extension package for OfX that leverages gRPC for efficient data transportation.         | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.Grpc/README.md)                |
| [OfX-Kafka][OfX-Kafka.nuget]               | OfX-Kafka is an extension package for OfX that leverages Kafka for efficient data transportation.       | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.Kafka/README.md)               |
| [OfX-Nats][OfX-Nats.nuget]                 | OfX-Nats is an extension package for OfX that leverages Nats for efficient data transportation.         | 8.0, 9.0     | This Document                                                                            |
| [OfX-RabbitMq][OfX-RabbitMq.nuget]         | OfX-RabbitMq is an extension package for OfX that leverages RabbitMq for efficient data transportation. | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.RabbitMq/README.md)            |

---

[OfX.nuget]: https://www.nuget.org/packages/OfX/

[OfX-EFCore.nuget]: https://www.nuget.org/packages/OfX-EFCore/

[OfX-MongoDb.nuget]: https://www.nuget.org/packages/OfX-MongoDb/

[OfX-HotChocolate.nuget]: https://www.nuget.org/packages/OfX-HotChocolate/

[OfX-gRPC.nuget]: https://www.nuget.org/packages/OfX-gRPC/

[OfX-Nats.nuget]: https://www.nuget.org/packages/OfX-Nats/

[OfX-RabbitMq.nuget]: https://www.nuget.org/packages/OfX-RabbitMq/

[OfX-Kafka.nuget]: https://www.nuget.org/packages/OfX-Kafka/