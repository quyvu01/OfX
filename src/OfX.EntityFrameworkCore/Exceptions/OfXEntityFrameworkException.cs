namespace OfX.EntityFrameworkCore.Exceptions;

public static class OfXEntityFrameworkException
{
    public class EntityFrameworkDbContextNotRegister() : Exception("DbContext must be registered first!");

    public class ThereAreNoDbContextHasModel(Type modelType)
        : Exception($"There are no any db context contains model: {modelType.Name}");

    public class DbContextsMustNotBeEmpty()
        : Exception("There are no any db contexts on AddOfXEFCore() method");

    public class DbContextTypeHasBeenRegisterBefore(Type dbContextType) : Exception(
        $"DbContext type {dbContextType.Name} already registered!");
}