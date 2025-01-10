# OfX-RabbitMq

OfX-RabbitMq is an extension package for OfX that leverages Nats for efficient data transportation. This package provides a high-performance, strongly-typed communication layer for OfX’s Attribute-based Data Mapping, enabling streamlined data retrieval across distributed systems.

[Demo Project!](https://github.com/quyvu01/TestOfX-Demo)

---

## Introduction

RabbitMq-based Transport: Implements RabbitMq to handle data communication between services, providing a fast, secure, and scalable solution.

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
app.StartRabbitMqListeningAsync(); // When you want to create RabbitMq Listening
app.Run();

```
That All, enjoy your moment!


| Package Name                                                 | Description                                                                                             | .NET Version | Document                                                                                 |
|--------------------------------------------------------------|---------------------------------------------------------------------------------------------------------|--------------|------------------------------------------------------------------------------------------|
| [OfX](https://www.nuget.org/packages/OfX/)                   | OfX core                                                                                                | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/README.md)                             |
| [OfX-EFCore](https://www.nuget.org/packages/OfX-EFCore/)     | This is the OfX extension package using EntityFramework to fetch data                                   | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.EntityFrameworkCore/README.md) |
| [OfX-gRPC](https://www.nuget.org/packages/OfX-gRPC/)         | OfX-gRPC is an extension package for OfX that leverages gRPC for efficient data transportation.         | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.Grpc/README.md)                |
| [OfX-Nats](https://www.nuget.org/packages/OfX-Nats/)         | OfX-Nats is an extension package for OfX that leverages Nats for efficient data transportation.         | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.Nats/README.md)                |
| [OfX-RabbitMq](https://www.nuget.org/packages/OfX-RabbitMq/) | OfX-RabbitMq is an extension package for OfX that leverages RabbitMq for efficient data transportation. | 8.0, 9.0     | This Document                                                                            |
---