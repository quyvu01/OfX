namespace OfX.MongoDb.Extensions;

using MongoDB.Bson;
using System.Text.RegularExpressions;

public static class ExpressionToBsonBuilder
{
    private static readonly Regex ArrayPattern = new(
        @"^(?<name>\w+)\[(?:(?<offset>-?\d+)(?:\s+(?<limit>\d+))?\s+)?(?<order>asc|desc)\s+(?<sortField>\w+)\]$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static BsonDocument BuildProjectionDocument(Dictionary<string, string> expressions)
    {
        var doc = new BsonDocument();
        foreach (var kvp in expressions) doc.Add(kvp.Key, BuildBsonValue(kvp.Value));
        return doc;
    }

    private static BsonValue BuildBsonValue(string expression)
    {
        var segments = expression.Split('.');
        BsonValue current = null;

        foreach (var segment in segments)
        {
            var match = ArrayPattern.Match(segment);
            if (match.Success)
            {
                var arrayName = match.Groups["name"].Value;
                var sortField = match.Groups["sortField"].Value;
                var order = match.Groups["order"].Value.ToLower() == "desc" ? -1 : 1;
                var offset = match.Groups["offset"].Success ? int.Parse(match.Groups["offset"].Value) : (int?)null;
                var limit = match.Groups["limit"].Success ? int.Parse(match.Groups["limit"].Value) : (int?)null;

                var sortStage = new BsonDocument("$sortArray", new BsonDocument
                {
                    { "input", $"${arrayName}" },
                    { "sortBy", new BsonDocument(sortField, order) }
                });

                BsonValue arrayValue = sortStage;
                
                arrayValue = (offset.HasValue && limit.HasValue) switch
                {
                    true => new BsonDocument("$slice", new BsonArray { arrayValue, offset.Value, limit.Value }),
                    _ => offset switch
                    {
                        0 => new BsonDocument("$first", arrayValue),
                        -1 => new BsonDocument("$last", arrayValue),
                        _ => arrayValue
                    }
                };

                current = current == null ? arrayValue : WrapGetField(current, arrayValue);
                continue;
            }

            current = current == null ? $"${segment}" : WrapGetField(current, segment);
        }

        return current;
    }

    private static BsonValue WrapGetField(BsonValue input, string field) => new BsonDocument("$getField",
        new BsonDocument
        {
            { "field", field },
            { "input", input }
        });

    private static BsonValue WrapGetField(BsonValue input, BsonValue nextStage)
    {
        // Khi nextStage là một stage aggregation, có thể wrap dưới dạng $let hoặc giữ nguyên tùy DSL mở rộng
        return nextStage; // để đơn giản: override input bằng stage tiếp theo
    }
}