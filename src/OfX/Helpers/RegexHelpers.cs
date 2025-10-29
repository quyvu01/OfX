using System.Text.RegularExpressions;

namespace OfX.Helpers;

public static partial class RegexHelpers
{
    public static string ResolvePlaceholders(string expression, IDictionary<string, object> parameters)
    {
        if (string.IsNullOrEmpty(expression) || parameters is null) return expression;

        return ParametersRegex.Replace(expression, match =>
        {
            var key = match.Groups[1].Value;
            if (parameters.TryGetValue(key, out var value)) return value?.ToString() ?? string.Empty;
            return match.Value;
        });
    }

    public static string ResolvePlaceholders(string expression, object parameters)
    {
        var paramAsDictionary = parameters
            .GetType()
            .GetProperties()
            .ToDictionary(p => p.Name, p => p.GetValue(parameters));
        return ResolvePlaceholders(expression, paramAsDictionary);
    }

    private static readonly Regex ParametersRegex = ExpressionParametersRegex();

    [GeneratedRegex(@"\$\{([a-zA-Z0-9_]+)\}", RegexOptions.Compiled)]
    private static partial Regex ExpressionParametersRegex();
}