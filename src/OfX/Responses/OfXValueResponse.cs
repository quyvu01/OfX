namespace OfX.Responses;

/// <summary>
/// Represents a single property value in an OfX query response.
/// </summary>
public sealed class OfXValueResponse
{
    /// <summary>
    /// Gets or sets the expression that was used to retrieve this value.
    /// </summary>
    public string Expression { get; set; }

    /// <summary>
    /// Gets or sets the JSON-serialized value of the property.
    /// </summary>
    public string Value { get; set; }
}