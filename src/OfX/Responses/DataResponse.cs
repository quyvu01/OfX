namespace OfX.Responses;

/// <summary>
/// Represents the data response for a single entity from an OfX query.
/// </summary>
public sealed class DataResponse
{
    /// <summary>
    /// Gets or sets the string representation of the entity's identifier.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the array of property values returned for this entity.
    /// </summary>
    public ValueResponse[] OfXValues { get; set; }
}