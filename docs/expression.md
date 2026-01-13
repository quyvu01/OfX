# OfX Expression Language

OfX Expression Language is a powerful expression language that allows flexible data querying and transformation. This document describes all supported syntax and features.

---

## Table of Contents

1. [Property Access](#1-property-access)
2. [Filters](#2-filters)
3. [Indexers](#3-indexers)
4. [Functions](#4-functions)
5. [Aggregations](#5-aggregations)
6. [Boolean Functions](#6-boolean-functions)
7. [Projections](#7-projections)
8. [Coalesce](#8-coalesce)
9. [Ternary Operator](#9-ternary-operator)
10. [GroupBy](#10-groupby)
11. [Literals](#11-literals)
12. [Complex Examples](#12-complex-examples)
13. [Notes](#13-notes)

---

## 1. Property Access

### Simple Properties
Access simple properties directly:
```
Name
Email
Status
```

### Null-Safe Property Access
Use `?` to return `null` instead of throwing an exception when the property is null:
```
Country?
Address?.City
```

### Nested Navigation
Access nested properties through multiple levels:
```
Country.Province.City.Name
Order.Customer.Address.ZipCode
```

### Null-Safe Navigation
Combine null-safe operator with navigation:
```
Country?.Province?.City.Name
Order?.Customer?.Email
```

---

## 2. Filters

Filters are applied to collections to narrow down results.

### Basic Syntax
```
Collection(Condition)
```

### Comparison Operators

| Operator | Syntax | Example |
|----------|--------|---------|
| Equal | `=` | `Status = 'Active'` |
| Not Equal | `!=` | `Status != 'Archived'` |
| Greater Than | `>` | `Total > 100` |
| Less Than | `<` | `Total < 50` |
| Greater or Equal | `>=` | `Score >= 90` |
| Less or Equal | `<=` | `Score <= 80` |
| Contains | `contains` | `Name contains 'John'` |
| Starts With | `startswith` | `Email startswith 'admin'` |
| Ends With | `endswith` | `Email endswith '@example.com'` |

### Logical Operators

| Operator | Alternative Syntax | Example |
|----------|-------------------|---------|
| AND | `&&`, `and`, `,` | `Status = 'Done' && Total > 100` |
| OR | `\|\|`, `or` | `Status = 'Pending' \|\| Status = 'Waiting'` |

### Filter Examples
```
Orders(Status = 'Completed')
Orders(Status = 'Done' && Total > 100)
Orders(Year = 2024, Status = 'Active')           # Comma equals AND
Orders(Status = 'Pending' || Status = 'Waiting')
Orders((Status = 'Done' && Total > 100) || Priority = 'High')
```

### Filter with Function in Condition
```
Orders(Items:count > 5)                          # Orders with more than 5 items
Users(Name:lower = 'john')                       # Case-insensitive comparison
```

---

## 3. Indexers

Indexers allow pagination and sorting on collections.

### Single Item Selection
Select a single item:
```
[skip asc|desc PropertyName]
```

### Range Selection
Select multiple items with skip and take:
```
[skip take asc|desc PropertyName]
```

### Indexer Examples
```
Orders[0 asc OrderDate]                          # First item by OrderDate ascending
Orders[-1 desc OrderDate]                        # Last item by OrderDate descending
Orders[0 10 asc OrderDate]                       # First 10 items
Orders[5 20 desc Total]                          # Skip 5, take 20 items
```

### Combining Filter and Indexer
```
Orders(Status = 'Done')[0 5 desc Total]          # Top 5 Done orders with highest Total
```

---

## 4. Functions

Functions are invoked using the syntax `:functionName` or `:functionName(args)`.

### A. String Functions

| Function | Syntax | Example | Result |
|----------|--------|---------|--------|
| Upper | `:upper` | `Name:upper` | `"JOHN"` |
| Lower | `:lower` | `Name:lower` | `"john"` |
| Trim | `:trim` | `Name:trim` | `"John"` (whitespace removed) |
| Substring | `:substring(start, length?)` | `Name:substring(0, 3)` | `"Joh"` |
| Replace | `:replace(old, new)` | `Name:replace('o', 'a')` | `"Jahn"` |
| Concat | `:concat(values...)` | `Name:concat(' ', LastName)` | `"John Doe"` |
| Split | `:split(separator)` | `Tags:split(',')` | `["a", "b", "c"]` |

### B. Date/Time Functions

| Function | Syntax | Example | Result |
|----------|--------|---------|--------|
| Year | `:year` | `CreatedAt:year` | `2024` |
| Month | `:month` | `CreatedAt:month` | `12` (1-12) |
| Day | `:day` | `CreatedAt:day` | `25` (1-31) |
| Hour | `:hour` | `CreatedAt:hour` | `14` (0-23) |
| Minute | `:minute` | `CreatedAt:minute` | `30` (0-59) |
| Second | `:second` | `CreatedAt:second` | `45` (0-59) |
| Day of Week | `:dayOfWeek` | `CreatedAt:dayOfWeek` | `1` (Sun=0, Mon=1, ..., Sat=6) |
| Days Ago | `:daysAgo` | `CreatedAt:daysAgo` | `5` (days from date to today) |
| Format | `:format(pattern)` | `CreatedAt:format('yyyy-MM-dd')` | `"2024-12-25"` |

### C. Math Functions

| Function | Syntax | Example | Description |
|----------|--------|---------|-------------|
| Round | `:round(decimals?)` | `Price:round(2)` | Round to 2 decimal places |
| Floor | `:floor` | `Price:floor` | Round down |
| Ceiling | `:ceil` | `Price:ceil` | Round up |
| Absolute | `:abs` | `Balance:abs` | Absolute value |
| Add | `:add(operand)` | `Price:add(Tax)` | Addition |
| Subtract | `:subtract(operand)` | `Price:subtract(10)` | Subtraction |
| Multiply | `:multiply(operand)` | `Price:multiply(Quantity)` | Multiplication |
| Divide | `:divide(operand)` | `Total:divide(2)` | Division |
| Modulo | `:mod(divisor)` | `Value:mod(3)` | Modulo operation |
| Power | `:pow(exponent)` | `Value:pow(2)` | Exponentiation |

> **Note:** Operand can be a number literal or property reference.

### D. Collection Functions

| Function | Syntax | Example | Description |
|----------|--------|---------|-------------|
| Distinct | `:distinct(property)` | `Items:distinct(Name)` | Get unique property values |
| Count | `:count` | `Items:count` | Count items in collection |

### Chained Functions
Functions can be chained together:
```
Name:trim:upper:substring(0, 3)                  # "  john  " → "JOH"
Price:add(Tax):round(2)                          # (Price + Tax) rounded to 2 decimals
CreatedAt:year:add(1)                            # Year + 1
Items:distinct(Category):count                   # Count unique categories
```

---

## 5. Aggregations

Aggregations operate on collections and return scalar values.

| Aggregation | Syntax | Example | Description |
|-------------|--------|---------|-------------|
| Count | `:count` | `Orders:count` | Count items |
| Sum | `:sum(property)` | `Orders:sum(Total)` | Sum of values |
| Average | `:avg(property)` | `Orders:avg(Total)` | Average value |
| Minimum | `:min(property)` | `Orders:min(Total)` | Minimum value |
| Maximum | `:max(property)` | `Orders:max(Total)` | Maximum value |

### Aggregation with Filter
```
Orders(Status = 'Done'):count                    # Count Done orders
Orders(Status = 'Done'):sum(Total)               # Sum of Done orders Total
Orders(Year = 2024):avg(Total)                   # Average Total for 2024
```

---

## 6. Boolean Functions

Boolean functions return `true`/`false` and operate on collections.

### Any
Check if at least one item matches the condition:
```
Orders:any                                       # true if has any orders
Orders:any(Status = 'Done')                      # true if at least 1 order is Done
Orders:any(Total > 1000)                         # true if any order > 1000
```

### All
Check if all items match the condition:
```
Documents:all(IsApproved = true)                 # true if all are approved
Items:all(Quantity > 0)                          # true if all quantities > 0
Orders:all(Status != 'Cancelled')                # true if no orders cancelled
```

### Using in Conditions
```
Orders:any(Status = 'Overdue') ? 'Has Overdue' : 'All Current'
```

---

## 7. Projections

Projections allow selecting and transforming specific properties.

### Regular Projection
Projection from a collection:
```
Orders.{Id, Status, Total}
Orders.{Id, Customer.Name, OrderDate}
```

### Root Projection
Projection from root object (no source):
```
{Id, Name, Email}
{Id, Country.Name, Status}
```

### Aliases
Rename output properties:
```
{Id, Country.Name as CountryName, Status as OrderStatus}
Orders.{Id, Total as Amount, Customer.Name as Buyer}
```

### Functions in Projection
```
Orders.{Id, Total:round(2) as RoundedTotal, Name:upper}
{CreatedAt:format('yyyy-MM-dd') as Date, Name:trim as CleanName}
```

### Computed Expressions
Computed expressions require parentheses `()` and an alias:
```
{(Nickname ?? Name) as DisplayName}
{(Status = 'Done' ? 'Yes' : 'No') as IsDone}
{(Total:multiply(1.1)) as TotalWithTax}
Orders.{Id, (Items:count > 5 ? 'Large' : 'Small') as Size}
```

---

## 8. Coalesce

The null-coalescing operator `??` returns the first non-null value.

### Syntax
```
A ?? B                                           # A if not null, otherwise B
A ?? B ?? C ?? Default                           # Fallback chain
```

### Examples
```
Nickname ?? Name                                 # Nickname or Name
Address?.City ?? 'Unknown'                       # City or 'Unknown'
Email ?? AlternateEmail ?? 'no-email@example.com'
```

### In Projection
```
{(Nickname ?? Name) as DisplayName}
{(PrimaryPhone ?? SecondaryPhone ?? 'N/A') as Phone}
```

---

## 9. Ternary Operator

The conditional operator `? :` returns different values based on a condition.

### Syntax
```
Condition ? WhenTrue : WhenFalse
```

### Examples
```
Status = 'Active' ? 'Yes' : 'No'
Total > 100 ? 'High' : 'Low'
Orders:count > 0 ? Orders:sum(Total) : 0
```

### Nested Ternary
```
Score >= 90 ? 'A' : Score >= 80 ? 'B' : Score >= 70 ? 'C' : 'F'
```

### Supported Condition Types
- **Binary Conditions:** `Property = Value`, `Property > Value`, etc.
- **Logical Conditions:** `Cond1 && Cond2`, `Cond1 || Cond2`
- **Boolean Functions:** `Orders:any`, `Orders:any(Status = 'Done')`

### In Projection
```
{Id, (Status = 'Active' ? 'Yes' : 'No') as IsActive}
{(Total > 100 ? 'Premium' : 'Standard') as Tier}
{(Orders:any(Status = 'Overdue') ? 'Warning' : 'OK') as Alert}
```

---

## 10. GroupBy

GroupBy groups a collection by one or more properties.

### Single Key GroupBy
```
Orders:groupBy(Status)
```

### Multiple Keys GroupBy
```
Orders:groupBy(Year, Month)
Orders:groupBy(Year, Quarter, Region)
```

### GroupBy with Projection
After groupBy, use projection to access:
- **Key properties** by their names (Status, Year, Month)
- **Aggregations** directly with `:function` syntax (operates on group elements)

```
Orders:groupBy(Status).{Status, :count as Count}
Orders:groupBy(Status).{Status, :count as Count, :sum(Total) as Revenue}
```

### In Projection Context

| Expression | Description |
|-----------|-------------|
| `Status` | Group key value |
| `Year`, `Month` | Key properties (multi-key) |
| `:count` | Number of items in group |
| `:sum(Total)` | Sum of property in group |
| `:avg(Price)` | Average of property in group |
| `:min(Date)` | Minimum value in group |
| `:max(Date)` | Maximum value in group |
| `{Id, Name} as Items` | Inner projection on group elements |

### Complex GroupBy Examples
```
Orders:groupBy(Status).{
  Status,
  :count as OrderCount,
  :sum(Total) as TotalRevenue,
  :avg(Total) as AvgOrderValue
}

Sales:groupBy(Year, Quarter).{
  Year,
  Quarter,
  :count as SalesCount,
  :sum(Amount) as TotalSales,
  :avg(Amount) as AvgSale
}

Orders:groupBy(CustomerId).{
  CustomerId,
  :count as OrderCount,
  :sum(Total) as TotalSpent,
  :min(OrderDate) as FirstOrder,
  :max(OrderDate) as LastOrder
}
```

### Aliases in GroupBy Projection
```
Orders:groupBy(Status).{Status as OrderStatus, :count as Count}
Orders:groupBy(Year, Month).{Year as Yr, Month as Mo, :sum(Total) as Revenue}
```

### Computed Expressions in GroupBy
```
Orders:groupBy(Status).{
  Status,
  :count as Count,
  (:count > 10 ? 'High' : 'Low') as Volume,
  (:sum(Discount) ?? 0) as TotalDiscount
}
```

### Inner Projection in GroupBy

Use `{...} as Alias` to project specific properties from group elements.

```
# Select Id and Total from each order in the group
Orders:groupBy(Status).{Status, {Id, Total} as Items}

# Translates to:
# g.GroupBy(o => o.Status).Select(g => new { Status = g.Key, Items = g.Select(item => new { item.Id, item.Total }) })
```

#### With Aliased Properties
```
Orders:groupBy(Status).{Status, {Id as OrderId, Total as OrderTotal} as Items}
```

#### Combined with Aggregations
```
Orders:groupBy(Status).{
  Status,
  :count as Count,
  :sum(Total) as Revenue,
  {Id, CustomerName, Total} as Details
}
```

#### Multiple Keys with Inner Projection
```
Sales:groupBy(Year, Month).{
  Year,
  Month,
  :count as TransactionCount,
  {Id, Amount, Customer} as Transactions
}
```

---

## 11. Literals

Supported literal values in expressions.

| Type | Syntax | Example |
|------|--------|---------|
| String | `'text'` or `"text"` | `'Active'`, `"John Doe"` |
| Number | Decimal | `100`, `3.14`, `-5`, `0.5` |
| Boolean | `true` / `false` | `true`, `false` |
| Null | `null` | `null` |

---

## 12. Complex Examples

### Navigation + Filter + Aggregation
```
Customer.Orders(Status = 'Completed'):sum(Total)
Company.Departments(IsActive = true).Employees:count
```

### Projection with Multiple Features
```
Orders(Status = 'Done')[0 10 desc OrderDate].{
  Id,
  OrderDate:format('yyyy-MM-dd') as Date,
  Total:round(2) as Amount,
  Customer.Name as Buyer,
  (Items:count > 5 ? 'Bulk' : 'Standard') as OrderType,
  (Total > 500 ? 'High Value' : 'Regular') as ValueTier
}
```

### GroupBy with Complex Calculations
```
Sales:groupBy(Year, Quarter, Region).{
  Year,
  Quarter,
  Region,
  :count as SalesCount,
  :sum(Amount) as TotalSales,
  :avg(Amount) as AvgSale
}
```

### Root Projection with Coalesce and Ternary
```
{
  Id,
  (Nickname ?? FirstName ?? 'Anonymous') as DisplayName,
  (Address?.City ?? Address?.Country ?? 'Unknown') as Location,
  (IsActive ? 'Active' : 'Inactive') as Status,
  Orders:count as TotalOrders,
  Orders(Status = 'Done'):sum(Total) as TotalSpent
}
```

### Complex Chained Operations
```
Company.Departments(IsActive = true)[0 5 desc EmployeeCount].Employees(Role = 'Developer'):distinct(Skill)

Orders(Year = 2024, Status != 'Cancelled'):groupBy(Month).{
  Month,
  :count as OrderCount,
  :sum(Total) as Revenue,
  :avg(Total) as AvgOrder,
  (:sum(Total) > 10000 ? 'Good' : 'Low') as Performance
}
```

---

## 13. Notes

### Case Sensitivity
- **Keywords:** Case-insensitive (`groupBy`, `GROUPBY`, `GroupBy` all work)
- **Property names:** Case-sensitive (must match exactly)
- **Functions:** Case-insensitive (`:upper`, `:UPPER`, `:Upper` all work)

### Null-Safe Operator
- The `?` operator can be used on any segment in a navigation chain
- Returns `null` instead of throwing `NullReferenceException`

### Comma in Filter
- Comma `,` in filter equals `&&` (AND)
- `Orders(A = 'x', B = 'y')` = `Orders(A = 'x' && B = 'y')`

### Function Chaining
- Functions can be chained as long as output type matches input type
- Example: `:trim:upper:substring(0,3)` - trim returns string, upper accepts string, substring accepts string

### GroupBy Limits
- Maximum 5 key properties supported for multi-key groupBy
- Single-key has no limit

### Operator Precedence (lowest to highest)
1. Ternary: `? :`
2. Coalesce: `??`
3. Logical: `&&`, `||`
4. Comparison: `=`, `!=`, `>`, `<`, `>=`, `<=`
5. Navigation: `.`
6. Filter: `()`
7. Indexer: `[]`
8. Projection: `.{}`
9. Function: `:`

---

## Quick Reference

```
# Property Access
Name                                    # Simple
Country?.Name                           # Null-safe
Country.Province.City                   # Nested

# Filters
Orders(Status = 'Done')                 # Equal
Orders(Total > 100 && Status = 'Active')# Multiple conditions
Orders(Status = 'A' || Status = 'B')    # OR condition

# Indexers
Orders[0 asc Date]                      # First item
Orders[0 10 desc Total]                 # Top 10

# Functions
Name:upper                              # String function
Price:round(2)                          # Math function
CreatedAt:year                          # Date function
Items:distinct(Category)                # Collection function

# Aggregations
Orders:count                            # Count
Orders:sum(Total)                       # Sum
Orders:avg(Price)                       # Average

# Boolean Functions
Orders:any                              # Has any
Orders:any(Status = 'Done')             # Any matching
Orders:all(IsValid = true)              # All matching

# Projections
{Id, Name, Status}                      # Root projection
Orders.{Id, Total, Customer.Name}       # Collection projection
{Id, Name as FullName}                  # With alias
{(A ?? B) as Value}                     # Computed

# Coalesce
Nickname ?? Name ?? 'Unknown'

# Ternary
Status = 'Active' ? 'Yes' : 'No'

# GroupBy
Orders:groupBy(Status).{Status, :count as Count}
Orders:groupBy(Year, Month).{Year, Month, :sum(Total) as Total}
Orders:groupBy(Status).{Status, {Id, Total} as Items}   # Inner projection
```
