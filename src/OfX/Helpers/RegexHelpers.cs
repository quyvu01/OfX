using System.Text.RegularExpressions;
using OfX.Exceptions;

namespace OfX.Helpers;

public static partial class RegexHelpers
{
    public static string ResolvePlaceholders(string expression, IDictionary<string, object> parameters)
    {
        if (expression is null) return null;
        return ParametersRegex.Replace(expression, match =>
        {
            var hasParameter = match.Groups["parameter"].Success;
            if (!hasParameter) return expression;
            var parameter = match.Groups["parameter"].Value;
            var hasDefault = match.Groups["default"].Success;
            if (!hasDefault) throw new OfXException.InvalidParameter(expression);
            var fallback = match.Groups["default"].Value;

            if (parameters != null && parameters.TryGetValue(parameter, out var value) && value != null)
                return value.ToString();

            return fallback;
        });
    }

    private static readonly Regex ParametersRegex = ExpressionParametersRegex();

    [GeneratedRegex(@"\$\{(?<parameter>[A-Za-z_][A-Za-z0-9_]*)(\|(?<default>[^}]*))?\}", RegexOptions.Compiled)]
    private static partial Regex ExpressionParametersRegex();
}