namespace OfX.Responses;

/// <summary>
/// Represents a generic response containing a array of items.
/// </summary>
/// <typeparam name="T">The type of items in the response.</typeparam>
/// <param name="items">The array of items to include in the response.</param>
public sealed class ItemsResponse<T>(T[] items) where T : class
{
    /// <summary>
    /// Gets the array of items in this response.
    /// </summary>
    public T[] Items { get; } = items;
}