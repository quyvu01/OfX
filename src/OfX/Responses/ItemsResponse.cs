namespace OfX.Responses;

public sealed class ItemsResponse<T>(List<T> items)
    where T : class
{
    public List<T> Items { get; } = items;
}