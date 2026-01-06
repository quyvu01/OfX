# Expression plan!

## Phase 1 - Core

- Conditional (??, ? :) - null/fallback
- Aggregate (:count, :sum, :avg, :min, :max) - collections calculation
- Filter ([condition]) - filter

## Phase 2 - Transformation

- Select Multiple ({A, B, C}) - grab multiple fields
- String Functions (:upper, :lower, :trim, :substring) - Transform strings
- Any/All (:any, :all) - Boolean checks

## Phase 3 - Advanced

- Date Functions (:format, :year, :daysAgo) - DateTime operations
- Math Operations (+, -, *, /, :round) - Arithmetic
- Distinct (:distinct) - Unique values
- Group By (:groupBy) - Aggregation với grouping

## Expression

- runtime_parameter → parsed to expression. So we just need to care about final expression, they will be consumed by
  EfCore or MongDb
- null
- "Field"
- "NavigatorA.NavigatorB.FieldC"
- "Items[asc|desc FieldName]"
- "Items[0 asc|desc FieldName]"
- "Items[-1 asc|desc FieldName]"
- "Items[skip take asc|desc FieldName]"
- Next thing. Let try with [condition]. But seems we need to re-structure the current expression parsed.
- Let thing about this one:

    1. Expression resolver?
    2. Example, if we have the expression like this one: "Country.Provinces[Name:count > 3][0 10 asc Name].{Id, Name,
       Description}" - The first entity is National? It can be:
       x => x.Country.Provinces.Where(a => a.Name.Length > 3)
       .OrderBy(a => a.Name).Skip(0)
       .Take(10).Select(a => new {a.Id, a.Name, a.Description}). How can we do?
    3. Split by '.' first.
    4. Then extract the Expression.
    5. Standardize the function like :count(present for .Count, .Lenght, .Count())...
    6. Try to use expression parsed from DynamicExpression or try to think more about expression