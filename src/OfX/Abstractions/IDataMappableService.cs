namespace OfX.Abstractions;

/// <summary>
/// This is the abstraction. You can map anything within this function MapDataAsync!
/// </summary>
public interface IDataMappableService
{
    Task MapDataAsync(object value, IContext context = null);
}