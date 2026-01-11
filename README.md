# OfX

```csharp
public string XId { get; set; } 
[XOf(nameof(XId))] public string X { get; set; } 
```

OfX is an open-source, which focus on Attribute-based data mapping, streamlines data handling across services, reduces
boilerplate code, and improves maintainability

> [!WARNING]  
> All OfX* packages need to have the same version.

## Give a Star! :star:
If you like this project, learn something or you are using it in your applications, please give it a star. Thanks!

## Project Highlights

Attribute-based Data Mapping in OfX is a feature that lets developers annotate properties in their data models with
custom attributes. These attributes define how and from where data should be fetched, eliminating repetitive code and
automating data retrieval.
For example, imagine a scenario where Service A needs a user’s name stored in Service B. With Attribute-based Data
Mapping, Service A can define a UserName property annotated with `[UserOf(nameof(UserId))]`. This tells the system to
automatically retrieve the UserName based on UserId, without writing custom code each time.

Example:

```csharp
// Basic Config
builder.Services.AddOfX(cfg =>
{
    cfg.AddAttributesContainNamespaces(typeof(UserOfAttribute).Namespace!);
    cfg.AddModelConfigurationsFromNamespaceContaining<SomeModelAssemblyMarker>();
});

// Define a custom OfXAttribute
public sealed class UserOfAttribute(string propertyName) : OfXAttribute(propertyName);

// Tell OfX which model the attribute applies to
[OfXConfigFor<UserOfAttribute>(nameof(Id), nameof(Name))]
public sealed class User
{
    public string Id { get; set; } 
    public string Name { get; set; } 
    public string Email { get; set; } 
    // Add other properties as needed
}

// Sample DTO
public sealed class SomeDataResponse
{
    public string Id { get; set; } 
    public string UserId { get; set; } 
    
    [UserOf(nameof(UserId), Expression = "Email")]
    public string UserEmail { get; set; } 

    [UserOf(nameof(UserId))]
    public string UserName { get; set; } 

    // Add other properties as needed
}

// You are touching the OfX!

```

The `[UserOf]` annotation acts as a directive to automatically retrieve `UserName` based on `UserId`,you can also fetch
custom fields as `Email` on the User Table using Expression like `[UserOf(nameof(UserId), Expression="Email")]`. This
eliminates the need for manual mapping logic, freeing developers to focus on core functionality rather than data
plumbing.

## Start with OfX

To install the OfX package, use the following NuGet command:

```bash
dotnet add package OfX
```

Or via the NuGet Package Manager:

```bash
Install-Package OfX
```

## How to Use

### 1. Register OfX in the Dependency Injection Container

Add the OfX to your service configuration to register OfX:

```csharp
builder.Services.AddOfX(cfg =>
{
    cfg.AddAttributesContainNamespaces(typeof(WhereTheAttributeDefined).Assembly);
    cfg.AddHandlersFromNamespaceContaining<SomeHandlerAssemblyMarker>(); //<- Add this one when you want to self-handle the request as the example at the end of this guide. Otherwise, if you install the package OfX-gRPC or OfX-Nats...(like OfX transport extension package), there is no need to add this one anymore!
    cfg.AddReceivedPipelines(c => c.OfType(typeof(GenericReceivedPipeline<>).OfType<OtherReceivedPipeline>());
    cfg.AddSendPipelines(c => c.OfType(typeof(GenericSendPipeline<>).OfType(typeof(OtherSendPipeline<>)));    
    // When you have the stronglyTypeId, you have to create the config how to resolve the Id(from string type) to StronglyTypeId
    cfg.AddStronglyTypeIdConverter(a => a.OfType<StronglyTypeIdRegisters>());
    cfg.AddModelConfigurationsFromNamespaceContaining<SomeModelAssemblyMarker>();
    cfg.ThrowIfException(); // Add this when you want to handle the error and know why the errors are occupied
    cfg.SetMaxObjectSpawnTimes(16); // Add this when you want to limit the maxObject spawn times. It mean you can be noticed that your objects are so complex...
    cfg.SetRetryPolicy(3, retryAttempt => retryAttempt * TimeSpan.FromSeconds(2), (e, ts) => Console.WriteLine($"Error: {e.Message}"));
});
```

