using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace OfX.DynamicExpression;

internal class Detector(ParserSettings settings)
{
    private static readonly Regex RootIdentifierDetectionRegex =
        new(@"(?<=[^\w@]|^)(?<id>@?[\p{L}\p{Nl}_][\p{L}\p{Nl}\p{Nd}\p{Mn}\p{Mc}\p{Pc}\p{Cf}_]*)",
            RegexOptions.Compiled);

    private static readonly string Id = RootIdentifierDetectionRegex.ToString();
    private static readonly string Type = Id.Replace("<id>", "<type>");

    private static readonly Regex LambdaDetectionRegex =
        new($@"(\((((?<withtype>({Type}\s+)?{Id}))(\s*,\s*)?)+\)|(?<withtype>{Id}))\s*=>",
            RegexOptions.Compiled);

    private static readonly Regex StringDetectionRegex = new(@"(?<!\\)?"".*?(?<!\\)""", RegexOptions.Compiled);

    private static readonly Regex CharDetectionRegex = new(@"(?<!\\)?'.{1,2}?(?<!\\)'", RegexOptions.Compiled);

    public IdentifiersInfo DetectIdentifiers(string expression, DetectorOptions option)
    {
        expression = PrepareExpression(expression);

        var unknownIdentifiers = new HashSet<string>(settings.KeyComparer);
        var knownIdentifiers = new HashSet<Identifier>();
        var knownTypes = new HashSet<ReferenceType>();

        // find lambda parameters
        var lambdaParameters = new Dictionary<string, Identifier>();
        foreach (Match match in LambdaDetectionRegex.Matches(expression))
        {
            var withTypes = match.Groups["withtype"].Captures;
            var types = match.Groups["type"].Captures;
            var identifiers = match.Groups["id"].Captures;

            // match identifier with its type
            var t = 0;
            for (var i = 0; i < withTypes.Count; i++)
            {
                var withType = withTypes[i].Value;
                var identifier = identifiers[i].Value;
                var type = typeof(object);
                if (withType != identifier)
                {
                    var typeName = types[t].Value;
                    if (settings.KnownTypes.TryGetValue(typeName, out var knownType))
                        type = knownType.Type;

                    t++;
                }

                // there might be several lambda parameters with the same name
                //  -> in that case, we ignore the detected type
                if (lambdaParameters.TryGetValue(identifier, out var already) &&
                    already.Expression.Type != type)
                    type = typeof(object);

                var defaultValue = type.IsValueType ? Activator.CreateInstance(type) : null;
                lambdaParameters[identifier] = new Identifier(identifier, Expression.Constant(defaultValue, type));
            }
        }

        foreach (Match match in RootIdentifierDetectionRegex.Matches(expression))
        {
            var idGroup = match.Groups["id"];
            var identifier = idGroup.Value;

            if (IsReservedKeyword(identifier))
                continue;

            if (option == DetectorOptions.None && idGroup.Index > 0)
            {
                var previousChar = expression[idGroup.Index - 1];

                // don't consider member accesses as identifiers (e.g. "x.Length" will only return x but not Length)
                if (previousChar == '.')
                    continue;

                // don't consider number literals as identifiers
                if (char.IsDigit(previousChar))
                    continue;
            }

            if (settings.Identifiers.TryGetValue(identifier, out var knownIdentifier))
                knownIdentifiers.Add(knownIdentifier);
            else if (lambdaParameters.TryGetValue(identifier, out var knownLambdaParam))
                knownIdentifiers.Add(knownLambdaParam);
            else if (settings.KnownTypes.TryGetValue(identifier, out var knownType))
                knownTypes.Add(knownType);
            else
                unknownIdentifiers.Add(identifier);
        }

        return new IdentifiersInfo(unknownIdentifiers, knownIdentifiers, knownTypes);
    }

    private static string PrepareExpression(string expression)
    {
        expression = expression ?? string.Empty;

        expression = RemoveStringLiterals(expression);

        expression = RemoveCharLiterals(expression);

        return expression;
    }

    private static string RemoveStringLiterals(string expression) => StringDetectionRegex.Replace(expression, "");

    private static string RemoveCharLiterals(string expression) => CharDetectionRegex.Replace(expression, "");

    private bool IsReservedKeyword(string identifier) =>
        ParserConstants.ReservedKeywords.Contains(identifier, settings.KeyComparer);
}