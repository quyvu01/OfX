# OfX.gRPC

OfX.gRPC is an extension package for OfX that leverages gRPC for efficient data transportation. This package provides a high-performance, strongly-typed communication layer for OfX’s Attribute-based Data Mapping, enabling streamlined data retrieval across distributed systems.

[Demo Project!](https://github.com/quyvu01/TestOfX-Demo)

---

## Introduction

gRPC-based Transport: Implements gRPC to handle data communication between services, providing a fast, secure, and scalable solution.

---

## Installation

To install the OfX.EntityFrameworkCore package, use the following NuGet command:

```bash
dotnet add package OfX-gRPC
```

Or via the NuGet Package Manager:

```bash
Install-Package OfX-gRPC
```

---

## How to Use

### 1. Register OfX-gRPC

Add OfX-gRPC to your service configuration during application startup:

For Client:

```csharp
builder.Services.AddOfXEntityFrameworkCore(cfg =>
{
    cfg.RegisterContractsContainsAssemblies(typeof(SomeContractAssemblyMarker).Assembly);
    cfg.RegisterHandlersContainsAssembly<SomeHandlerAssemblyMarker>();
    cfg.RegisterClientsAsGrpc(config => config.RegisterForAssembly<SomeContractAssemblyMarker>("http://localhost:5001")); //gRPC server host

});
```

For Server:

```csharp
var builder = WebApplication.CreateBuilder(args);
...
var app = builder.Build();
...
app.MapOfXGrpcService();
...
```

After installing the package OfX-gRPC, you can use the extension method `RegisterClientsAsGrpc()` for client and `MapOfXGrpcService()` for server. Look up at `RegisterClientsAsGrpc` function, we have to defind the contract assembly with server host, on this example above, all the queries are inclued in `SomeContractAssemblyMarker` assembly.

That All, enjoy your moment!

---