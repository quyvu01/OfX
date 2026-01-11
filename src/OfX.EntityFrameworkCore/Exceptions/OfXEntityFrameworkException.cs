namespace OfX.EntityFrameworkCore.Exceptions;

/// <summary>
/// Contains exception types specific to the OfX Entity Framework Core integration.
/// </summary>
public static class OfXEntityFrameworkException
{
    /// <summary>
    /// Thrown when attempting to resolve a DbContext that has not been registered with the DI container.
    /// </summary>
    public class EntityFrameworkDbContextNotRegister() : Exception("DbContext must be registered first!");

    /// <summary>
    /// Thrown when no registered DbContext contains the requested entity model type.
    /// </summary>
    /// <param name="modelType">The model type that was not found in any DbContext.</param>
    public class ThereAreNoDbContextHasModel(Type modelType)
        : Exception($"There are no any db context contains model: {modelType.Name}");

    /// <summary>
    /// Thrown when AddOfXEFCore is called without providing any DbContext types.
    /// </summary>
    public class DbContextsMustNotBeEmpty()
        : Exception("There are no any db contexts on AddOfXEFCore() method");

    /// <summary>
    /// Thrown when the same DbContext type is registered more than once.
    /// </summary>
    /// <param name="dbContextType">The DbContext type that was already registered.</param>
    public class DbContextTypeHasBeenRegisterBefore(Type dbContextType) : Exception(
        $"DbContext type {dbContextType.Name} already registered!");
}