using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using OfX.Expressions.Parsing;

namespace OfX.Analyzers;

/// <summary>
/// Analyzer is used to validate OfX Expression parameters syntax.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public partial class OfXExpressionSyntaxAnalyzer : DiagnosticAnalyzer
{
    // Rule ID and metadata
    public const string DiagnosticId = "OFX001";
    private const string Category = "Syntax";

    private static readonly LocalizableString Title = "OfX Expression syntax is invalid";

    private static readonly LocalizableString MessageFormat = "Expression '{0}' is invalid: {1}";

    private static readonly LocalizableString Description =
        "OfX Expression must follow valid syntax rules including balanced brackets, braces, parentheses, runtime parameters, and proper use of operators.";

    //Diagnostic Rule definition
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Description);

    // Analyzer must be implemented 2 this members
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        // Do not analyze generated code
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        // Enable concurrent execution for better performance
        context.EnableConcurrentExecution();

        // Register callback: Run the analysis when meet OfX Attribute
        context.RegisterSyntaxNodeAction(AnalyzeAttribute, SyntaxKind.Attribute);
    }

    private static void AnalyzeAttribute(SyntaxNodeAnalysisContext context)
    {
        var attributeSyntax = (AttributeSyntax)context.Node;

        // Get semantic information about the attribute
        var attributeSymbol = context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol;
        if (attributeSymbol is not IMethodSymbol constructorSymbol) return;

        var attributeType = constructorSymbol.ContainingType;

        // Check if this is an OfX attribute using semantic analysis (preferred)
        // or fallback to naming convention (for test scenarios where OfX types are not available)
        if (!IsOfXAttribute(attributeType)) return;

        // Find argument named "Expression"
        var expressionArgument = attributeSyntax.ArgumentList?.Arguments
            .FirstOrDefault(arg => arg.NameEquals?.Name.Identifier.Text == "Expression");

        // Fetch the Expression value (string literal)
        if (expressionArgument?.Expression is not LiteralExpressionSyntax literalExpression) return;

        var expressionValue = literalExpression.Token.ValueText;
        if (string.IsNullOrWhiteSpace(expressionValue)) return;

        // VALIDATE: Using ExpressionParser to validate the expression
        var validationError = ValidateExpressionSyntax(expressionValue);

        if (validationError == null) return;
        // Create diagnostic error
        var diagnostic = Diagnostic.Create(Rule, literalExpression.GetLocation(), expressionValue,
            validationError);

        context.ReportDiagnostic(diagnostic);
    }

    /// <summary>
    /// Check if the attribute is an OfX attribute.
    /// Uses semantic analysis (preferred) and falls back to naming convention for test scenarios.
    /// </summary>
    private static bool IsOfXAttribute(INamedTypeSymbol attributeType)
    {
        // Preferred: Check if attribute inherits from OfX.Attributes.OfXAttribute
        if (InheritsFromOfXAttribute(attributeType))
            return true;

        // Fallback: Check naming convention for test scenarios where OfX assembly causes version conflicts
        // Attributes ending with "OfAttribute" are considered OfX attributes
        return attributeType.Name.EndsWith("OfAttribute", StringComparison.Ordinal);
    }

    /// <summary>
    /// Check if the attribute type inherits from OfX.Attributes.OfXAttribute
    /// </summary>
    private static bool InheritsFromOfXAttribute(INamedTypeSymbol attributeType)
    {
        var currentType = attributeType.BaseType;
        while (currentType != null)
        {
            // Check if base class is OfXAttribute in OfX.Attributes namespace
            if (currentType.Name == "OfXAttribute" &&
                currentType.ContainingNamespace?.ToDisplayString() == "OfX.Attributes")
                return true;

            currentType = currentType.BaseType;
        }

        return false;
    }

    /// <summary>
    /// Validate expression syntax using ExpressionParser.
    /// </summary>
    /// <returns>Error message if invalid, null if OK</returns>
    private static string ValidateExpressionSyntax(string expression)
    {
        try
        {
            // Step 1: Validate runtime parameters format ${variable|default}
            var runtimeParamRegex = ExpressionRegex();
            var matches = runtimeParamRegex.Matches(expression);

            foreach (Match match in matches)
            {
                var hasDefault = match.Groups["default"].Success;
                if (!hasDefault || string.IsNullOrEmpty(match.Groups["default"].Value))
                    return "Runtime parameter must have format ${variable|defaultValue}";
            }

            // Step 2: Replace runtime parameters with their default values for parsing
            var resolvedExpression = runtimeParamRegex.Replace(expression, match =>
            {
                var defaultValue = match.Groups["default"].Value;
                return defaultValue;
            });

            // Step 3: Use ExpressionParser to parse and validate the expression
            ExpressionParser.Parse(resolvedExpression);
            return null; // Parse successful, no error
        }
        catch (ExpressionParseException ex)
        {
            // Return the parser's error message
            return ex.Message;
        }
        catch (Exception ex)
        {
            // Catch any unexpected exceptions
            return $"Unexpected error: {ex.Message}";
        }
    }

    [GeneratedRegex(@"\$\{(?<parameter>[A-Za-z_][A-Za-z0-9_]*)(\|(?<default>[^}]*))?\}")]
    private static partial Regex ExpressionRegex();
}