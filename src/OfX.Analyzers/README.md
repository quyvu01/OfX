# OfX.Analyzers

Roslyn Code Analyzer for validating OfX Expression syntax at compile-time.

## Overview

OfX.Analyzers provides comprehensive syntax validation for OfX Expression strings used in attributes like `CountryOf`, `ProvinceOf`, `MemberOf`, etc. It catches expression syntax errors during compilation, preventing runtime failures.

## Diagnostic Rules

### OFX001: Expression Syntax Invalid

**Severity:** Error
**Category:** Syntax

This analyzer validates OfX Expression strings for correct syntax including:

#### 1. **Balanced Delimiters**
- Brackets `[]` for indexers
- Braces `{}` for projections
- Parentheses `()` for filters

#### 2. **Runtime Parameters**
- Must use format: `${variableName|defaultValue}`
- Both variable name and default value are required

#### 3. **Property Navigation**
- Projection requires dot: `Country.{Id, Name}` not `Country{Id, Name}`
- Navigation after filter requires dot: `Orders(Status = 'Done').Items` not `Orders(Status = 'Done')Items`
- Navigation after indexer requires dot: `Provinces[0 asc Name].Name` not `Provinces[0 asc Name]Name`

#### 4. **Function Validation**
- Function names must be valid (count, sum, avg, min, max, upper, lower, etc.)
- Functions requiring arguments must have them (e.g., `substring`, `replace`)

#### 5. **Computed Expressions**
- Computed expressions in projections must have alias: `{Id, (Name:upper) as UpperName}` not `{Id, (Name:upper)}`

#### 6. **Operators**
- Must use valid operators: `=`, `!=`, `>`, `<`, `>=`, `<=`, `contains`, `startswith`, `endswith`
- Logical operators: `&&`, `||`, `!`, `and`, `or`, `not`

## Examples

### ✅ Valid Expressions

```csharp
// Simple property navigation
[CountryOf(nameof(CountryName), Expression = "Country.Name")]

// Projection with proper dot
[CountryOf(nameof(CountryId), Expression = "Country.{Id, Name}")]

// Root projection
[MemberOf(nameof(UserId), Expression = "{Id, Name, Email}")]

// Projection with alias
[MemberOf(nameof(Data), Expression = "{Id, Country.Name as CountryName}")]

// Filter with proper navigation
[OrderOf(nameof(OrderId), Expression = "Orders(Status = 'Done').Items")]

// Indexer with proper navigation
[ProvinceOf(nameof(ProvinceName), Expression = "Provinces[0 asc Name].Name")]

// Complex filter with string operations
[ProvinceOf(nameof(ProvinceId), Expression = "Provinces(Name endswith 'a')[0 desc Name].Name")]

// Runtime parameters (both variable and default required)
[MemberOf(nameof(UserId), Expression = "Users[${Skip|0} ${Take|10} asc Email]")]

// Multiple runtime parameters
[MemberOf(nameof(Data), Expression = "Users(Age > ${MinAge|18})[${Skip|0} ${Take|10} asc Email].{Id, Name}")]

// Functions with arguments
[MemberOf(nameof(ShortName), Expression = "{Id, Name:substring(0, 3) as Short}")]

// Computed expressions with alias
[CountryOf(nameof(Data), Expression = "{Id, (Name:upper) as UpperName}")]

// Ternary with alias
[CountryOf(nameof(Status), Expression = "{Id, (Active = true ? 'Yes' : 'No') as StatusText}")]
```

### ❌ Invalid Expressions (Analyzer Errors)

