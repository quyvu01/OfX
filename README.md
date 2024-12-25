
# OfX

OfX is an open-source, which focus on Attribute-based Data Mapping, simplifying data handling across services and enhancing maintainability.

[Demo Project!](https://github.com/quyvu01/TestOfX-Demo)


## Project Highlights
Attribute-based Data Mapping in OfX is a feature that lets developers annotate properties in their data models with custom attributes. These attributes define how and from where data should be fetched, eliminating repetitive code and automating data retrieval.
For example, imagine a scenario where Service A needs a user’s name stored in Service B. With Attribute-based Data Mapping, Service A can define a UserName property annotated with [UserAttribute(nameof(UserId))]. This tells the system to automatically retrieve the UserName based on UserId, without writing custom code each time.

Example:

```C#
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
The [UserOfAttribute] annotation acts as a directive to automatically retrieve UserName based on UserId,you can also fetch custom fields as Email on the User Table using Expression like [UserOf(nameof(UserId), Expression="Email")]. This eliminates the need for manual mapping logic, freeing developers to focus on core functionality rather than data plumbing.

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

```C#
builder.Services.AddOfX(cfg =>
{
    cfg.RegisterContractsContainsAssemblies(typeof(SomeContractAssemblyMarker).Assembly);
    cfg.RegisterHandlersContainsAssembly<SomeHandlerAssemblyMarker>();
});
```

### 2. Create a Relevant Attribute Based on Your Use Case
Define a custom attribute, such as UserOfAttribute, to suit your specific purpose:

```C#
public sealed class UserOfAttribute(string propertyName) : OfXAttribute(propertyName);
```

### 3. Integrate the Attribute into Your Model, Entity, or DTO
Apply the attribute to your properties like this:
```C#
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

### 4. Write a Handler in Your Service to Fetch the Data
Implement a handler to process data requests. For example:
```C#
public class UserRequestHandler(IRequestClient<GetUserOfXQuery> client)
    : IMappableRequestHandler<GetUserOfXQuery, UserOfAttribute>
{
    public async Task<ItemsResponse<OfXDataResponse>> RequestAsync(
        GetUserOfXQuery request,
        CancellationToken cancellationToken = default)
    {
        // Implement data fetching logic here (e.g., via REST, RPC, or gRPC)
    }
}
```

Enjoy your moment!
