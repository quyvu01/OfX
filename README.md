# OfX

```csharp
public string XId { get; set; }
[XOf(nameof(XId))] public string X { get; set; }
```

OfX is an open-source library focused on Attribute-based data mapping. It streamlines data handling across services,
reduces boilerplate code, and improves maintainability.

**[Full Documentation](https://ofxmapper.net)** | **[Getting Started](https://ofxmapper.net/docs/getting-started)** |**[Expression Language](https://ofxmapper.net/docs/expressions)**

> [!WARNING]
> All OfX* packages need to have the same version.

## Quick Start

```bash
dotnet add package OfX
```

```csharp
// 1. Configure OfX
builder.Services.AddOfX(cfg =>
{
    cfg.AddAttributesContainNamespaces(typeof(UserOfAttribute).Namespace!);
    cfg.AddModelConfigurationsFromNamespaceContaining<SomeModelAssemblyMarker>();
});

// 2. Define a custom OfXAttribute
public sealed class UserOfAttribute(string propertyName) : OfXAttribute(propertyName);

// 3. Configure the model
[OfXConfigFor<UserOfAttribute>(nameof(Id), nameof(Name))]
public sealed class User
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

// 4. Use attributes in your DTOs
public sealed class SomeDataResponse
{
    public string UserId { get; set; }

    [UserOf(nameof(UserId))]
    public string UserName { get; set; }

    [UserOf(nameof(UserId), Expression = "Email")]
    public string UserEmail { get; set; }
}
```

## Key Features

- **Attribute-based Mapping**: Declarative data fetching using custom attributes
- **Powerful Expression Language**: SQL-like DSL for complex queries, filtering, aggregation, and projections
- **Multiple Data Providers**: Support for EF Core, MongoDB, and more
- **Multiple Transports**: gRPC, NATS, RabbitMQ, Kafka, Azure Service Bus
- **GraphQL Integration**: Seamless integration with HotChocolate

## Expression Examples

```csharp
// Simple property access
[UserOf(nameof(UserId), Expression = "Email")]
public string UserEmail { get; set; }

// Navigation properties
[ProvinceOf(nameof(ProvinceId), Expression = "Country.Name")]
public string CountryName { get; set; }

// Filtering
[UserOf(nameof(UserId), Expression = "Orders(Status = 'Done')")]
public List<OrderDTO> CompletedOrders { get; set; }

// Aggregation
[UserOf(nameof(UserId), Expression = "Orders:sum(Total)")]
public decimal TotalSpent { get; set; }

// Projection
[UserOf(nameof(UserId), Expression = "{Id, Name, Address.City as CityName}")]
public UserInfo UserDetails { get; set; }

// GroupBy
[UserOf(nameof(UserId), Expression = "Orders:groupBy(Status).{Status, :count as Count}")]
public List<OrderSummary> OrdersByStatus { get; set; }
```

For complete expression syntax including filters, indexers, functions, aggregations, boolean functions, coalesce,
ternary operators, and more, visit **[Expression Documentation](https://ofxmapper.net/docs/expressions)**.

## Packages

| Package                                            | Description                      | .NET     |
|----------------------------------------------------|----------------------------------|----------|
| **Core**                                           |
| [OfX][OfX.nuget]                                   | Core library                     | 8.0, 9.0 |
| **Data Providers**                                 |
| [OfX-EFCore][OfX-EFCore.nuget]                     | Entity Framework Core provider   | 8.0, 9.0 |
| [OfX-MongoDb][OfX-MongoDb.nuget]                   | MongoDB provider                 | 8.0, 9.0 |
| **Integrations**                                   |
| [OfX-HotChocolate][OfX-HotChocolate.nuget]         | HotChocolate GraphQL integration | 8.0, 9.0 |
| **Transports**                                     |
| [OfX-gRPC][OfX-gRPC.nuget]                         | gRPC transport                   | 8.0, 9.0 |
| [OfX-Nats][OfX-Nats.nuget]                         | NATS transport                   | 8.0, 9.0 |
| [OfX-RabbitMq][OfX-RabbitMq.nuget]                 | RabbitMQ transport               | 8.0, 9.0 |
| [OfX-Kafka][OfX-Kafka.nuget]                       | Kafka transport                  | 8.0, 9.0 |
| [OfX-Azure.ServiceBus][OfX-Azure.ServiceBus.nuget] | Azure Service Bus transport      | 8.0, 9.0 |

## Documentation

Visit **[ofxmapper.net](https://ofxmapper.net)** for:

- [Getting Started Guide](https://ofxmapper.net/docs/getting-started)
- [Configuration Options](https://ofxmapper.net/docs/configuration)
- [Expression Language Reference](https://ofxmapper.net/docs/expressions)
- [Data Provider Setup](https://ofxmapper.net/docs/providers)
- [Transport Configuration](https://ofxmapper.net/docs/transports)
- [API Reference](https://ofxmapper.net/docs/api)

## Contributing

Contributions are welcome! Please visit our [GitHub repository](https://github.com/quyvu01/OfX) to:

- Report issues
- Submit pull requests
- Request features

## License

This project is licensed under the Apache-2.0 license.

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
