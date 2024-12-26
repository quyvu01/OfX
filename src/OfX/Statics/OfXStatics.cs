namespace OfX.Statics;

public static class OfXStatics
{
    public static IReadOnlyDictionary<Type, Type> QueryMapHandler => InternalQueryMapHandler;

    public static Dictionary<Type, Type> InternalQueryMapHandler { get; set; } = [];
}