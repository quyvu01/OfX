namespace OfX.Responses;

public sealed class ItemsResponse<T>(List<T> items) where T : class
{
    public IReadOnlyCollection<T> Items { get; } = items;
}