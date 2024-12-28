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
    cfg.AddContractsContainNamespaces(typeof(SomeContractAssemblyMarker).Assembly);
    cfg.AddHandlersFromNamespaceContaining<SomeHandlerAssemblyMarker>();
    cfg.AddGrpcClients(config => config
        .AddGrpcHostWithOfXAttributes("http://localhost:5001", [typeof(UserOfAttribute)])
        .AddGrpcHostWithOfXAttributes("http://localhost:5002", [typeof(CountryOfAttribute), typeof(ProvinceOfAttribute)...])
        ... //Other host configurations, you can also filter attributes by creating an interface and then filtering the attributes that implement the interface...
    ); //gRPC server host

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

After installing the package OfX-gRPC, you can use the extension method `AddGrpcClients()` for client and `MapOfXGrpcService()` for server. Look up at `AddGrpcClients` function, we have to define the contract assembly with server host, on this example above, all the queries are included in `SomeContractAssemblyMarker` assembly.

That All, enjoy your moment!


| Package Name                                             | Description                                                                                     | .NET Version | Document                                                                                 |
|----------------------------------------------------------|-------------------------------------------------------------------------------------------------|--------------|------------------------------------------------------------------------------------------|
| [OfX](https://www.nuget.org/packages/OfX/)               | OfX core                                                                                        | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/README.md)                             |
| [OfX-EFCore](https://www.nuget.org/packages/OfX-EFCore/) | This is the OfX extension package using EntityFramework to fetch data                           | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.EntityFrameworkCore/README.md) |
| [OfX-gRPC](https://www.nuget.org/packages/OfX-gRPC/)     | OfX.gRPC is an extension package for OfX that leverages gRPC for efficient data transportation. | 8.0, 9.0     | This Document                                                                            |

---