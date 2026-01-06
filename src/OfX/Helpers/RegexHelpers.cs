using System.Text.RegularExpressions;
using OfX.Exceptions;

namespace OfX.Helpers;

/// <summary>
/// Provides regex-based helper methods for expression processing in the OfX framework.
/// </summary>
public static partial class RegexHelpers
{
    /// <summary>
    /// Resolves parameter placeholders in an expression string.
    /// </summary>
    /// <param name="expression">The expression containing placeholders (e.g., "${param|default}").</param>
    /// <param name="parameters">Dictionary of parameter values to substitute.</param>
    /// <returns>The expression with placeholders replaced by parameter values or defaults.</returns>
    /// <exception cref="OfXException.InvalidParameter">Thrown when a placeholder has no default value and parameter is missing.</exception>
    /// <remarks>
    /// Placeholder format: <c>${parameterName|defaultValue}</c>
    /// <list type="bullet">
    ///   <item><description><c>${userId|0}</c> - Uses "0" if "userId" parameter is not provided</description></item>
    ///   <item><description><c>${status|active}</c> - Uses "active" if "status" parameter is not provided</description></item>
    /// </list>
    /// </remarks>
    public static string ResolvePlaceholders(string expression, IDictionary<string, string> parameters)
    {
        if (expression is null) return null;
        parameters ??= new Dictionary<string, string>();
        var lookupCache = new Dictionary<string, string>(parameters.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in parameters) lookupCache[kvp.Key] = kvp.Value;
        return ParametersRegex.Replace(expression, match =>
        {
            var hasParameter = match.Groups["parameter"].Success;
            if (!hasParameter) return expression;
            var parameter = match.Groups["parameter"].Value;
            var hasDefault = match.Groups["default"].Success;
            if (!hasDefault) throw new OfXException.InvalidParameter(expression);
            var fallback = match.Groups["default"].Value;
            return lookupCache.GetValueOrDefault(parameter, fallback);
        });
    }

    private static readonly Regex ParametersRegex = ExpressionParametersRegex();

    [GeneratedRegex(@"\$\{(?<parameter>[A-Za-z_][A-Za-z0-9_]*)(\|(?<default>[^}]*))?\}", RegexOptions.Compiled)]
    private static partial Regex ExpressionParametersRegex();
}