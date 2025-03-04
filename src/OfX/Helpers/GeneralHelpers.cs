namespace OfX.Helpers;

public static class GeneralHelpers
{
    public static string GetAssemblyName(this Type type) => $"{type.FullName},{type.Assembly.GetName().Name}";

    public static bool IsPrimitiveType(object obj)
    {
        if (obj == null) return false;
        var type = obj.GetType();
        return IsPrimitiveType(type);
    }

    public static bool IsPrimitiveType(Type objectType) =>
        objectType.IsPrimitive ||
        objectType == typeof(string) ||
        objectType == typeof(DateTime) ||
        objectType.IsEnum ||
        objectType == typeof(decimal) ||
        objectType.IsValueType; // Covers all value types (structs, etc.)
}