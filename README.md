# OfX

OfX is an open-source, which focus on Attribute-based Data Mapping, simplifying data handling across services and enhancing maintainability.

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
    cfg.AddReceivedPipelines(c =>
    {
        c.OfType(typeof(GenericPipeline<>)).OfType<OtherPipeline>();
    });
});
```

### Function Descriptions
#### AddAttributesContainNamespaces

Registers assemblies that contain the attributes, used by OfX for data mapping.

The Attribute should be inherited from `OfXAttribute` and will be scanned!

Parameters:
`Assembly`: The assembly containing the (OfX) attributes.

#### AddHandlersFromNamespaceContaining

Add assemblies that contain handlers responsible for processing queries or commands for data retrieval.

Handlers are the execution units that resolve attributes applied to models.

If this function is not called. The default value `ItemsResponse<OfXDataResponse>` is returned!

Parameters:
`Type`: A marker type within the assembly that includes the handler implementations.
Example:

```csharp
cfg.AddHandlersFromNamespaceContaining<SomeHandlerAssemblyMarker>();
```

Here, `AddHandlersFromNamespaceContaining` is a type within the assembly where your handler logic resides.

#### AddReceivedPipelines

When you want to create some pipelines to handle the request of some attribute.
Parameters:
`Action<ReceivedPipeline>`: add the pipelines.
Example:
```csharp
cfg.AddReceivedPipelines(c =>
    {
        c.OfType(typeof(GenericPipeline<>)).OfType<OtherPipeline>();
    });
```

### 2. Integrate the Attribute into Your Model, Entity, or DTO
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

    // Add other properties as needed
}
```

### 3. Write a Handler in Your Service to Fetch the Data when you are using OfX only. If you use OfX-gRPC or other transport data layer(next version extension packages), there are no need to create Handlers anymore!
Implement a handler to process data requests. For example:
```csharp
public class UserRequestHandler(): IMappableRequestHandler<UserOfAttribute>
{
    public async Task<ItemsResponse<OfXDataResponse>> RequestAsync(RequestContext<UserOfAttribute> request)
    {
        // Implement data fetching logic here (e.g., via REST, RPC, or gRPC)
    }
}
```

Enjoy your moment!

| Package Name                                             | Description                                                                                     | .NET Version | Document                                                                                 |
|----------------------------------------------------------|-------------------------------------------------------------------------------------------------|--------------|------------------------------------------------------------------------------------------|
| [OfX](https://www.nuget.org/packages/OfX/)               | OfX core                                                                                        | 8.0, 9.0     | This Document                                                                            |
| [OfX-EFCore](https://www.nuget.org/packages/OfX-EFCore/) | This is the OfX extension package using EntityFramework to fetch data                           | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.EntityFrameworkCore/README.md) |
| [OfX-gRPC](https://www.nuget.org/packages/OfX-gRPC/)     | OfX.gRPC is an extension package for OfX that leverages gRPC for efficient data transportation. | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.Grpc/README.md)                |
| [OfX-Nats](https://www.nuget.org/packages/OfX-Nats/)     | OfX-Nats is an extension package for OfX that leverages Nats for efficient data transportation. | 8.0, 9.0     | [ReadMe](https://github.com/quyvu01/OfX/blob/main/src/OfX.Nats/README.md)                |