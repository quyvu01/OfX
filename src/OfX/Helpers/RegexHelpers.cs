using System.Text.RegularExpressions;
using OfX.Exceptions;

namespace OfX.Helpers;

public static partial class RegexHelpers
{
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