### Function Descriptions

#### AddAttributesContainNamespaces

```csharp
cfg.AddAttributesContainNamespaces(typeof(WhereTheAttributeDefined).Assembly);
```

Registers assemblies that contain the attributes, used by OfX for data mapping.

The Attribute must be inherited from `OfXAttribute` and they will be scanned by OfX!

Parameters:
`Assembly`: The assembly containing the (OfX) attributes.

#### AddHandlersFromNamespaceContaining

```csharp
cfg.AddHandlersFromNamespaceContaining<SomeHandlerAssemblyMarker>(); //<- Add this one when you want to self-handle the request as the example at the end of this guide. Otherwise, if you install the package OfX-gRPC or OfX-Nats...(like OfX transport extension package), there is no need to add this one anymore!

```

Add assemblies that contain handlers responsible for processing queries or commands for data retrieval.

Handlers are the execution units that resolve attributes applied to models.

If this function is not invoked. The default value `ItemsResponse<OfXDataResponse>` is returned!

Parameters:
`Type`: A marker type within the assembly that includes the handler implementations.
Example:

```csharp
cfg.AddHandlersFromNamespaceContaining<SomeHandlerAssemblyMarker>();
```

#### AddReceivedPipelines

```csharp
cfg.AddReceivedPipelines(c => c.OfType(typeof(GenericReceivedPipeline<>).OfType<OtherReceivedPipeline>());
```

When you want to create pipelines to handle the received request for `OfXAttribute`. You should use it on the server,
where you are fetching and response to the client!

Parameters:
`Action<ReceivedPipeline>`: add the pipelines.

Example:

```csharp
cfg.AddSendPipelines(c => c.OfType(typeof(GenericSendPipeline<>).OfType(typeof(OtherSendPipeline<>)));    
```

#### AddSendPipelines

```csharp
cfg.AddSendPipelines(c => c.OfType(typeof(GenericSendPipeline<>).OfType(typeof(OtherSendPipeline<>)));    
```

When you want to create pipelines to handle the send request for `OfXAttribute`. You should use it on the client, where
you send request to get data!

Parameters:
`Action<SendPipeline>`: add the pipelines.

Example:

```csharp
cfg.AddReceivedPipelines(c => c.OfType(typeof(GenericPipeline<>)).OfType<OtherPipeline>());
```

#### AddStronglyTypeIdConverter

```csharp
// When you have the stronglyTypeId, you have to create the config how to resolve the Id(from string type) to StronglyTypeId
cfg.AddStronglyTypeIdConverter(a => a.OfType<StronglyTypeIdRegisters>());
```

When your models(entities) are using Strongly Type ID, you have to configure to tell how OfX can convert from general ID
type(string) to your strongly type ID.

Parameters:
`Action<StronglyTypeIdRegister>` the strongly type ID register delegate.

#### OfType

You have to create a class and implement interface `IStronglyTypeConverter<T>`, then you have to override 2 methods(
`Convert` and `CanConvert`) to help OfX convert from general ID type(string) to your strongly type.
Please check the example above!

#### AddModelConfigurationsFromNamespaceContaining

```csharp
cfg.AddModelConfigurationsFromNamespaceContaining<SomeModelAssemblyMarker>();
```

Locate your models and OfX will dynamic create the handler relevant to Model and OfXAttribute

#### ThrowIfException

```csharp
cfg.ThrowIfException(); // Add this when you want to handle the error and know why the errors are occupied
```

This function enables strict error handling within `OfX`.
When added, it ensures that any exceptions encountered during data mapping, request handling, or pipeline execution are
not silently ignored but instead explicitly thrown.
This helps developers quickly identify and debug issues by surfacing errors, making it easier to track down problems in
the OfX processing flow.

#### SetMaxObjectSpawnTimes

```csharp
cfg.SetMaxObjectSpawnTimes(16); // Add this when you want to limit the maxObject spawn times. It mean you can be noticed that your objects are so complex...
```

