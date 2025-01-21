# OfX

OfX is an open-source, which focus on Attribute-based data mapping, streamlines data handling across services, reduces boilerplate code, and improves maintainability

[Demo Project!](https://github.com/quyvu01/TestOfX-Demo)

## Project Highlights
Attribute-based Data Mapping in OfX is a feature that lets developers annotate properties in their data models with custom attributes. These attributes define how and from where data should be fetched, eliminating repetitive code and automating data retrieval.
For example, imagine a scenario where Service A needs a user’s name stored in Service B. With Attribute-based Data Mapping, Service A can define a UserName property annotated with `[UserOf(nameof(UserId))]`. This tells the system to automatically retrieve the UserName based on UserId, without writing custom code each time.

Example:

```csharp
public sealed class SomeDataResponse
{
    public string Id { get; set; }
    public string UserId { get; set; }
    [UserOf(nameof(UserId), Expression = "Email")]
    public string UserEmail { get; set; }
    [UserOf(nameof(UserId))] public string UserName { get; set; }
    ...
}
```
The `[UserOf]` annotation acts as a directive to automatically retrieve `UserName` based on `UserId`,you can also fetch custom fields as `Email` on the User Table using Expression like `[UserOf(nameof(UserId), Expression="Email")]`. This eliminates the need for manual mapping logic, freeing developers to focus on core functionality rather than data plumbing.

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
});
```
StronglyTypeIdRegister Example:
```csharp
// StronglyTypeIdRegisters example:
public sealed class StronglyTypeIdRegisters : IStronglyTypeConverter<UserId>
{
    public UserId Convert(string input) => new UserId(input);
    public bool CanConvert(string input) => true;
}

