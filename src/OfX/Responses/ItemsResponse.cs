namespace OfX.Responses;

/// <summary>
/// Represents a generic response containing a list of items.
/// </summary>
/// <typeparam name="T">The type of items in the response.</typeparam>
/// <param name="items">The list of items to include in the response.</param>
public sealed class ItemsResponse<T>(List<T> items) where T : class
{
    /// <summary>
    /// Gets the list of items in this response.
    /// </summary>
    public List<T> Items { get; } = items;
}