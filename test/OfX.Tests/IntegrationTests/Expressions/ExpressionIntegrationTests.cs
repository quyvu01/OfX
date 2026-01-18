using Microsoft.EntityFrameworkCore;
using OfX.Expressions.Building;
using OfX.Expressions.Nodes;
using OfX.Expressions.Parsing;
using Shouldly;
using Xunit;

namespace OfX.Tests.IntegrationTests.Expressions;

/// <summary>
/// Comprehensive integration tests for OfX Expression system with in-memory DbContext.
/// Tests cover all expression features including indexers, filters, projections, functions, and complex chains.
/// </summary>
public sealed class ExpressionIntegrationTests : IDisposable
{
    private readonly ExpressionTestDbContext _dbContext;

    public ExpressionIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<ExpressionTestDbContext>()
            .UseInMemoryDatabase(databaseName: $"ExpressionTest_{Guid.NewGuid()}")
            .Options;

        _dbContext = new ExpressionTestDbContext(options);
        SeedComplexData();
    }

    #region Test Data Setup

    private void SeedComplexData()
    {
        // Create Countries with Provinces and Cities for deep navigation tests
        var countries = new List<TestCountry>
        {
            new()
            {
                Id = "country-usa",
                Name = "United States",
                Code = "US",
                Population = 331000000,
                Provinces =
                [
                    new TestProvince
                    {
                        Id = "province-ca",
                        Name = "California",
                        Code = "CA",
                        Area = 423967,
                        Cities =
                        [
                            new TestCity { Id = "city-la", Name = "Los Angeles", Population = 3900000, IsCapital = false, FoundedYear = 1781 },
                            new TestCity { Id = "city-sf", Name = "San Francisco", Population = 870000, IsCapital = false, FoundedYear = 1776 },
                            new TestCity { Id = "city-sac", Name = "Sacramento", Population = 500000, IsCapital = true, FoundedYear = 1850 }
                        ]
                    },
                    new TestProvince
                    {
                        Id = "province-ny",
                        Name = "New York",
                        Code = "NY",
                        Area = 141297,
                        Cities =
                        [
                            new TestCity { Id = "city-nyc", Name = "New York City", Population = 8400000, IsCapital = false, FoundedYear = 1624 },
                            new TestCity { Id = "city-alb", Name = "Albany", Population = 98000, IsCapital = true, FoundedYear = 1614 }
                        ]
                    },
                    new TestProvince
                    {
                        Id = "province-tx",
                        Name = "Texas",
                        Code = "TX",
                        Area = 695662,
                        Cities =
                        [
                            new TestCity { Id = "city-hou", Name = "Houston", Population = 2300000, IsCapital = false, FoundedYear = 1837 },
                            new TestCity { Id = "city-dal", Name = "Dallas", Population = 1300000, IsCapital = false, FoundedYear = 1841 },
                            new TestCity { Id = "city-aus", Name = "Austin", Population = 950000, IsCapital = true, FoundedYear = 1839 },
                            new TestCity { Id = "city-sat", Name = "San Antonio", Population = 1500000, IsCapital = false, FoundedYear = 1718 }
                        ]
                    }
                ]
            },
            new()
            {
                Id = "country-jpn",
                Name = "Japan",
                Code = "JP",
                Population = 126000000,
                Provinces =
                [
                    new TestProvince
                    {
                        Id = "province-tokyo",
                        Name = "Tokyo",
                        Code = "TK",
                        Area = 2194,
                        Cities =
                        [
                            new TestCity { Id = "city-tokyo", Name = "Tokyo", Population = 13960000, IsCapital = true, FoundedYear = 1457 },
                            new TestCity { Id = "city-shibuya", Name = "Shibuya", Population = 230000, IsCapital = false, FoundedYear = 1932 }
                        ]
                    },
                    new TestProvince
                    {
                        Id = "province-osaka",
                        Name = "Osaka",
                        Code = "OS",
                        Area = 1905,
                        Cities =
                        [
                            new TestCity { Id = "city-osaka", Name = "Osaka", Population = 2750000, IsCapital = false, FoundedYear = 1583 }
                        ]
                    }
                ]
            },
            new()
            {
                Id = "country-empty",
                Name = "Empty Country",
                Code = "XX",
                Population = 0,
                Provinces = [] // No provinces for edge case testing
            }
        };

        // Create Customers with Orders and Items for complex aggregation tests
        var customers = new List<TestCustomer>
        {
            new()
            {
                Id = "customer-john",
                Name = "John Doe",
                Email = "john@example.com",
                Age = 35,
                IsVip = true,
                CreatedAt = DateTime.Now.AddYears(-3),
                Orders =
                [
                    new TestOrder
                    {
                        Id = "order-1",
                        Status = "Completed",
                        Total = 1500.00m,
                        OrderDate = DateTime.Now.AddDays(-30),
                        Items =
                        [
                            new TestOrderItem { Id = "item-1-1", ProductName = "Laptop", Quantity = 1, Price = 1200.00m, Category = "Electronics" },
                            new TestOrderItem { Id = "item-1-2", ProductName = "Mouse", Quantity = 2, Price = 150.00m, Category = "Electronics" }
                        ]
                    },
                    new TestOrder
                    {
                        Id = "order-2",
                        Status = "Completed",
                        Total = 350.00m,
                        OrderDate = DateTime.Now.AddDays(-20),
                        Items =
                        [
                            new TestOrderItem { Id = "item-2-1", ProductName = "Book", Quantity = 5, Price = 50.00m, Category = "Books" },
                            new TestOrderItem { Id = "item-2-2", ProductName = "Pen Set", Quantity = 2, Price = 50.00m, Category = "Stationery" }
                        ]
                    },
                    new TestOrder
                    {
                        Id = "order-3",
                        Status = "Pending",
                        Total = 2500.00m,
                        OrderDate = DateTime.Now.AddDays(-5),
                        Items =
                        [
                            new TestOrderItem { Id = "item-3-1", ProductName = "Monitor", Quantity = 2, Price = 800.00m, Category = "Electronics" },
                            new TestOrderItem { Id = "item-3-2", ProductName = "Keyboard", Quantity = 1, Price = 250.00m, Category = "Electronics" },
                            new TestOrderItem { Id = "item-3-3", ProductName = "Webcam", Quantity = 1, Price = 150.00m, Category = "Electronics" },
                            new TestOrderItem { Id = "item-3-4", ProductName = "Desk Lamp", Quantity = 2, Price = 250.00m, Category = "Home" }
                        ]
                    },
                    new TestOrder
                    {
                        Id = "order-4",
                        Status = "Cancelled",
                        Total = 100.00m,
                        OrderDate = DateTime.Now.AddDays(-60),
                        Items =
                        [
                            new TestOrderItem { Id = "item-4-1", ProductName = "Notebook", Quantity = 10, Price = 10.00m, Category = "Stationery" }
                        ]
                    }
                ]
            },
            new()
            {
                Id = "customer-jane",
                Name = "Jane Smith",
                Email = "jane@example.com",
                Age = 28,
                IsVip = false,
                CreatedAt = DateTime.Now.AddYears(-1),
                Orders =
                [
                    new TestOrder
                    {
                        Id = "order-5",
                        Status = "Completed",
                        Total = 800.00m,
                        OrderDate = DateTime.Now.AddDays(-10),
                        Items =
                        [
                            new TestOrderItem { Id = "item-5-1", ProductName = "Headphones", Quantity = 1, Price = 300.00m, Category = "Electronics" },
                            new TestOrderItem { Id = "item-5-2", ProductName = "Phone Case", Quantity = 3, Price = 50.00m, Category = "Accessories" },
                            new TestOrderItem { Id = "item-5-3", ProductName = "Charger", Quantity = 2, Price = 200.00m, Category = "Electronics" }
                        ]
                    }
                ]
            },
            new()
            {
                Id = "customer-bob",
                Name = "Bob Wilson",
                Email = "bob@example.com",
                Age = 45,
                IsVip = true,
                CreatedAt = DateTime.Now.AddYears(-5),
                Orders = [] // No orders for edge case testing
            },
            new()
            {
                Id = "customer-alice",
                Name = "Alice Brown",
                Email = "alice@example.com",
                Age = 32,
                IsVip = false,
                CreatedAt = DateTime.Now.AddMonths(-6),
                Orders =
                [
                    new TestOrder
                    {
                        Id = "order-6",
                        Status = "Completed",
                        Total = 5000.00m,
                        OrderDate = DateTime.Now.AddDays(-2),
                        Items =
                        [
                            new TestOrderItem { Id = "item-6-1", ProductName = "MacBook Pro", Quantity = 1, Price = 2500.00m, Category = "Electronics" },
                            new TestOrderItem { Id = "item-6-2", ProductName = "iPad", Quantity = 1, Price = 1500.00m, Category = "Electronics" },
                            new TestOrderItem { Id = "item-6-3", ProductName = "AirPods", Quantity = 2, Price = 500.00m, Category = "Electronics" }
                        ]
                    },
                    new TestOrder
                    {
                        Id = "order-7",
                        Status = "Pending",
                        Total = 150.00m,
                        OrderDate = DateTime.Now.AddDays(-1),
                        Items =
                        [
                            new TestOrderItem { Id = "item-7-1", ProductName = "USB Cable", Quantity = 5, Price = 20.00m, Category = "Accessories" },
                            new TestOrderItem { Id = "item-7-2", ProductName = "Screen Protector", Quantity = 2, Price = 25.00m, Category = "Accessories" }
                        ]
                    }
                ]
            }
        };

        _dbContext.Countries.AddRange(countries);
        _dbContext.Customers.AddRange(customers);
        _dbContext.SaveChanges();
    }

    public void Dispose() => _dbContext.Dispose();

    #endregion

    #region Indexer Tests - All 4 Formats

    [Fact]
    public async Task Indexer_OrderOnly_AscName_ReturnsOrderedCollection()
    {
        // Format: [asc Name] - Order only without skip/take
        var node = ExpressionParser.Parse("Cities[asc Name]");
        node.ShouldBeOfType<IndexerNode>();
        var indexerNode = (IndexerNode)node;
        indexerNode.IsOrderOnly.ShouldBeTrue();

        // Execute via ProjectionBuilder
        var builder = new ProjectionBuilder<TestProvince>("Id", "Name");
        var expressions = new List<string> { "Cities[asc Name].{Id, Name}" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Provinces
            .Where(p => p.Id == "province-ca")
            .Select(projection)
            .ToArrayAsync();

        results.Length.ShouldBe(1);
        var cities = (results[0][1] as IEnumerable<Dictionary<string, object>>)!.ToList();
        cities.Count.ShouldBe(3);

        // Should be ordered by Name ascending
        cities[0]["Name"].ShouldBe("Los Angeles");
        cities[1]["Name"].ShouldBe("Sacramento");
        cities[2]["Name"].ShouldBe("San Francisco");
    }

    [Fact]
    public async Task Indexer_OrderOnly_DescPopulation_ReturnsOrderedCollection()
    {
        // Format: [desc Population] - Order only descending
        var builder = new ProjectionBuilder<TestProvince>("Id", "Name");
        var expressions = new List<string> { "Cities[desc Population].{Name, Population}" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Provinces
            .Where(p => p.Id == "province-ca")
            .Select(projection)
            .ToArrayAsync();

        var cities = (results[0][1] as IEnumerable<Dictionary<string, object>>)!.ToList();
        cities.Count.ShouldBe(3);

        // Should be ordered by Population descending
        cities[0]["Name"].ShouldBe("Los Angeles");
        ((int)cities[0]["Population"]).ShouldBe(3900000);
        cities[1]["Name"].ShouldBe("San Francisco");
        cities[2]["Name"].ShouldBe("Sacramento");
    }

    [Fact]
    public async Task Indexer_SingleItem_FirstItem_ReturnsFirstOrderedItem()
    {
        // Format: [0 asc Name] - First item after ordering
        var builder = new ProjectionBuilder<TestProvince>("Id", "Name");
        var expressions = new List<string> { "Cities[0 asc Name].{Id, Name}" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Provinces
            .Where(p => p.Id == "province-tx")
            .Select(projection)
            .ToArrayAsync();

        results.Length.ShouldBe(1);
        var firstCity = results[0][1] as Dictionary<string, object>;
        firstCity.ShouldNotBeNull();
        firstCity["Name"].ShouldBe("Austin"); // First alphabetically
    }

    [Fact]
    public async Task Indexer_SingleItem_LastItem_ReturnsLastOrderedItem()
    {
        // Format: [-1 desc Population] - Last item (using negative index)
        var builder = new ProjectionBuilder<TestProvince>("Id", "Name");
        var expressions = new List<string> { "Cities[-1 desc Population].{Name, Population}" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Provinces
            .Where(p => p.Id == "province-tx")
            .Select(projection)
            .ToArrayAsync();

        var lastCity = results[0][1] as Dictionary<string, object>;
        lastCity.ShouldNotBeNull();
        // desc Population order: Houston(2.3M), San Antonio(1.5M), Dallas(1.3M), Austin(950K)
        // Last item = Austin (smallest population)
        lastCity["Name"].ShouldBe("Austin");
    }

    [Fact]
    public async Task Indexer_Range_SkipTake_ReturnsPagedCollection()
    {
        // Format: [0 2 asc Name] - Skip 0, Take 2, ordered by Name
        var builder = new ProjectionBuilder<TestProvince>("Id", "Name");
        var expressions = new List<string> { "Cities[0 2 asc Name].{Name}" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Provinces
            .Where(p => p.Id == "province-tx")
            .Select(projection)
            .ToArrayAsync();

        var cities = (results[0][1] as IEnumerable<Dictionary<string, object>>)!.ToList();
        cities.Count.ShouldBe(2);
        cities[0]["Name"].ShouldBe("Austin");
        cities[1]["Name"].ShouldBe("Dallas");
    }

    [Fact]
    public async Task Indexer_Range_WithSkip_ReturnsPaginatedResults()
    {
        // Format: [1 2 asc Name] - Skip 1, Take 2
        var builder = new ProjectionBuilder<TestProvince>("Id", "Name");
        var expressions = new List<string> { "Cities[1 2 asc Name].{Name}" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Provinces
            .Where(p => p.Id == "province-tx")
            .Select(projection)
            .ToArrayAsync();

        var cities = (results[0][1] as IEnumerable<Dictionary<string, object>>)!.ToList();
        cities.Count.ShouldBe(2);
        // Order: Austin, Dallas, Houston, San Antonio
        // Skip 1 (Austin), Take 2 (Dallas, Houston)
        cities[0]["Name"].ShouldBe("Dallas");
        cities[1]["Name"].ShouldBe("Houston");
    }

    #endregion

    #region Filter Tests - Complex Conditions

    [Fact]
    public async Task Filter_SimpleEquals_ReturnsMatchingItems()
    {
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Orders(Status = 'Completed'):count" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-john")
            .Select(projection)
            .ToArrayAsync();

        results[0][1].ShouldBe(2); // John has 2 completed orders
    }

    [Fact]
    public async Task Filter_GreaterThan_ReturnsMatchingItems()
    {
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Orders(Total > 1000):count" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-john")
            .Select(projection)
            .ToArrayAsync();

        results[0][1].ShouldBe(2); // Orders with Total > 1000: order-1 (1500), order-3 (2500)
    }

    [Fact]
    public async Task Filter_LessThanOrEqual_ReturnsMatchingItems()
    {
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Orders(Total <= 500):count" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-john")
            .Select(projection)
            .ToArrayAsync();

        results[0][1].ShouldBe(2); // order-2 (350), order-4 (100)
    }

    [Fact]
    public async Task Filter_NotEquals_ReturnsNonMatchingItems()
    {
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Orders(Status != 'Cancelled'):count" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-john")
            .Select(projection)
            .ToArrayAsync();

        results[0][1].ShouldBe(3); // All orders except Cancelled
    }

    [Fact]
    public async Task Filter_Contains_ReturnsMatchingItems()
    {
        // Test contains filter on string property - Orders where Status contains 'end'
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Orders(Status contains 'end'):count" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-john")
            .Select(projection)
            .ToArrayAsync();

        results[0][1].ShouldBe(1); // "Pending" contains "end"
    }

    [Fact]
    public async Task Filter_StartsWith_ReturnsMatchingItems()
    {
        // Test startswith filter - Orders where Status starts with 'C'
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Orders(Status startswith 'C'):count" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-john")
            .Select(projection)
            .ToArrayAsync();

        results[0][1].ShouldBe(3); // "Completed" (2) + "Cancelled" (1)
    }

    [Fact]
    public async Task Filter_EndsWith_ReturnsMatchingItems()
    {
        // Test endswith filter - Orders where Status ends with 'ed'
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Orders(Status endswith 'ed'):count" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-john")
            .Select(projection)
            .ToArrayAsync();

        results[0][1].ShouldBe(3); // "Completed" (2) + "Cancelled" (1)
    }

    [Fact]
    public async Task Filter_AndCondition_ReturnsMatchingItems()
    {
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Orders(Status = 'Completed', Total > 500):count" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-john")
            .Select(projection)
            .ToArrayAsync();

        results[0][1].ShouldBe(1); // Only order-1 (Status=Completed AND Total=1500)
    }

    [Fact]
    public async Task Filter_OrCondition_ReturnsMatchingItems()
    {
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Orders(Status = 'Pending' || Status = 'Cancelled'):count" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-john")
            .Select(projection)
            .ToArrayAsync();

        results[0][1].ShouldBe(2); // order-3 (Pending), order-4 (Cancelled)
    }

    #endregion

    #region Aggregate Function Tests

    [Fact]
    public async Task Function_Count_ReturnsCollectionCount()
    {
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Orders:count" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-john")
            .Select(projection)
            .ToArrayAsync();

        results[0][1].ShouldBe(4);
    }

    [Fact]
    public async Task Function_Sum_ReturnsSum()
    {
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Orders:sum(Total)" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-john")
            .Select(projection)
            .ToArrayAsync();

        ((decimal)results[0][1]).ShouldBe(4450.00m); // 1500 + 350 + 2500 + 100
    }

    [Fact]
    public async Task Function_Avg_ReturnsAverage()
    {
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Orders:avg(Total)" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-john")
            .Select(projection)
            .ToArrayAsync();

        ((decimal)results[0][1]).ShouldBe(1112.50m); // 4450 / 4
    }

    [Fact]
    public async Task Function_Min_ReturnsMinimum()
    {
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Orders:min(Total)" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-john")
            .Select(projection)
            .ToArrayAsync();

        ((decimal)results[0][1]).ShouldBe(100.00m);
    }

    [Fact]
    public async Task Function_Max_ReturnsMaximum()
    {
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Orders:max(Total)" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-john")
            .Select(projection)
            .ToArrayAsync();

        ((decimal)results[0][1]).ShouldBe(2500.00m);
    }

    [Fact]
    public async Task Function_Any_ReturnsTrue_WhenMatchExists()
    {
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Orders:any(Status = 'Pending')" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-john")
            .Select(projection)
            .ToArrayAsync();

        results[0][1].ShouldBe(true);
    }

    [Fact]
    public async Task Function_Any_ReturnsFalse_WhenNoMatch()
    {
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Orders:any(Status = 'Shipped')" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-john")
            .Select(projection)
            .ToArrayAsync();

        results[0][1].ShouldBe(false);
    }

    [Fact]
    public async Task Function_All_ReturnsTrue_WhenAllMatch()
    {
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Orders:all(Total > 0)" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-john")
            .Select(projection)
            .ToArrayAsync();

        results[0][1].ShouldBe(true);
    }

    [Fact]
    public async Task Function_All_ReturnsFalse_WhenNotAllMatch()
    {
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Orders:all(Total > 200)" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-john")
            .Select(projection)
            .ToArrayAsync();

        results[0][1].ShouldBe(false); // order-4 has Total = 100
    }

    #endregion

    #region String Function Tests

    [Fact]
    public async Task Function_Upper_ReturnsUppercase()
    {
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Name:upper" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-john")
            .Select(projection)
            .ToArrayAsync();

        results[0][1].ShouldBe("JOHN DOE");
    }

    [Fact]
    public async Task Function_Lower_ReturnsLowercase()
    {
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Email:lower" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-john")
            .Select(projection)
            .ToArrayAsync();

        results[0][1].ShouldBe("john@example.com");
    }

    [Fact]
    public async Task Function_StringCount_ReturnsLength()
    {
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Name:count" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-john")
            .Select(projection)
            .ToArrayAsync();

        results[0][1].ShouldBe(8); // "John Doe".Length
    }

    #endregion

    #region Complex Chain Tests

    [Fact]
    public async Task Complex_FilterThenIndexerThenProjection()
    {
        // Orders(Status = 'Completed')[0 asc OrderDate].{Id, Total}
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Orders(Status = 'Completed')[0 asc OrderDate].{Id, Total}" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-john")
            .Select(projection)
            .ToArrayAsync();

        var firstCompletedOrder = results[0][1] as Dictionary<string, object>;
        firstCompletedOrder.ShouldNotBeNull();
        // Completed orders sorted by date: order-2 (20 days ago), order-1 (30 days ago)
        // Wait, ascending means oldest first, so order-1 (30 days ago) is first
        firstCompletedOrder["Id"].ShouldBe("order-1");
        ((decimal)firstCompletedOrder["Total"]).ShouldBe(1500.00m);
    }

    [Fact]
    public async Task Complex_FilterThenAggregate()
    {
        // Orders(Status = 'Completed'):sum(Total)
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Orders(Status = 'Completed'):sum(Total)" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-john")
            .Select(projection)
            .ToArrayAsync();

        ((decimal)results[0][1]).ShouldBe(1850.00m); // 1500 + 350
    }

    [Fact]
    public async Task Complex_FilteredCollectionMax()
    {
        // Orders(Status = 'Completed'):max(Total)
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Orders(Status = 'Completed'):max(Total)" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-john")
            .Select(projection)
            .ToArrayAsync();

        // Completed orders: order-1 (1500), order-2 (350). Max = 1500
        ((decimal)results[0][1]).ShouldBe(1500.00m);
    }

    [Fact]
    public async Task Complex_FilteredCollectionMin()
    {
        // Orders(Status = 'Completed'):min(Total)
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Orders(Status = 'Completed'):min(Total)" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-john")
            .Select(projection)
            .ToArrayAsync();

        // Completed orders: order-1 (1500), order-2 (350). Min = 350
        ((decimal)results[0][1]).ShouldBe(350.00m);
    }

    [Fact]
    public async Task Complex_FilteredProvincesCount()
    {
        // Provinces(Area > 200000):count - Texas and California
        var builder = new ProjectionBuilder<TestCountry>("Id", "Name");
        var expressions = new List<string> { "Provinces(Area > 200000):count" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Countries
            .Where(c => c.Id == "country-usa")
            .Select(projection)
            .ToArrayAsync();

        // Texas (695662), California (423967) > 200000
        results[0][1].ShouldBe(2);
    }

    [Fact]
    public async Task Complex_OrderThenTakeWithProjection()
    {
        // Provinces[0 2 desc Area].{Name, Area}
        var builder = new ProjectionBuilder<TestCountry>("Id", "Name");
        var expressions = new List<string> { "Provinces[0 2 desc Area].{Name, Area}" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Countries
            .Where(c => c.Id == "country-usa")
            .Select(projection)
            .ToArrayAsync();

        var provinces = (results[0][1] as IEnumerable<Dictionary<string, object>>)!.ToList();
        provinces.Count.ShouldBe(2);
        // Desc by Area: Texas (695662), California (423967), New York (141297)
        provinces[0]["Name"].ShouldBe("Texas");
        provinces[1]["Name"].ShouldBe("California");
    }

    [Fact]
    public async Task Complex_MultipleExpressions_MixedTypes()
    {
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string>
        {
            "Name",
            "Age",
            "IsVip",
            "Orders:count",
            "Orders(Status = 'Completed'):sum(Total)",
            "Orders:avg(Total)"
        };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-john")
            .Select(projection)
            .ToArrayAsync();

        results.Length.ShouldBe(1);
        results[0][0].ShouldBe("customer-john"); // Id
        results[0][1].ShouldBe("John Doe"); // Name
        results[0][2].ShouldBe(35); // Age
        results[0][3].ShouldBe(true); // IsVip
        results[0][4].ShouldBe(4); // Orders:count
        ((decimal)results[0][5]).ShouldBe(1850.00m); // Completed orders sum
        ((decimal)results[0][6]).ShouldBe(1112.50m); // Average of all orders: (1500+350+2500+100)/4
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task EdgeCase_EmptyCollection_CountReturnsZero()
    {
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Orders:count" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-bob") // Bob has no orders
            .Select(projection)
            .ToArrayAsync();

        results[0][1].ShouldBe(0);
    }

    [Fact]
    public async Task EdgeCase_EmptyCollection_SumReturnsZero()
    {
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Orders:sum(Total)" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-bob")
            .Select(projection)
            .ToArrayAsync();

        ((decimal)results[0][1]).ShouldBe(0m);
    }

    [Fact]
    public async Task EdgeCase_FilterReturnsEmpty_CountReturnsZero()
    {
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Orders(Status = 'NonExistent'):count" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-john")
            .Select(projection)
            .ToArrayAsync();

        results[0][1].ShouldBe(0);
    }

    [Fact]
    public async Task EdgeCase_IndexerOnEmptyCollection_ReturnsDictionaryWithNullValues()
    {
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Orders[0 asc OrderDate].{Id}" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-bob") // Bob has no orders
            .Select(projection)
            .ToArrayAsync();

        // When accessing first item of empty collection with projection, returns Dictionary with null values
        var dict = results[0][1] as Dictionary<string, object>;
        dict.ShouldNotBeNull();
        dict["Id"].ShouldBeNull();
    }

    [Fact]
    public async Task EdgeCase_EmptyProvincesCollection_CountReturnsZero()
    {
        var builder = new ProjectionBuilder<TestCountry>("Id", "Name");
        var expressions = new List<string> { "Provinces:count" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Countries
            .Where(c => c.Id == "country-empty") // No provinces
            .Select(projection)
            .ToArrayAsync();

        results[0][1].ShouldBe(0);
    }

    [Fact]
    public async Task EdgeCase_ProjectionOnEmptyCollection_ReturnsEmptyList()
    {
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Orders.{Id, Status}" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-bob")
            .Select(projection)
            .ToArrayAsync();

        var orders = (results[0][1] as IEnumerable<Dictionary<string, object>>)!.ToList();
        orders.Count.ShouldBe(0);
    }

    [Fact]
    public async Task EdgeCase_AnyOnEmptyCollection_ReturnsFalse()
    {
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Orders:any(Status = 'Completed')" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-bob")
            .Select(projection)
            .ToArrayAsync();

        results[0][1].ShouldBe(false);
    }

    [Fact]
    public async Task EdgeCase_AllOnEmptyCollection_ReturnsTrue()
    {
        // All on empty collection returns true (vacuous truth)
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Orders:all(Status = 'Completed')" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-bob")
            .Select(projection)
            .ToArrayAsync();

        results[0][1].ShouldBe(true);
    }

    #endregion

    #region Root Projection Tests

    [Fact]
    public async Task RootProjection_SimpleFields()
    {
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "{Id, Name, Email, Age}" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-jane")
            .Select(projection)
            .ToArrayAsync();

        var rootProj = results[0][1] as Dictionary<string, object>;
        rootProj.ShouldNotBeNull();
        rootProj["Id"].ShouldBe("customer-jane");
        rootProj["Name"].ShouldBe("Jane Smith");
        rootProj["Email"].ShouldBe("jane@example.com");
        rootProj["Age"].ShouldBe(28);
    }

    [Fact]
    public async Task RootProjection_WithAlias()
    {
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "{Id, Name as CustomerName, Age as CustomerAge}" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-jane")
            .Select(projection)
            .ToArrayAsync();

        var rootProj = results[0][1] as Dictionary<string, object>;
        rootProj.ShouldNotBeNull();
        rootProj["Id"].ShouldBe("customer-jane");
        rootProj["CustomerName"].ShouldBe("Jane Smith");
        rootProj["CustomerAge"].ShouldBe(28);
    }

    #endregion

    #region Coalesce and Ternary Tests

    [Fact]
    public async Task Coalesce_ReturnsFirstNonNull()
    {
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Email ?? Name" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-john")
            .Select(projection)
            .ToArrayAsync();

        results[0][1].ShouldBe("john@example.com");
    }

    [Fact]
    public async Task Ternary_ReturnsCorrectBranch_WhenTrue()
    {
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "IsVip = true ? 'VIP Customer' : 'Regular Customer'" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-john") // John is VIP
            .Select(projection)
            .ToArrayAsync();

        results[0][1].ShouldBe("VIP Customer");
    }

    [Fact]
    public async Task Ternary_ReturnsCorrectBranch_WhenFalse()
    {
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "IsVip = true ? 'VIP Customer' : 'Regular Customer'" };
        var projection = builder.Build(expressions);

        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-jane") // Jane is not VIP
            .Select(projection)
            .ToArrayAsync();

        results[0][1].ShouldBe("Regular Customer");
    }

    #endregion

    #region BuildWithMetadata Tests

    [Fact]
    public void BuildWithMetadata_ReturnsCorrectMetadata()
    {
        // Arrange
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Name", "Age", null };

        // Act
        var result = builder.BuildWithMetadata(expressions);

        // Assert
        result.Metadata.Count.ShouldBe(4); // Id + 3 expressions

        result.Metadata[0].IsId.ShouldBeTrue();
        result.Metadata[0].Index.ShouldBe(0);

        result.Metadata[1].IsId.ShouldBeFalse();
        result.Metadata[1].Expression.ShouldBe("Name");
        result.Metadata[1].Index.ShouldBe(1);

        result.Metadata[2].Expression.ShouldBe("Age");
        result.Metadata[2].Index.ShouldBe(2);

        result.Metadata[3].Expression.ShouldBeNull(); // null expression
        result.Metadata[3].Index.ShouldBe(3);
    }

    #endregion

    #region Transformer Tests

    [Fact]
    public async Task Transform_ConvertsRawResultsToOfXDataResponse()
    {
        // Arrange
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Name", "Age" };
        var projection = builder.Build(expressions);

        var rawResults = await _dbContext.Customers
            .Where(c => c.Id == "customer-john")
            .Select(projection)
            .ToArrayAsync();

        // Act
        var responses = ProjectionTransformer.TransformToArray(rawResults, expressions);

        // Assert
        responses.Length.ShouldBe(1);
        responses[0].Id.ShouldBe("customer-john");
        responses[0].OfXValues.Length.ShouldBe(2);
        responses[0].OfXValues[0].Expression.ShouldBe("Name");
        responses[0].OfXValues[1].Expression.ShouldBe("Age");
    }

    #endregion

    #region Collection Projection Tests

    [Fact]
    public async Task CollectionProjection_ReturnsProjectedProperties()
    {
        // Arrange
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Orders.{Id, Status}" };
        var projection = builder.Build(expressions);

        // Act
        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-john")
            .Select(projection)
            .ToArrayAsync();

        // Assert
        results.Length.ShouldBe(1);
        results[0][0].ShouldBe("customer-john"); // Id

        var projectedOrders = (results[0][1] as IEnumerable<Dictionary<string, object>>)!.ToList();
        projectedOrders.ShouldNotBeNull();
        projectedOrders.Count.ShouldBe(4); // John has 4 orders

        projectedOrders.ShouldContain(o => (string)o["Id"] == "order-1" && (string)o["Status"] == "Completed");
        projectedOrders.ShouldContain(o => (string)o["Id"] == "order-2" && (string)o["Status"] == "Completed");
        projectedOrders.ShouldContain(o => (string)o["Id"] == "order-3" && (string)o["Status"] == "Pending");
        projectedOrders.ShouldContain(o => (string)o["Id"] == "order-4" && (string)o["Status"] == "Cancelled");
    }

    [Fact]
    public async Task CollectionProjection_WithFilter_ReturnsFilteredProjectedProperties()
    {
        // Arrange
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Orders(Status = 'Completed').{Id, Total}" };
        var projection = builder.Build(expressions);

        // Act
        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-john")
            .Select(projection)
            .ToArrayAsync();

        // Assert
        results.Length.ShouldBe(1);
        results[0][0].ShouldBe("customer-john");

        var projectedOrders = (results[0][1] as IEnumerable<Dictionary<string, object>>)!.ToList();
        projectedOrders.ShouldNotBeNull();
        projectedOrders.Count.ShouldBe(2); // 2 completed orders

        projectedOrders.ShouldContain(o => (string)o["Id"] == "order-1" && (decimal)o["Total"] == 1500.00m);
        projectedOrders.ShouldContain(o => (string)o["Id"] == "order-2" && (decimal)o["Total"] == 350.00m);
    }

    [Fact]
    public async Task CollectionProjection_MultipleFields_ReturnsAllFields()
    {
        // Arrange
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Orders.{Id, Status, Total}" };
        var projection = builder.Build(expressions);

        // Act
        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-jane")
            .Select(projection)
            .ToArrayAsync();

        // Assert
        results.Length.ShouldBe(1);
        results[0][0].ShouldBe("customer-jane");

        var projectedOrders = (results[0][1] as IEnumerable<Dictionary<string, object>>)!.ToList();
        projectedOrders.ShouldNotBeNull();
        projectedOrders.Count.ShouldBe(1); // Jane has 1 order

        projectedOrders[0]["Id"].ShouldBe("order-5");
        projectedOrders[0]["Status"].ShouldBe("Completed");
        projectedOrders[0]["Total"].ShouldBe(800.00m);
    }

    [Fact]
    public async Task CollectionProjection_EmptyCollection_ReturnsEmptyList()
    {
        // Arrange
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { "Orders.{Id, Status}" };
        var projection = builder.Build(expressions);

        // Act
        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-bob") // Bob has no orders
            .Select(projection)
            .ToArrayAsync();

        // Assert
        results.Length.ShouldBe(1);
        results[0][0].ShouldBe("customer-bob");

        var projectedOrders = (results[0][1] as IEnumerable<Dictionary<string, object>>)!.ToList();
        projectedOrders.ShouldNotBeNull();
        projectedOrders.Count.ShouldBe(0);
    }

    #endregion

    #region Null Expression Tests

    [Fact]
    public async Task NullExpression_UsesDefaultProperty()
    {
        // Arrange
        var builder = new ProjectionBuilder<TestCustomer>("Id", "Name");
        var expressions = new List<string> { null }; // null should use defaultProperty "Name"
        var projection = builder.Build(expressions);

        // Act
        var results = await _dbContext.Customers
            .Where(c => c.Id == "customer-john")
            .Select(projection)
            .ToArrayAsync();

        // Assert
        results.Length.ShouldBe(1);
        results[0][0].ShouldBe("customer-john"); // Id
        results[0][1].ShouldBe("John Doe"); // Name (default property)
    }

    #endregion

    #region Parser Edge Cases

    [Fact]
    public void Parser_IndexerOrderOnly_ParsesCorrectly()
    {
        var node = ExpressionParser.Parse("Items[asc Name]");

        node.ShouldBeOfType<IndexerNode>();
        var indexer = (IndexerNode)node;
        indexer.IsOrderOnly.ShouldBeTrue();
        indexer.IsSingleItem.ShouldBeFalse();
        indexer.Skip.ShouldBeNull();
        indexer.Take.ShouldBeNull();
        indexer.OrderDirection.ShouldBe(OrderDirection.Asc);
        indexer.OrderBy.ShouldBe("Name");
    }

    [Fact]
    public void Parser_IndexerSingleItem_ParsesCorrectly()
    {
        var node = ExpressionParser.Parse("Items[0 desc Price]");

        node.ShouldBeOfType<IndexerNode>();
        var indexer = (IndexerNode)node;
        indexer.IsOrderOnly.ShouldBeFalse();
        indexer.IsSingleItem.ShouldBeTrue();
        indexer.Skip.ShouldBe(0);
        indexer.Take.ShouldBeNull();
        indexer.OrderDirection.ShouldBe(OrderDirection.Desc);
        indexer.OrderBy.ShouldBe("Price");
    }

    [Fact]
    public void Parser_IndexerLastItem_ParsesCorrectly()
    {
        var node = ExpressionParser.Parse("Items[-1 asc CreatedAt]");

        node.ShouldBeOfType<IndexerNode>();
        var indexer = (IndexerNode)node;
        indexer.IsLastItem.ShouldBeTrue();
        indexer.Skip.ShouldBe(-1);
    }

    [Fact]
    public void Parser_IndexerRange_ParsesCorrectly()
    {
        var node = ExpressionParser.Parse("Items[5 10 desc Total]");

        node.ShouldBeOfType<IndexerNode>();
        var indexer = (IndexerNode)node;
        indexer.IsOrderOnly.ShouldBeFalse();
        indexer.IsSingleItem.ShouldBeFalse();
        indexer.Skip.ShouldBe(5);
        indexer.Take.ShouldBe(10);
        indexer.OrderDirection.ShouldBe(OrderDirection.Desc);
        indexer.OrderBy.ShouldBe("Total");
    }

    [Fact]
    public void Parser_ComplexChain_ParsesCorrectly()
    {
        // Orders(Status = 'Done')[0 2 asc OrderDate].Items(Quantity > 1):sum(Price)
        var node = ExpressionParser.Parse("Orders(Status = 'Done')[0 2 asc OrderDate].Items(Quantity > 1):sum(Price)");

        // The structure should be: FunctionNode -> NavigationNode
        node.ShouldNotBeNull();
    }

    #endregion
}

