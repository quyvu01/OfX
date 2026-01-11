namespace OfX.Helpers;

/// <summary>
/// Provides general-purpose helper methods used throughout the OfX framework.
/// </summary>
public static class GeneralHelpers
{
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