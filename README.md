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
    cfg.AddReceivedPipelines(c => c.OfType(typeof(GenericPipeline<>).OfType<OtherPipeline>());
    
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

When you want to create pipelines to handle the request for `OfXAttribute`.

Parameters:
`Action<ReceivedPipeline>`: add the pipelines.

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

That all, Enjoy your moment!

| Package Name                                                 | Description                                                                                             | .NET Version | Document                                                                                 |
|--------------------------------------------------------------|---------------------------------------------------------------------------------------------------------|--------------|------------------------------------------------------------------------------------------|
| [OfX](https://www.nuget.org/packages/OfX/)                   | OfX core                                                                                                | 8.0, 9.0     | This Document                                                                            |
| [OfX-EFCore](https://www.nuget.org/packages/OfX-EFCore/)     | This is the OfX extension package using EntityFramework to fetch data                                   | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.EntityFrameworkCore/README.md) |
| [OfX-gRPC](https://www.nuget.org/packages/OfX-gRPC/)         | OfX.gRPC is an extension package for OfX that leverages gRPC for efficient data transportation.         | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.Grpc/README.md)                |
| [OfX-Nats](https://www.nuget.org/packages/OfX-Nats/)         | OfX-Nats is an extension package for OfX that leverages Nats for efficient data transportation.         | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.Nats/README.md)                |
| [OfX-RabbitMq](https://www.nuget.org/packages/OfX-RabbitMq/) | OfX-RabbitMq is an extension package for OfX that leverages RabbitMq for efficient data transportation. | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.RabbitMq/README.md)            |
---