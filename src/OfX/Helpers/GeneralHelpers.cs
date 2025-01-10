namespace OfX.Helpers;

public static class GeneralHelpers
{
    public static string GetAssemblyName(this Type type) => $"{type.FullName},{type.Assembly.GetName().Name}";
}