// You can also implement many IStronglyTypeConverter<T>
```

### Function Descriptions
#### AddAttributesContainNamespaces

Registers assemblies that contain the attributes, used by OfX for data mapping.

The Attribute must be inherited from `OfXAttribute` and they will be scanned by OfX!

Parameters:
`Assembly`: The assembly containing the (OfX) attributes.

#### AddHandlersFromNamespaceContaining

Add assemblies that contain handlers responsible for processing queries or commands for data retrieval.

Handlers are the execution units that resolve attributes applied to models.

If this function is not invoked. The default value `ItemsResponse<OfXDataResponse>` is returned!

Parameters:
`Type`: A marker type within the assembly that includes the handler implementations.
Example:

```csharp
cfg.AddHandlersFromNamespaceContaining<SomeHandlerAssemblyMarker>();
```

Here, `AddHandlersFromNamespaceContaining` is a type within the assembly where your handler logic resides.

#### AddReceivedPipelines

When you want to create pipelines to handle the received request for `OfXAttribute`. You should use it on the server, where you fetching and response to client!

Parameters:
`Action<ReceivedPipeline>`: add the pipelines.

Example:

```csharp
    cfg.AddSendPipelines(c => c.OfType(typeof(GenericSendPipeline<>).OfType(typeof(OtherSendPipeline<>)));    
```

#### AddSendPipelines

When you want to create pipelines to handle the send request for `OfXAttribute`. You should use it on the client, where you send request to get data!

Parameters:
`Action<SendPipeline>`: add the pipelines.

Example:

```csharp
cfg.AddReceivedPipelines(c => c.OfType(typeof(GenericPipeline<>)).OfType<OtherPipeline>());
```

#### AddStronglyTypeIdConverter
When your models(entities) are using Strongly Type Id, you have to configure to tell how OfX can convert from general ID type(string) to your strongly type ID.

Parameters:
`Action<StronglyTypeIdRegister>` the strongly type ID register delegate.

#### OfType
You have to create a class and implement interface `IStronglyTypeConverter<T>`, then you have to override 2 methods(`Convert` and `CanConvert`) to help OfX convert from general Id type(string) to your strongly type.
Please check the example above!

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
    
    [ProvinceOf(nameof(ProvinceId), Order = 1)]
    public string ProvinceName { get; set; }
    
    [ProvinceOf(nameof(ProvinceId), Expression = "Country.Name", Order = 1)]
    public string CountryName { get; set; }

    [ProvinceOf(nameof(ProvinceId), Expression = "CountryId", Order = 1)]
    public string CountryId { get; set; }

    [CountryOf(nameof(CountryId), Expression = "Provinces[0 asc Name].Name", Order = 2)]
    public string Province { get; set; }
    
    // Add other properties as needed
}
```

### 3. Annotate `OfXConfigForAttribute` your models with `OfXAttribute` to then `OfX` will dynamic create relevant proxy handler for model and `OfXAttribute`

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

`Note:` If you use OfX-gRPC, OfX-Nats, OfX-RabbitMq... or other transport data layer(next version extension packages), there are no need to create Handlers anymore, they should be dynamic proxy handlers!

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

### 5. Unlock the Full Power of `Expressions` 🚀

Expressions in **OfX** enable you to fetch external data dynamically and powerfully. By leveraging these, you can go beyond default data fetching and define specific rules to access external resources effortlessly. Let’s dive into how **Expressions** work and what makes them so versatile.

#### Default Data vs. External Data
- **Default Data**: Automatically fetched using `OfX Attribute`. No `Expression` is required.
- **External Data**: Define an `Expression` to fetch specific or relational data from other tables.

Here’s how you can harness the power of **Expressions** in different scenarios:

#### Fetching Data on the Same Table
Simple case: fetching additional fields from the same table.

```csharp
public sealed class SomeDataResponse
{
    public string Id { get; set; }
    public string UserId { get; set; }

    [UserOf(nameof(UserId), Expression = "Email")]
    public string UserEmail { get; set; }

    [UserOf(nameof(UserId))]
    public string UserName { get; set; }
}
```

`User` structure:
```csharp
[OfXConfigFor<UserOfAttribute>(nameof(Id), nameof(Name))]
public sealed class User
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    ...
}
```
Generated SQL:
```SQL
 SELECT u."Id", u."Email" FROM "Users" AS u WHERE u."Id" IN (@__SomeUserIds__)
```
#### Fetching Data from Navigated Tables
Expressions also support navigation through navigated tables.

```csharp
public sealed class SomeDataResponse
{
    ...
    [UserOf(nameof(UserId), Expression = "ProvinceId")]
    public string ProvinceId { get; set; }
    
    [ProvinceOf(nameof(ProvinceId), Expression = "Country.Name", Order = 1)]
    public string CountryName { get; set; }
    ...
}    

```
In this case, `Expression = "Country.Name"` means:

- Start from the `Provinces` table.

- Navigate to the `Country` property.

- Fetch the `Name` field from the Countries table.

Structures:
```csharp
[OfXConfigFor<ProvinceOfAttribute>(nameof(Id), nameof(Name))]
public sealed class Province
{
    public ProvinceId Id { get; set; }
    public string Name { get; set; }
    public CountryId CountryId { get; set; }
    public Country Country { get; set; }
}
```

```csharp
[OfXConfigFor<CountryOfAttribute>(nameof(Id), nameof(Name))]
public class Country
{
    public CountryId Id { get; set; }
    public string Name { get; set; }
    public List<Province> Provinces { get; set; }
}
```
If the `Countries` table have the single navigator(like `Country` on the table `Provinces`) to other table, you can extend the `Expression` to *thousand kilometers :D*. Like this one: `Expression = "Country.[SingleNavigator]...[Universal]`.

Generated SQL:
```SQL
SELECT p."Id", c."Name" FROM "Provinces" AS p
LEFT JOIN "Countries" AS c ON p."CountryId" = c."Id"
WHERE p."Id" IN (@__SomeProvinceIds___)
```

#### Mapping Objects Dynamically.
```csharp
public sealed class SomeDataResponse
{
    ...
    [UserOf(nameof(UserId), Expression = "ProvinceId")]
    public string ProvinceId { get; set; }
    
    [ProvinceOf(nameof(ProvinceId), Expression = "Country", Order = 1)]
    public CountryDTO Country { get; set; }
    ...
}    
```
```csharp
public sealed class CountryDTO
{
    public string Id { get; set; }
    public string Name {get; set;}
}
```
`Note`: The DTO structure (e.g., `CountryDTO`) must match the source model's structure.
- Only properties directly on the source model (e.g., `Id`, `Name`) are selected.
- Navigators (e.g., `Provinces`) are ignored.

`Note`: When you map an object, the correlation DTO should have the same structure with `Model` like the `CountryDTO` above.

#### Array Mapping:
Unlock powerful features for mapping collections!
#### 1. All Items: [`asc|desc` `Property`]
- Retrieves all items ordered by the specified property.
- Example:
```csharp
[CountryOf(nameof(CountryId), Expression = "Provinces[asc Name]")]
public List<ProvinceDTO> Provinces { get; set; }
```

`Note`: We will retrieve all the items of a collection on navigator property, like the `Provinces` on the `Countries` table.

#### 2.Single Item: [`0|-1` `asc|desc` `Property`]example above:
- Fetches the first (`0`) or last (`-1`) item in the collection.
- Example:
```csharp
public sealed class SomeDataResponse
{
    ...
    [ProvinceOf(nameof(ProvinceId), Expression = "CountryId", Order = 1)]
    public string CountryId { get; set; }

    [CountryOf(nameof(CountryId), Expression = "Provinces[0 asc Name]", Order = 2)]
    public ProvinceDTO Province { get; set; }
    ...
}
```

When you select one item, you can navigate to the next level of the Table. Like this one:
```csharp
public sealed class SomeDataResponse
{
    ...
    [ProvinceOf(nameof(ProvinceId), Expression = "CountryId", Order = 1)]
    public string CountryId { get; set; }

    [CountryOf(nameof(CountryId), Expression = "Provinces[0 asc Name].Name", Order = 2)]
    public string ProvinceName { get; set; }
    ...
}
```

### 3.Offset & Limit: [`Offset` `Limit` `asc|desc` `Property`]
- Retrieves a slice of the collection.
- Example:

```csharp
public sealed class SomeDataResponse
{
    ...
    [ProvinceOf(nameof(ProvinceId), Expression = "CountryId", Order = 1)]
    public string CountryId { get; set; }

    [CountryOf(nameof(CountryId), Expression = "Provinces[2 10 asc Name]", Order = 2)]
    public List<ProvinceDTO> Provinces { get; set; }
    ...
}
```

#### Conclusion: 
The Expression feature in `OfX` opens up endless possibilities for querying and mapping data across complex relationships. Whether you're working with single properties, nested objects, or collections, `OfX` has you covered. Stay tuned for even more exciting updates as we expand the capabilities of `Expressions`!


That all, Enjoy your moment!

| Package Name                                                 | Description                                                                                             | .NET Version | Document                                                                                 |
|--------------------------------------------------------------|---------------------------------------------------------------------------------------------------------|--------------|------------------------------------------------------------------------------------------|
| [OfX](https://www.nuget.org/packages/OfX/)                   | OfX core                                                                                                | 8.0, 9.0     | This Document                                                                            |
| [OfX-EFCore](https://www.nuget.org/packages/OfX-EFCore/)     | This is the OfX extension package using EntityFramework to fetch data                                   | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.EntityFrameworkCore/README.md) |
| [OfX-gRPC](https://www.nuget.org/packages/OfX-gRPC/)         | OfX.gRPC is an extension package for OfX that leverages gRPC for efficient data transportation.         | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.Grpc/README.md)                |
| [OfX-Nats](https://www.nuget.org/packages/OfX-Nats/)         | OfX-Nats is an extension package for OfX that leverages Nats for efficient data transportation.         | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.Nats/README.md)                |
| [OfX-RabbitMq](https://www.nuget.org/packages/OfX-RabbitMq/) | OfX-RabbitMq is an extension package for OfX that leverages RabbitMq for efficient data transportation. | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.RabbitMq/README.md)            |
| [OfX-Kafka](https://www.nuget.org/packages/OfX-Kafka/)       | OfX-Kafka is an extension package for OfX that leverages Kafka for efficient data transportation.       | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.Kafka/README.md)               |
---