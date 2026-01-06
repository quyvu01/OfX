namespace OfX.Helpers;

/// <summary>
/// Provides general-purpose helper methods used throughout the OfX framework.
/// </summary>
public static class GeneralHelpers
{
    /// <summary>
    /// Gets the assembly-qualified name of a type in the format "FullName,AssemblyName".
    /// </summary>
    /// <param name="type">The type to get the assembly name for.</param>
    /// <returns>A string containing the type's full name and assembly name.</returns>
    public static string GetAssemblyName(this Type type) => $"{type.FullName},{type.Assembly.GetName().Name}";

    /// <summary>
    /// Determines if an object is of a primitive or value type.
    /// </summary>
    /// <param name="obj">The object to check.</param>
    /// <returns>True if the object is a primitive or value type; otherwise false.</returns>
    public static bool IsPrimitiveType(object obj)
    {
        if (obj == null) return false;
        var type = obj.GetType();
        return IsPrimitiveType(type);
    }

    /// <summary>
    /// Determines if a type is a primitive or value type.
    /// </summary>
    /// <param name="objectType">The type to check.</param>
    /// <returns>True if the type is a primitive, string, DateTime, enum, decimal, or any value type.</returns>
    /// <remarks>
    /// This method is used to determine whether a type should be recursively processed
    /// for OfX attribute mapping or treated as a leaf value.
    /// </remarks>
    public static bool IsPrimitiveType(Type objectType) =>
        objectType.IsPrimitive ||
        objectType == typeof(string) ||
        objectType == typeof(DateTime) ||
        objectType.IsEnum ||
        objectType == typeof(decimal) ||
        objectType.IsValueType; // Covers all value types (structs, etc.)
}