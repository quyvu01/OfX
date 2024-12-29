namespace OfX.EntityFrameworkCore.Exceptions;

public static class OfXEntityFrameworkException
{
    public class EntityFrameworkDbContextNotRegister(string message) : Exception(message);

    public class ThereAreNoDbContextHasModel(Type modelType)
        : Exception($"There are no any db context contains model: {modelType.Name}");
}