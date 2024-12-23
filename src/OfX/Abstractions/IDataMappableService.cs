namespace OfX.Abstractions;

public interface IDataMappableService
{
    Task MapDataAsync(object value, CancellationToken token = default);
}