#region Test Entities

public class TestCountry
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int Population { get; set; }
    public List<TestProvince> Provinces { get; set; } = [];
}

public class TestProvince
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int Area { get; set; }
    public string CountryId { get; set; } = string.Empty;
    public List<TestCity> Cities { get; set; } = [];
}

public class TestCity
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Population { get; set; }
    public bool IsCapital { get; set; }
    public int FoundedYear { get; set; }
    public string ProvinceId { get; set; } = string.Empty;
}

public class TestCustomer
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
    public bool IsVip { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<TestOrder> Orders { get; set; } = [];
}

public class TestOrder
{
    public string Id { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public DateTime OrderDate { get; set; }
    public List<TestOrderItem> Items { get; set; } = [];
}

public class TestOrderItem
{
    public string Id { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
}

#endregion

#region Test DbContext

public class ExpressionTestDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<TestCountry> Countries { get; set; } = null!;
    public DbSet<TestProvince> Provinces { get; set; } = null!;
    public DbSet<TestCity> Cities { get; set; } = null!;
    public DbSet<TestCustomer> Customers { get; set; } = null!;
    public DbSet<TestOrder> Orders { get; set; } = null!;
    public DbSet<TestOrderItem> OrderItems { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TestCountry>()
            .HasMany(c => c.Provinces)
            .WithOne()
            .HasForeignKey(p => p.CountryId);

        modelBuilder.Entity<TestProvince>()
            .HasMany(p => p.Cities)
            .WithOne()
            .HasForeignKey(c => c.ProvinceId);

        modelBuilder.Entity<TestCustomer>()
            .HasMany(c => c.Orders)
            .WithOne()
            .HasForeignKey(o => o.CustomerId);

        modelBuilder.Entity<TestOrder>()
            .HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId);
    }
}

#endregion
