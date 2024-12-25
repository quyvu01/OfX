namespace OfX.Abstractions;

public interface IDataMappableService
{
    Task MapDataAsync(object value, IContext context = null);
}