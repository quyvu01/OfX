using OfX.Responses;
using OfX.Serializable;

namespace OfX.Expressions.Building;

/// <summary>
/// Transforms raw projection results (object[]) into OfXDataResponse objects.
/// </summary>
public static class ProjectionTransformer
{
    /// <summary>
    /// Transforms a collection of raw projection results into OfXDataResponse objects.
    /// </summary>
    /// <param name="rawResults">The raw results from database query (each row is object[]).</param>
    /// <param name="expressions">The original expression strings (in order).</param>
    /// <returns>Collection of OfXDataResponse objects.</returns>
    public static IEnumerable<DataResponse> Transform(
        IEnumerable<object[]> rawResults,
        IReadOnlyList<string> expressions)
    {
        foreach (var row in rawResults)
            yield return TransformRow(row, expressions);
    }

    /// <summary>
    /// Transforms a single raw projection result into an OfXDataResponse.
    /// </summary>
    /// <param name="row">The raw result row (object[]).</param>
    /// <param name="expressions">The original expression strings (in order).</param>
    /// <returns>An OfXDataResponse object.</returns>
    private static DataResponse TransformRow(object[] row, IReadOnlyList<string> expressions)
    {
        // row[0] = Id
        // row[1..n] = Expression values

        var id = row[0]?.ToString() ?? string.Empty;
        var values = new ValueResponse[expressions.Count];

        for (var i = 0; i < expressions.Count; i++)
        {
            var value = row[i + 1]; // +1 because index 0 is Id
            values[i] = new ValueResponse
            {
                Expression = expressions[i],
                Value = SerializeObjects.SerializeObject(value)
            };
        }

        return new DataResponse
        {
            Id = id,
            OfXValues = values
        };
    }

    /// <summary>
    /// Transforms raw results using projection metadata for error handling.
    /// </summary>
    /// <param name="rawResults">The raw results from database query.</param>
    /// <param name="metadata">The projection metadata.</param>
    /// <returns>Collection of OfXDataResponse objects.</returns>
    public static IEnumerable<DataResponse> TransformWithMetadata(
        IEnumerable<object[]> rawResults,
        IReadOnlyList<ProjectionMetadata> metadata)
    {
        var valueMetadata = metadata.Where(m => !m.IsId).ToList();

        foreach (var row in rawResults)
        {
            yield return TransformRowWithMetadata(row, valueMetadata);
        }
    }

    /// <summary>
    /// Transforms a single row using projection metadata.
    /// </summary>
    private static DataResponse TransformRowWithMetadata(object[] row,
        IReadOnlyList<ProjectionMetadata> valueMetadata)
    {
        var id = row[0]?.ToString() ?? string.Empty;
        var values = new ValueResponse[valueMetadata.Count];

        for (var i = 0; i < valueMetadata.Count; i++)
        {
            var meta = valueMetadata[i];
            var value = row[meta.Index];

            values[i] = new ValueResponse
            {
                Expression = meta.Expression,
                Value = meta.HasError ? null : SerializeObjects.SerializeObject(value)
            };
        }

        return new DataResponse
        {
            Id = id,
            OfXValues = values
        };
    }

    /// <summary>
    /// Transforms results to an array synchronously.
    /// </summary>
    public static DataResponse[] TransformToArray(object[][] rawResults, IReadOnlyList<string> expressions)
    {
        var result = new DataResponse[rawResults.Length];
        for (var i = 0; i < rawResults.Length; i++) result[i] = TransformRow(rawResults[i], expressions);

        return result;
    }
}