This function sets an upper limit on the number of times an object can be spawned during recursive data mapping.
By default, (Max spawn times: `128`), OfX allows objects to be dynamically created and mapped, but in complex object
structures, excessive recursive mapping can lead to performance issues or infinite loops.
Setting maxTimes helps prevent excessive nesting by defining a safe threshold, ensuring that the mapping process remains
efficient and controlled.

#### SetRetryPolicy

```csharp
cfg.SetRetryPolicy(3, retryAttempt => retryAttempt * TimeSpan.FromSeconds(2),
    (e, ts) => Console.WriteLine($"Error: {e.Message}")); 
```

This function configures the retry mechanism for transient or recoverable operations during the mapping or data-handling
process.

- **maxRetryCount** (`3` in this example): The maximum number of retry attempts before the operation is considered
  failed.
- **retryDelayProvider** (`retryAttempt => retryAttempt * TimeSpan.FromSeconds(2)`): A function that determines the
  waiting time before each retry. Here, it uses an exponential backoff pattern—each retry waits longer than the last (
  e.g., 2s, 4s, 6s…).
- **onRetry** (`(e, ts) => Console.WriteLine($"Error: {e.Message}")`): A callback triggered whenever a retry occurs,
  useful for logging or monitoring.

By default, OfX executes operations without retrying on failure. Using `SetRetryPolicy` helps improve resilience when
dealing with unstable external dependencies, ensuring temporary issues don't cause the entire mapping process to fail.

### 2. Integrate the `OfXAttribute` into Your Models, Entities, or DTOs

Apply the attribute to your properties like this:

```csharp
public sealed class SomeDataResponse
{
    public string Id { get; set; }
    public string UserId { get; set; }

    [UserOf(nameof(UserId), Expression = "Email")]
    public string UserEmail { get; set; }

    [UserOf(nameof(UserId))]
    public string UserName { get; set; }

    [UserOf(nameof(UserId), Expression = "ProvinceId")]
    public string ProvinceId { get; set; }
    
    [ProvinceOf(nameof(ProvinceId))]
    public string ProvinceName { get; set; }
    
    [ProvinceOf(nameof(ProvinceId), Expression = "Country.Name")]
    public string CountryName { get; set; }

    [ProvinceOf(nameof(ProvinceId), Expression = "CountryId")]
    public string CountryId { get; set; }

    [CountryOf(nameof(CountryId), Expression = "Provinces[0 asc Name].Name")]
    public string Province { get; set; }
    
    // Add other properties as needed
}
```

### 3. Annotate `OfXConfigForAttribute` your models with `OfXAttribute` to then

`OfX` will dynamic create relevant proxy handler for model and `OfXAttribute`

Example:

```csharp
[OfXConfigFor<UserOfAttribute>(nameof(Id), nameof(Name))]
public class User
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string ProvinceId {get; set;}
}
```

### 4. Write the Handlers in Your Service to Fetch the Data(when you are using `OfX` only).

`Note:` If you use OfX-gRPC, OfX-Nats, OfX-RabbitMq... or other transport data layer(next version extension packages),
there are no need to create Handlers anymore, they should be dynamic proxy handlers!

Implement a handler to process data requests.

Example:

```csharp
public class UserRequestHandler(): IMappableRequestHandler<UserOfAttribute>
{
    public async Task<ItemsResponse<OfXDataResponse>> RequestAsync(RequestContext<UserOfAttribute> request)
    {
        // Implement data fetching logic here (e.g., via REST, RPC, or gRPC)
    }
}
```

### 5. Unlock the Full Power of `Expressions`

Expressions in **OfX** enable you to fetch external data dynamically and powerfully. The Expression DSL (Domain Specific
Language) provides a rich set of features for querying, filtering, aggregating, and projecting data.

#### Expression DSL Grammar

```
Expression       := RootProjection | Segment ('.' Segment)*
RootProjection   := '{' ProjectionProperty (',' ProjectionProperty)* '}'
ProjectionProperty := PropertyPath ('as' Identifier)?
PropertyPath     := Identifier ('.' Identifier)*
Segment          := PropertyAccess Filter? Indexer? Projection? Function?
PropertyAccess   := Identifier ('?')?
Filter           := '(' Condition ')'
Condition        := Comparison (('&&' | ',' | 'and' | '||' | 'or') Comparison)*
Comparison       := FieldPath Operator Value
Operator         := '=' | '!=' | '>' | '<' | '>=' | '<=' | 'contains' | 'startswith' | 'endswith'
Indexer          := '[' Number (Number)? ('asc' | 'desc') Identifier ']'
Projection       := '.{' Identifier (',' Identifier)* '}'
Function         := ':' FunctionName ('(' Identifier ')')?
FunctionName     := 'count' | 'sum' | 'avg' | 'min' | 'max'
```

