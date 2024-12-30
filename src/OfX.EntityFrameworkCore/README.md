# OfX.EntityFrameworkCore

OfX.EntityFrameworkCore is an extension package for OfX that integrates with Entity Framework Core to simplify data fetching by leveraging attribute-based data mapping. This extension streamlines data retrieval using EF Core, reducing boilerplate code and improving maintainability.

[Demo Project!](https://github.com/quyvu01/TestOfX-Demo)

---

## Introduction

OfX.EntityFrameworkCore extends the core OfX library by providing seamless integration with Entity Framework Core. This enables developers to automatically map and retrieve data directly from a database, leveraging the power of EF Core along with attribute-based data mapping.

For example, suppose you have a `UserId` property in your model, and you want to fetch the corresponding `UserName` and `Email` fields from the database. By using OfX.EntityFrameworkCore, you can annotate your model with attributes, and the library will handle data fetching for you.

---

## Installation

To install the OfX.EntityFrameworkCore package, use the following NuGet command:

```bash
dotnet add package OfX-EFCore
```

Or via the NuGet Package Manager:

```bash
Install-Package OfX-EFCore
```

---

## How to Use

### 1. Register OfX.EntityFrameworkCore

Add OfX.EntityFrameworkCore to your service configuration during application startup:

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

After installing the package OfX-EFCore, you can use the method `AddDbContexts()`, which takes `DbContext(s)` to executing.

### 2. Mark the model you want to use with OfXAttribute
Example:

```csharp
[OfXConfigFor<UserOfAttribute>(nameof(Id), nameof(Name))]
public class User
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}
```
That all! Let go to the moon!

Note: In this release, Id is exclusively supported as a string. But hold tight—I'm gearing up to blow your mind with the next update! Stay tuned!

| Package Name                                             | Description                                                                                     | .NET Version | Document                                                                  |
|----------------------------------------------------------|-------------------------------------------------------------------------------------------------|--------------|---------------------------------------------------------------------------|
| [OfX](https://www.nuget.org/packages/OfX/)               | OfX core                                                                                        | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/README.md)              |
| [OfX-EFCore](https://www.nuget.org/packages/OfX-EFCore/) | This is the OfX extension package using EntityFramework to fetch data                           | 8.0, 9.0     | This Document                                                             |
| [OfX-gRPC](https://www.nuget.org/packages/OfX-gRPC/)     | OfX.gRPC is an extension package for OfX that leverages gRPC for efficient data transportation. | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.Grpc/README.md) |

---