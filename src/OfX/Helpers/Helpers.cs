namespace OfX.Helpers;

public static class Helpers
{
    public static string GetAssemblyName(this Type type) => $"{type.FullName},{type.Assembly.GetName().Name}";
}