#### Default Data vs. External Data

- **Default Data**: Automatically fetched using `OfX Attribute`. No `Expression` is required.
- **External Data**: Define an `Expression` to fetch specific or relational data from other tables.

---

### Expression Features

#### 1. Simple Property Access

Fetching fields from the same table:

```csharp
public sealed class SomeDataResponse
{
    public string UserId { get; set; }

    [UserOf(nameof(UserId), Expression = "Email")]
    public string UserEmail { get; set; }

    [UserOf(nameof(UserId))]  // Uses default property (Name)
    public string UserName { get; set; }
}
```

#### 2. Navigation Properties

Navigate through related tables using dot notation:

```csharp
[ProvinceOf(nameof(ProvinceId), Expression = "Country.Name")]
public string CountryName { get; set; }

// Deep navigation
[ProvinceOf(nameof(ProvinceId), Expression = "Country.Region.Continent.Name")]
public string ContinentName { get; set; }
```

#### 3. Null-Safe Navigation

Use `?` for null-safe property access:

```csharp
[UserOf(nameof(UserId), Expression = "Address?.City?.Name")]
public string CityName { get; set; }
```

---

### Filtering: `(Condition)`

Filter collections using conditions inside parentheses:

#### Simple Filter

```csharp
[UserOf(nameof(UserId), Expression = "Orders(Status = 'Done')")]
public List<OrderDTO> CompletedOrders { get; set; }
```

#### Multiple Conditions (AND)

```csharp
// Using comma or && or 'and'
[UserOf(nameof(UserId), Expression = "Orders(Status = 'Done' and Total > 100)")]
public List<OrderDTO> LargeCompletedOrders { get; set; }

[UserOf(nameof(UserId), Expression = "Orders(Status = 'Done' && Total > 100)")]
public List<OrderDTO> LargeCompletedOrders { get; set; }
```

#### OR Conditions

```csharp
[UserOf(nameof(UserId), Expression = "Orders(Status = 'Done' || Status = 'Shipped')")]
public List<OrderDTO> ProcessedOrders { get; set; }

[UserOf(nameof(UserId), Expression = "Orders(Status = 'Done' or Status = 'Shipped')")]
public List<OrderDTO> ProcessedOrders { get; set; }
```

#### String Operators

```csharp
// Contains
[UserOf(nameof(UserId), Expression = "Orders(ProductName contains 'Phone')")]
public List<OrderDTO> PhoneOrders { get; set; }

// StartsWith
[UserOf(nameof(UserId), Expression = "Orders(ProductName startswith 'Apple')")]
public List<OrderDTO> AppleOrders { get; set; }

// EndsWith
[UserOf(nameof(UserId), Expression = "Orders(Email endswith '@gmail.com')")]
public List<OrderDTO> GmailOrders { get; set; }
```

#### Comparison Operators

```csharp
[UserOf(nameof(UserId), Expression = "Orders(Total >= 100 and Total <= 500)")]
public List<OrderDTO> MidRangeOrders { get; set; }

[UserOf(nameof(UserId), Expression = "Orders(Quantity != 0)")]
public List<OrderDTO> NonEmptyOrders { get; set; }
```

---

### Indexer: `[skip (take)? asc|desc Property]`

Access specific items or ranges from collections:

#### All Items (Sorted)

```csharp
[CountryOf(nameof(CountryId), Expression = "Provinces[asc Name]")]
public List<ProvinceDTO> Provinces { get; set; }
```

#### First Item

```csharp
[CountryOf(nameof(CountryId), Expression = "Provinces[0 asc Name]")]
public ProvinceDTO FirstProvince { get; set; }
```

#### Last Item

