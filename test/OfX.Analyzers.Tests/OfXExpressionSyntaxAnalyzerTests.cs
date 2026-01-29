using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<
    OfX.Analyzers.OfXExpressionSyntaxAnalyzer>;

namespace OfX.Analyzers.Tests;

public class OfXExpressionSyntaxAnalyzerTests
{
    [Fact]
    public async Task ValidExpression_SimpleProperty_NoDiagnostic()
    {
        const string testCode = """
                                using System;

                                public class CountryOfAttribute : Attribute
                                {
                                    public CountryOfAttribute(string key) { }
                                    public string Expression { get; set; }
                                }

                                public class Test
                                {
                                    [CountryOf(nameof(CountryId), Expression = "Country.Name")]
                                    public string CountryId { get; set; }
                                }
                                """;

        await VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task ValidExpression_WithProjection_NoDiagnostic()
    {
        const string testCode = """
                                using System;

                                public class CountryOfAttribute : Attribute
                                {
                                    public CountryOfAttribute(string key) { }
                                    public string Expression { get; set; }
                                }

                                public class Test
                                {
                                    [CountryOf(nameof(CountryId), Expression = "Country.{Id, Name}")]
                                    public string CountryId { get; set; }
                                }
                                """;

        await VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task ValidExpression_WithFilter_NoDiagnostic()
    {
        const string testCode = """
                                using System;

                                public class CountryOfAttribute : Attribute
                                {
                                    public CountryOfAttribute(string key) { }
                                    public string Expression { get; set; }
                                }

                                public class Test
                                {
                                    [CountryOf(nameof(CountryId), Expression = "Countries(Active = true)")]
                                    public string CountryId { get; set; }
                                }
                                """;

        await VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task ValidExpression_WithRuntimeParameters_NoDiagnostic()
    {
        const string testCode = """
                                using System;

                                public class MemberOfAttribute : Attribute
                                {
                                    public MemberOfAttribute(string key) { }
                                    public string Expression { get; set; }
                                }

                                public class Test
                                {
                                    [MemberOf(nameof(UserId), Expression = "Users[${Skip|0} ${Take|10} asc Email]")]
                                    public string UserId { get; set; }
                                }
                                """;

        await VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task InvalidExpression_MissingClosingBracket_ReportsDiagnostic()
    {
        const string testCode = """
                                using System;

                                public class CountryOfAttribute : Attribute
                                {
                                    public CountryOfAttribute(string key) { }
                                    public string Expression { get; set; }
                                }

                                public class Test
                                {
                                    [CountryOf(nameof(CountryId), Expression = {|#0:"Provinces[asc Name"|})]
                                    public string CountryId { get; set; }
                                }
                                """;

        var expected = VerifyCS.Diagnostic(OfXExpressionSyntaxAnalyzer.DiagnosticId)
            .WithLocation(0)
            .WithArguments("Provinces[asc Name", "Expected ']' after indexer. Got 'EndOfExpression' at position 18");

        await VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task InvalidExpression_MissingClosingBrace_ReportsDiagnostic()
    {
        const string testCode = """
                                using System;

                                public class CountryOfAttribute : Attribute
                                {
                                    public CountryOfAttribute(string key) { }
                                    public string Expression { get; set; }
                                }

                                public class Test
                                {
                                    [CountryOf(nameof(CountryId), Expression = {|#0:"Country.{Id, Name"|})]
                                    public string CountryId { get; set; }
                                }
                                """;

        var expected = VerifyCS.Diagnostic(OfXExpressionSyntaxAnalyzer.DiagnosticId)
            .WithLocation(0)
            .WithArguments("Country.{Id, Name", "Expected '}' after projection. Got 'EndOfExpression' at position 17");

        await VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task InvalidExpression_MissingRuntimeParameterDefault_ReportsDiagnostic()
    {
        const string testCode = """
                                using System;

                                public class MemberOfAttribute : Attribute
                                {
                                    public MemberOfAttribute(string key) { }
                                    public string Expression { get; set; }
                                }

                                public class Test
                                {
                                    [MemberOf(nameof(UserId), Expression = {|#0:"Users[${Skip} ${Take|10} asc Email]"|})]
                                    public string UserId { get; set; }
                                }
                                """;

        var expected = VerifyCS.Diagnostic(OfXExpressionSyntaxAnalyzer.DiagnosticId)
            .WithLocation(0)
            .WithArguments("Users[${Skip} ${Take|10} asc Email]", "Runtime parameter must have format ${variable|defaultValue}");

        await VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task InvalidExpression_MissingDotBeforeProjection_ReportsDiagnostic()
    {
        const string testCode = """
                                using System;

                                public class CountryOfAttribute : Attribute
                                {
                                    public CountryOfAttribute(string key) { }
                                    public string Expression { get; set; }
                                }

                                public class Test
                                {
                                    [CountryOf(nameof(CountryId), Expression = {|#0:"Country{Id, Name}"|})]
                                    public string CountryId { get; set; }
                                }
                                """;

        var expected = VerifyCS.Diagnostic(OfXExpressionSyntaxAnalyzer.DiagnosticId)
            .WithLocation(0)
            .WithArguments("Country{Id, Name}", "Projection requires '.' before '{' at position 7");

        await VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task InvalidExpression_UnknownFunction_ReportsDiagnostic()
    {
        const string testCode = """
                                using System;

                                public class CountryOfAttribute : Attribute
                                {
                                    public CountryOfAttribute(string key) { }
                                    public string Expression { get; set; }
                                }

                                public class Test
                                {
                                    [CountryOf(nameof(CountryId), Expression = {|#0:"Name:invalid"|})]
                                    public string CountryId { get; set; }
                                }
                                """;

        var expected = VerifyCS.Diagnostic(OfXExpressionSyntaxAnalyzer.DiagnosticId)
            .WithLocation(0)
            .WithArguments("Name:invalid", "Unknown function 'invalid' at position 5");

        await VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task InvalidExpression_FunctionRequiresArguments_ReportsDiagnostic()
    {
        const string testCode = """
                                using System;

                                public class CountryOfAttribute : Attribute
                                {
                                    public CountryOfAttribute(string key) { }
                                    public string Expression { get; set; }
                                }

                                public class Test
                                {
                                    [CountryOf(nameof(CountryId), Expression = {|#0:"Name:substring"|})]
                                    public string CountryId { get; set; }
                                }
                                """;

        var expected = VerifyCS.Diagnostic(OfXExpressionSyntaxAnalyzer.DiagnosticId)
            .WithLocation(0)
            .WithArguments("Name:substring", "Function 'substring' requires arguments at position 5");

        await VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task InvalidExpression_ComputedExpressionRequiresAlias_ReportsDiagnostic()
    {
        const string testCode = """
                                using System;

                                public class CountryOfAttribute : Attribute
                                {
                                    public CountryOfAttribute(string key) { }
                                    public string Expression { get; set; }
                                }

                                public class Test
                                {
                                    [CountryOf(nameof(CountryId), Expression = {|#0:"{Id, (Name:upper)}"|})]
                                    public string CountryId { get; set; }
                                }
                                """;

        var expected = VerifyCS.Diagnostic(OfXExpressionSyntaxAnalyzer.DiagnosticId)
            .WithLocation(0)
            .WithArguments("{Id, (Name:upper)}", "Expected 'as' keyword after computed expression - alias is required. Got 'CloseBrace' at position 17");

        await VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task ValidExpression_MultipleRuntimeParameters_NoDiagnostic()
    {
        const string testCode = """
                                using System;

                                public class MemberOfAttribute : Attribute
                                {
                                    public MemberOfAttribute(string key) { }
                                    public string Expression { get; set; }
                                }

                                public class Test
                                {
                                    [MemberOf(nameof(UserId), Expression = "Users(Age > ${MinAge|18})[${Skip|0} ${Take|10} asc Email].{Id, Name}")]
                                    public string UserId { get; set; }
                                }
                                """;

        await VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task ValidExpression_RootProjection_NoDiagnostic()
    {
        const string testCode = """
                                using System;

                                public class MemberOfAttribute : Attribute
                                {
                                    public MemberOfAttribute(string key) { }
                                    public string Expression { get; set; }
                                }

                                public class Test
                                {
                                    [MemberOf(nameof(UserId), Expression = "{Id, Name, Country.Name as CountryName}")]
                                    public string UserId { get; set; }
                                }
                                """;

        await VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task InvalidExpression_MissingDotAfterIndexer_ReportsDiagnostic()
    {
        const string testCode = """
                                using System;

                                public class ProvinceOfAttribute : Attribute
                                {
                                    public ProvinceOfAttribute(string key) { }
                                    public string Expression { get; set; }
                                }

                                public class Test
                                {
                                    [ProvinceOf(nameof(ProvinceId), Expression = {|#0:"Provinces[0 asc Name]Name"|})]
                                    public string ProvinceId { get; set; }
                                }
                                """;

        var expected = VerifyCS.Diagnostic(OfXExpressionSyntaxAnalyzer.DiagnosticId)
            .WithLocation(0)
            .WithArguments("Provinces[0 asc Name]Name", "Property navigation requires '.' before identifier at position 21");

        await VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task InvalidExpression_MissingDotAfterFilter_ReportsDiagnostic()
    {
        const string testCode = """
                                using System;

                                public class OrderOfAttribute : Attribute
                                {
                                    public OrderOfAttribute(string key) { }
                                    public string Expression { get; set; }
                                }

                                public class Test
                                {
                                    [OrderOf(nameof(OrderId), Expression = {|#0:"Orders(Status = 'Done')Items"|})]
                                    public string OrderId { get; set; }
                                }
                                """;

        var expected = VerifyCS.Diagnostic(OfXExpressionSyntaxAnalyzer.DiagnosticId)
            .WithLocation(0)
            .WithArguments("Orders(Status = 'Done')Items", "Property navigation requires '.' before identifier at position 23");

        await VerifyAnalyzerAsync(testCode, expected);
    }

    private static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        return VerifyCS.VerifyAnalyzerAsync(source, expected);
    }
}