```csharp
// Missing closing brace
[CountryOf(nameof(CountryId), Expression = "Country.{Id, Name")]
// Error: Expected '}' after projection

// Missing closing bracket
[ProvinceOf(nameof(ProvinceId), Expression = "Provinces[asc Name")]
// Error: Expected ']' after indexer

// Missing closing parenthesis
[OrderOf(nameof(OrderId), Expression = "Orders(Status = 'Done'")]
// Error: Expected ')' after filter condition

// Missing dot before projection
[CountryOf(nameof(CountryId), Expression = "Country{Id, Name}")]
// Error: Projection requires '.' before '{'

// Missing dot after filter
[OrderOf(nameof(OrderId), Expression = "Orders(Status = 'Done')Items")]
// Error: Property navigation requires '.' before identifier

// Missing dot after indexer
[ProvinceOf(nameof(ProvinceName), Expression = "Provinces[0 asc Name]Name")]
// Error: Property navigation requires '.' before identifier

// Invalid runtime parameter (missing default)
[MemberOf(nameof(UserId), Expression = "Users[${Skip} ${Take|10} asc Email]")]
// Error: Runtime parameter must have format ${variable|defaultValue}

// Missing runtime parameter closing brace
[MemberOf(nameof(UserId), Expression = "Users[${Skip|0 asc Email]")]
// Error: Expected '}' after runtime parameter

// Unknown function
[CountryOf(nameof(Name), Expression = "Name:invalid")]
// Error: Unknown function 'invalid'

// Function missing required arguments
[CountryOf(nameof(Name), Expression = "Name:substring")]
// Error: Function 'substring' requires arguments

// Computed expression without alias
[CountryOf(nameof(Data), Expression = "{Id, (Name:upper)}")]
// Error: Expected 'as' keyword after computed expression - alias is required

// Invalid operator
[MemberOf(nameof(UserId), Expression = "Users(Age >> 18)")]
// Error: Expected value ('>>' is not a valid operator)
```

## Installation

### Via NuGet Package

```bash
dotnet add package OfX-Analyzers
```

### Via Project Reference

Add to your `.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="../path/to/OfX.Analyzers/OfX.Analyzers.csproj" />
</ItemGroup>
```

## How It Works

1. **Compile-Time Analysis:** The analyzer runs during compilation and inspects all attributes ending with "Of"
2. **Expression Parsing:** Each `Expression` parameter is validated using the full OfX ExpressionParser
3. **Immediate Feedback:** Syntax errors are reported as compiler errors with detailed messages including position information

## Features

### ✅ Current Validations

- **Syntax Structure:** All brackets, braces, parentheses must be balanced
- **Runtime Parameters:** Format validation for `${variable|defaultValue}`
- **Navigation Syntax:** Dot requirements for property navigation
- **Function Names:** Validates against known function list
- **Function Arguments:** Ensures required arguments are provided
- **Operator Validation:** Only valid operators are allowed
- **Alias Requirements:** Computed expressions must have aliases in projections
- **Position Information:** Error messages include exact position of syntax errors

### ❌ Current Limitations

This is a **syntax-only analyzer**. It does NOT validate:
- Whether property names exist in your entities
- Whether navigation paths are valid for your data model
- Data types or type compatibility
- Semantic correctness of expressions

These semantic validations happen at runtime when expressions are executed.

## Demo Project

See the demo project at `sample/Service1.Contract` for comprehensive examples of both valid and invalid expressions.

```bash
cd sample/Service1.Contract
dotnet build
```

The demo build will show analyzer errors for all invalid expressions.

## Error Message Format

```
error OFX001: Expression '{expression}' is invalid: {detailed-error-message}
```

Example:
```
error OFX001: Expression 'Country{Id, Name}' is invalid: Projection requires '.' before '{' at position 7
```

## Supported OfX Expression Features

- Property navigation: `Country.Name`, `Country.Province.Name`
- Projections: `Country.{Id, Name}`, `{Id, Name as UserName}`
- Root projections: `{Id, Email, Country.Name as CountryName}`
- Filters: `Countries(Active = true)`, `Provinces(Name contains 'land')`
- Indexers: `Provinces[0 10 asc Name]`, `Items[${Skip|0} ${Take|10} desc Price]`
- Functions: `Name:upper`, `Price:round(2)`, `Items:count`
- String functions: `upper`, `lower`, `trim`, `substring`, `replace`, `concat`, `split`
- Math functions: `round`, `floor`, `ceil`, `abs`, `add`, `subtract`, `multiply`, `divide`
- Date functions: `year`, `month`, `day`, `format`
- Aggregate functions: `count`, `sum`, `avg`, `min`, `max`, `distinct`
- Collection functions: `any`, `all`, `groupBy`
- Boolean functions: `any(condition)`, `all(condition)`
- Ternary operator: `(Status = 'Active' ? 'Yes' : 'No') as StatusText`
- Null coalescing: `(Nickname ?? Name) as DisplayName`
- Runtime parameters: `${variableName|defaultValue}`
- Chained functions: `Name:trim:upper`, `Price:add(10):round(2)`

## Contributing

Found a bug or want to contribute? Please visit [GitHub Repository](https://github.com/quyvu01/OfX).

## License

This project is licensed under the Apache-2.0 license.
