using System;

namespace OfX.Analyzers.Demo;

// Fake attribute classes để demo analyzer
public class CountryOfAttribute : Attribute
{
    public CountryOfAttribute(string key) { }
    public string? Expression { get; set; }
}

public class ProvinceOfAttribute : Attribute
{
    public ProvinceOfAttribute(string key) { }
    public string? Expression { get; set; }
}

public class MemberOfAttribute : Attribute
{
    public MemberOfAttribute(string key) { }
    public string? Expression { get; set; }
}

public class OrderOfAttribute : Attribute
{
    public OrderOfAttribute(string key) { }
    public string? Expression { get; set; }
}

/// <summary>
/// Demo: Valid expressions - analyzer sẽ KHÔNG báo lỗi
/// </summary>
public class ValidExpressions
{
    // ✅ Simple field selector
    [CountryOf(nameof(CountryId), Expression = "Country.{Id, Name}")]
    public string CountryId { get; set; } = "";

    // ✅ Array operation
    [ProvinceOf(nameof(ProvinceId), Expression = "Provinces[asc Name]")]
    public string ProvinceId { get; set; } = "";

    // ✅ Complex: array + field selector
    [MemberOf(nameof(MemberId), Expression = "Members[0 10 asc Email].{Id, Email}")]
    public string MemberId { get; set; } = "";

    // ✅ With filter using parentheses
    [CountryOf(nameof(ActiveCountryId), Expression = "Countries(Active = true)[asc Name].{Id}")]
    public string ActiveCountryId { get; set; } = "";

    // ✅ With runtime parameters
    [MemberOf(nameof(UserId), Expression = "Users[${Skip|0} ${Take|10} asc Email]")]
    public string UserId { get; set; } = "";

    // ✅ Complex filter with string operations
    [ProvinceOf(nameof(ProvinceEndWithA), Expression = "Provinces(Name endswith 'a')[0 desc Name].Name")]
    public string ProvinceEndWithA { get; set; } = "";

    // ✅ All combined: filter + runtime params + array + field selector
    [CountryOf(nameof(ComplexId), Expression = "Countries(Active = true).Provinces[${Index|0} asc Name].{Id, Name}")]
    public string ComplexId { get; set; } = "";

    // ✅ Root projection with alias
    [MemberOf(nameof(ProjectionId), Expression = "{Id, Name, Country.Name as CountryName}")]
    public string ProjectionId { get; set; } = "";

    // ✅ Computed expression with alias
    [ProvinceOf(nameof(ComputedId), Expression = "{Id, (Name:upper) as UpperName}")]
    public string ComputedId { get; set; } = "";
}

/// <summary>
/// Demo: Invalid expressions - analyzer SẼ báo lỗi OFX001
/// </summary>
public class InvalidExpressions
{
    // ❌ Missing closing brace
    // Error: OFX001 - Expression has unbalanced braces {} - 1 unclosed brace(s)
    [CountryOf(nameof(CountryId), Expression = "Country.{Id, Name")]
    public string CountryId { get; set; } = "";

    // ❌ Missing closing bracket
    // Error: OFX001 - Expression has unbalanced brackets [] - 1 unclosed bracket(s)
    [ProvinceOf(nameof(ProvinceId), Expression = "Provinces[asc Name")]
    public string ProvinceId { get; set; } = "";

    // ❌ Closing bracket before opening
    // Error: OFX001 - Expression has unbalanced brackets [] - closing bracket before opening
    [MemberOf(nameof(MemberId), Expression = "Members]asc Email[")]
    public string MemberId { get; set; } = "";

    // ❌ Missing closing parenthesis
    // Error: OFX001 - Expression has unbalanced parentheses () - 1 unclosed parenthesis(es)
    [CountryOf(nameof(FilterErrorId), Expression = "Countries(Active = true[asc Name]")]
    public string FilterErrorId { get; set; } = "";

    // ❌ Missing closing brace in runtime parameter
    // Error: OFX001 - Expression has unbalanced runtime parameters ${} - 1 unclosed parameter(s)
    [MemberOf(nameof(RuntimeParamErrorId), Expression = "Users[${Skip|0 ${Take|10} asc Email]")]
    public string RuntimeParamErrorId { get; set; } = "";

    // ❌ Closing parenthesis before opening
    // Error: OFX001 - Expression has unbalanced parentheses () - closing parenthesis before opening
    [ProvinceOf(nameof(ParenOrderErrorId), Expression = "Provinces)Name == 'Test'(")]
    public string ParenOrderErrorId { get; set; } = "";

    // ❌ Multiple errors: missing ] and }
    // Error: OFX001 - Expression has unbalanced brackets []
    [CountryOf(nameof(ComplexErrorId), Expression = "Country.Provinces[0 asc Name.{Id")]
    public string ComplexErrorId { get; set; } = "";

    // ❌ Runtime parameter missing default value (requires ${Variable|Default})
    // Error: OFX001 - Runtime parameter must have format ${variable|defaultValue}
    [MemberOf(nameof(RuntimeParamNoDefaultId), Expression = "Users[${Skip} ${Take|10} asc Email]")]
    public string RuntimeParamNoDefaultId { get; set; } = "";

    // ❌ Computed expression without alias
    // Error: OFX001 - Computed expression in projection requires 'as' alias
    [ProvinceOf(nameof(ComputedNoAliasId), Expression = "{Id, (Name:upper)}")]
    public string ComputedNoAliasId { get; set; } = "";

    // ❌ Invalid function name
    // Error: OFX001 - Unknown function 'invalid'
    [CountryOf(nameof(InvalidFunctionId), Expression = "Name:invalid")]
    public string InvalidFunctionId { get; set; } = "";

    // ❌ Invalid operator in filter
    // Error: OFX001 - Unknown operator
    [MemberOf(nameof(InvalidOperatorId), Expression = "Users(Age >> 18)")]
    public string InvalidOperatorId { get; set; } = "";

    // ❌ Function requires argument but none provided
    // Error: OFX001 - Function requires argument
    [ProvinceOf(nameof(MissingArgId), Expression = "Name:substring")]
    public string MissingArgId { get; set; } = "";

    // ❌ Ternary expression without alias in projection
    // Error: OFX001 - Computed expression requires 'as' alias
    [CountryOf(nameof(TernaryNoAliasId), Expression = "{Id, (Status = 'Active' ? 'Yes' : 'No')}")]
    public string TernaryNoAliasId { get; set; } = "";

    // ❌ Missing dot before projection
    // Error: OFX001 - Projection requires '.' before '{'
    [CountryOf(nameof(MissingDotId), Expression = "Country{Id, Name}")]
    public string MissingDotId { get; set; } = "";

    // ❌ Missing dot after indexer
    // Error: OFX001 - Property navigation requires '.' before identifier
    [ProvinceOf(nameof(MissingDotAfterIndexerId), Expression = "Provinces[0 asc Name]Name")]
    public string MissingDotAfterIndexerId { get; set; } = "";

    // ❌ Missing dot after filter
    // Error: OFX001 - Property navigation requires '.' before identifier
    [OrderOf(nameof(MissingDotAfterFilterId), Expression = "Orders(Status = 'Done')Items")]
    public string MissingDotAfterFilterId { get; set; } = "";
}
