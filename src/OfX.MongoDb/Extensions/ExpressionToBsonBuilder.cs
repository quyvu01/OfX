using OfX.Helpers;

namespace OfX.MongoDb.Extensions;

using MongoDB.Bson;

/// <summary>
/// Builds MongoDB BSON projection documents from OfX expression strings.
/// </summary>
/// <remarks>
/// This builder translates OfX expression syntax into MongoDB aggregation pipeline stages,
/// supporting:
/// <list type="bullet">
///   <item><description>Simple property access (e.g., "Name")</description></item>
///   <item><description>Nested property access (e.g., "Address.City")</description></item>
///   <item><description>Array operations with sorting and slicing (e.g., "Orders[0 asc CreatedAt]")</description></item>
/// </list>
/// </remarks>
public static class ExpressionToBsonBuilder
{
    /// <summary>
    /// Builds a BSON projection document from a dictionary of expressions.
    /// </summary>
    /// <param name="expressions">Dictionary mapping field names to expression paths.</param>
    /// <returns>A BSON document suitable for MongoDB projection.</returns>
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
            var match = ExpressionHelpers.ArrayPattern.Match(segment);
            if (match.Success)
            {
                var arrayName = match.Groups["name"].Value;
                var sortField = match.Groups["orderBy"].Value;
                var order = match.Groups["orderDirection"].Value.ToLower() == "desc" ? -1 : 1;
                var offset = match.Groups["skip"].Success ? int.Parse(match.Groups["skip"].Value) : (int?)null;
                var limit = match.Groups["take"].Success ? int.Parse(match.Groups["take"].Value) : (int?)null;

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
        // When the nextStage is aggregation stage, we can wrap by using $let or keep DSL extensible
        return nextStage; // To make it easier: override input by next stage
    }
}