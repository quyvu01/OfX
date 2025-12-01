using OfX.Attributes;
using OfX.Tests.Attributes;

namespace OfX.Tests.OfXTypeModels;

public class CustomAAttribute(string propertyName) : OfXAttribute(propertyName);

public class ComplexType
{
    public string Value { get; set; }
}

public class DemoClass
{
    public string SomeId { get; set; }
    [CustomA(nameof(SomeId))] public string Name { get; set; }

    public int Age { get; set; }

    public ComplexType Meta { get; set; }

    public string NoAttribute { get; set; }
}

public class EmptyClass
{
    public int A { get; set; }
    public string B { get; set; }
}

public class MemberResponse
{
    public string UserId { get; set; }

    [UserOf(nameof(UserId))] public string UserName { get; set; }

    [UserOf(nameof(UserId), Expression = "${UserAlias|Email}")]
    public string UserEmail { get; set; }

    [UserOf(nameof(UserId), Expression = "CustomExpression")]
    public string UserCustomExpression { get; set; }

    [UserOf(nameof(UserId), Expression = "ProvinceId")]
    public string ProvinceId { get; set; }

    [ProvinceOf(nameof(ProvinceId))] public string ProvinceName { get; set; }
}