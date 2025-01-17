# OfX-EFCore

OfX-EFCore is an extension package for OfX that integrates with Entity Framework Core to simplify data fetching by leveraging attribute-based data mapping. This extension streamlines data retrieval using EF Core, reducing boilerplate code and improving maintainability.

[Demo Project!](https://github.com/quyvu01/TestOfX-Demo)

---

## Introduction

OfX-EFCore extends the core OfX library by providing seamless integration with Entity Framework Core. This enables developers to automatically map and retrieve data directly from a database, leveraging the power of Entity Framework Core along with attribute-based data mapping.

For example, suppose you have a `UserId` property in your model, and you want to fetch the corresponding `Name` and `Email` fields from the database. By using OfX-EFCore, you can annotate your model with attributes, and the library will handle data fetching for you.

---

## Installation

To install the OfX-EFCore package, use the following NuGet command:

```bash
dotnet add package OfX-EFCore
```

Or via the NuGet Package Manager:

```bash
Install-Package OfX-EFCore
```

---

## How to Use

### 1. Register OfX-EfCore

Add OfX-EfCore to your service configuration during application startup:

```csharp
builder.Services.AddOfXEntityFrameworkCore(cfg =>
{
    cfg.AddAttributesContainNamespaces(typeof(WhereTheAttributeDefined).Assembly);
    cfg.AddHandlersFromNamespaceContaining<SomeHandlerAssemblyMarker>();
})
.AddOfXEFCore(options =>
{
    options.AddDbContexts(typeof(TestDbContext));
    options.AddModelConfigurationsFromNamespaceContaining<SomeModelAssemblyMarker>();
});
```
### Function Descriptions
#### AddDbContexts
Here, you can use the method `AddDbContexts()`, which takes `DbContext(s)` to executing.
#### AddModelConfigurationsFromNamespaceContaining
You have to tell OfX-EFCore where your models are located by assembly. OfX-EFCore will find all `OfXConfigForAttribute` to create handler automatically!

That all, Enjoy your moment!

| Package Name                                                 | Description                                                                                             | .NET Version | Document                                                                      |
|--------------------------------------------------------------|---------------------------------------------------------------------------------------------------------|--------------|-------------------------------------------------------------------------------|
| [OfX](https://www.nuget.org/packages/OfX/)                   | OfX core                                                                                                | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/README.md)                  |
| [OfX-EFCore](https://www.nuget.org/packages/OfX-EFCore/)     | This is the OfX extension package using EntityFramework to fetch data                                   | 8.0, 9.0     | This Document                                                                 |
| [OfX-gRPC](https://www.nuget.org/packages/OfX-gRPC/)         | OfX.gRPC is an extension package for OfX that leverages gRPC for efficient data transportation.         | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.Grpc/README.md)     |
| [OfX-Nats](https://www.nuget.org/packages/OfX-Nats/)         | OfX-Nats is an extension package for OfX that leverages Nats for efficient data transportation.         | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.Nats/README.md)     |
| [OfX-RabbitMq](https://www.nuget.org/packages/OfX-RabbitMq/) | OfX-RabbitMq is an extension package for OfX that leverages RabbitMq for efficient data transportation. | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.RabbitMq/README.md) |
| [OfX-Kafka](https://www.nuget.org/packages/OfX-Kafka/)       | OfX-Kafka is an extension package for OfX that leverages Kafka for efficient data transportation.       | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.Kafka/README.md)    |
---
