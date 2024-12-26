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
    cfg.RegisterContractsContainsAssemblies(typeof(SomeContractAssemblyMarker).Assembly);
    cfg.RegisterHandlersContainsAssembly<SomeHandlerAssemblyMarker>();
})
.AddOfXEFCore<ServiceDbContext>()
.AddOfXHandlers<IHandlerAssemblyMarker>();
```

After installing the package OfX-EFCore, you can use the extension method `RegisterOfXEntityFramework()`, which takes two arguments: the `DbContext` and the handlers assembly.

### 2. Write a Handler Using EF Core

Implement a request handler to fetch the required data using Entity Framework Core. For example:

```csharp
public sealed class UserOfXHandler(IServiceProvider serviceProvider)
    : EfQueryOfXHandler<User, GetUserOfXQuery>(serviceProvider)
{
    protected override Func<GetUserOfXQuery, Expression<Func<User, bool>>> SetFilter() =>
        q => u => q.SelectorIds.Contains(u.Id);

    protected override Expression<Func<User, OfXDataResponse>> SetHowToGetDefaultData() =>
        u => new OfXDataResponse { Id = u.Id, Value = u.Name };
}
```

### Function Details

#### `SetFilter`
This function is used to define the filter for querying data. It takes the query (`GetUserOfXQuery`) as an argument and returns an `Expression<Func<TModel, bool>>`, where `TModel` is your EF Core entity.

Example:
```csharp
protected override Func<GetUserOfXQuery, Expression<Func<User, bool>>> SetFilter() =>
    q => u => q.SelectorIds.Contains(u.Id);
```
Here, `SetFilter` ensures that only entities matching the provided `SelectorIds` in the query are retrieved.

#### `SetHowToGetDefaultData`
This function specifies how to map the retrieved entity to the default data format. It returns an `Expression<Func<TModel, OfXDataResponse>>`, where `TModel` is your EF Core entity and `OfXDataResponse` is the mapped data structure.

Example:
```csharp
protected override Expression<Func<User, OfXDataResponse>> SetHowToGetDefaultData() =>
    u => new OfXDataResponse { Id = u.Id, Value = u.Name };
```
Here, `SetHowToGetDefaultData` maps the `Id` and `Name` of the `User` entity to the `OfXDataResponse` format.

By overriding these functions, you can customize the filtering logic and data mapping behavior to suit your application's requirements.

---