```csharp
[CountryOf(nameof(CountryId), Expression = "Provinces[-1 desc CreatedAt]")]
public ProvinceDTO LatestProvince { get; set; }
```

#### Pagination (Skip & Take)

```csharp
[CountryOf(nameof(CountryId), Expression = "Provinces[10 5 asc Name]")]  // Skip 10, Take 5
public List<ProvinceDTO> PagedProvinces { get; set; }
```

#### Chaining After Indexer

```csharp
[CountryOf(nameof(CountryId), Expression = "Provinces[0 asc Name].Cities[0 asc Population].Name")]
public string LargestCityName { get; set; }
```

---

### Aggregation Functions: `:function`

Perform calculations on collections:

#### Count

```csharp
[UserOf(nameof(UserId), Expression = "Orders:count")]
public int TotalOrders { get; set; }

// Count with filter
[UserOf(nameof(UserId), Expression = "Orders(Status = 'Done'):count")]
public int CompletedOrderCount { get; set; }
```

#### Sum

```csharp
[UserOf(nameof(UserId), Expression = "Orders:sum(Total)")]
public decimal TotalSpent { get; set; }

// Sum with filter
[UserOf(nameof(UserId), Expression = "Orders(Status = 'Done'):sum(Total)")]
public decimal CompletedOrdersTotal { get; set; }
```

#### Average

```csharp
[UserOf(nameof(UserId), Expression = "Orders:avg(Total)")]
public decimal AverageOrderValue { get; set; }
```

#### Min/Max

```csharp
[UserOf(nameof(UserId), Expression = "Orders:min(Total)")]
public decimal SmallestOrder { get; set; }

[UserOf(nameof(UserId), Expression = "Orders:max(Total)")]
public decimal LargestOrder { get; set; }
```

---

### Object Projection: `.{Properties}` and `{Properties}`

Project specific fields from objects or collections:

#### Collection Projection

```csharp
// Select only Id and Status from each order
[UserOf(nameof(UserId), Expression = "Orders.{Id, Status}")]
public List<Dictionary<string, object>> OrderSummaries { get; set; }

// With filter
[UserOf(nameof(UserId), Expression = "Orders(Status = 'Done').{Id, Total}")]
public List<Dictionary<string, object>> CompletedOrderSummaries { get; set; }
```

#### Single Object Projection

```csharp
// Project from navigation property
[ProvinceOf(nameof(ProvinceId), Expression = "Country.{Id, Name}")]
public Dictionary<string, object> CountryInfo { get; set; }
```

#### Root Projection with Navigation & Alias

```csharp
// Project from root with nested navigation and aliases
[ProvinceOf(nameof(ProvinceId), Expression = "{Id, Name, Country.Name as CountryName}")]
public ProvinceComplexResponse ProvinceData { get; set; }
```

**ProvinceComplexResponse structure:**

```csharp
public class ProvinceComplexResponse
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string CountryName { get; set; }  // Mapped from Country.Name
}
```

#### Advanced Root Projection

```csharp
// Multiple navigation paths with aliases
[UserOf(nameof(UserId), Expression = "{Id, Name, Address.City.Name as CityName, Department.Manager.Name as ManagerName}")]
public UserDetailResponse UserDetails { get; set; }
```

**Output:**

```json
{
  "Id": "user_123",
  "Name": "John Doe",
  "CityName": "New York",
  "ManagerName": "Jane Smith"
}
```

---

### Combining Features

Chain multiple features together for powerful queries:

```csharp
// Filter → Sort → Take First → Navigate → Get Property
[CountryOf(nameof(CountryId), Expression = "Provinces(Population > 1000000)[0 desc Population].Capital.Name")]
public string LargestProvinceCapital { get; set; }

// Filter → Aggregate
[UserOf(nameof(UserId), Expression = "Orders(Status = 'Done', CreatedAt > '2024-01-01'):sum(Total)")]
public decimal RecentCompletedTotal { get; set; }

// Navigate → Filter → Project
[UserOf(nameof(UserId), Expression = "Department.Employees(IsActive = true).{Id, Name, Email}")]
public List<Dictionary<string, object>> ActiveColleagues { get; set; }
```

---

### Object Mapping

Map entire objects dynamically:

```csharp
[ProvinceOf(nameof(ProvinceId), Expression = "Country")]
public CountryDTO Country { get; set; }
```

```csharp
public sealed class CountryDTO
{
    public string Id { get; set; }
    public string Name { get; set; }
}
```

**Note**: The DTO structure must match the source model's structure. Only direct properties are selected; navigation
properties are ignored.

### 6. Runtime Parameters

#### 1. Expressions now support dynamic runtime values:

```bash
${parameter|default}
```

Example:

```csharp
public sealed class SomeDataResponse
{
    [CountryOf(nameof(CountryId), Expression = "Provinces[${index|0} ${order|asc} Name]")]
    public ProvinceResponse Province { get; set; }
}
```

Runtime input:

```csharp
await mapper.MapDataAsync(response, new { index = -1, order = "desc" });
```

Resolved expression:

```csharp
public sealed class SomeDataResponse
{
    [CountryOf(nameof(CountryId), Expression = "Provinces[-1 desc Name]")]
    public ProvinceResponse Province { get; set; }
}
```

#### 2. MapDataAsync with Parameters

```csharp
await mapper.MapDataAsync(response, new { index = 1, order = "desc" }, cancellationToken);
```

#### 3. GraphQL Parameters

Use the `[Parameters]` attribute:

```csharp
public List<MemberResponse> GetMembers([Parameters] GetMembersParameters parameters)
{
    // Execute logic here, the parameters will be passed to all Expression
}
```

Run query:

```bash
{
  members(parameters: {
       index: 1, order = "desc"
    }) {
    id
    province 
    {
      id
      name 
    }
  }
}
```

### Expression Quick Reference

| Feature               | Syntax                 | Example                       |
|-----------------------|------------------------|-------------------------------|
| Property              | `PropertyName`         | `Email`                       |
| Navigation            | `A.B.C`                | `Country.Region.Name`         |
| Null-safe             | `A?.B`                 | `Address?.City`               |
| Filter                | `(condition)`          | `Orders(Status = 'Done')`     |
| AND                   | `,` or `&&` or `and`   | `(A = 1, B = 2)`              |
| OR                    | `\|\|` or `or`         | `(A = 1 \|\| B = 2)`          |
| Indexer (first)       | `[0 asc Prop]`         | `Orders[0 asc Date]`          |
| Indexer (last)        | `[-1 desc Prop]`       | `Orders[-1 desc Date]`        |
| Indexer (range)       | `[skip take asc Prop]` | `Orders[10 5 asc Date]`       |
| Count                 | `:count`               | `Orders:count`                |
| Sum                   | `:sum(Prop)`           | `Orders:sum(Total)`           |
| Avg                   | `:avg(Prop)`           | `Orders:avg(Total)`           |
| Min/Max               | `:min(Prop)`           | `Orders:min(Total)`           |
| Collection Projection | `.{A, B}`              | `Orders.{Id, Status}`         |
| Root Projection       | `{A, B}`               | `{Id, Name}`                  |
| Alias                 | `A.B as C`             | `Country.Name as CountryName` |

### Conclusion

The Expression DSL in **OfX** provides a powerful, SQL-like syntax for querying and mapping data across complex
relationships. Key features include:

- **Navigation**: Access nested properties with dot notation
- **Filtering**: Filter collections with conditions
- **Indexing**: Access specific items or ranges
- **Aggregation**: Count, sum, average, min, max
- **Projection**: Select specific fields with aliases
- **Runtime Parameters**: Dynamic values at execution time

Whether you're working with single properties, nested objects, or collections, OfX has you covered!

That all, Enjoy your moment!

| Package Name                                       | Description                                                                                                             | .NET Version | Document                                                                                 |
|----------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------|--------------|------------------------------------------------------------------------------------------|
| **Core**                                           |                                                                                                                         |
| [OfX][OfX.nuget]                                   | OfX core                                                                                                                | 8.0, 9.0     | This Document                                                                            |
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
| [OfX-RabbitMq][OfX-RabbitMq.nuget]                 | OfX-RabbitMq is an extension package for OfX that leverages RabbitMq for efficient data transportation.                 | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.RabbitMq/README.md)            |

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
