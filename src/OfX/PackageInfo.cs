using System.Reflection;

namespace OfX;

internal static class PackageInfo
{
    private static readonly Lazy<string> PackageVersion = new(() => typeof(PackageInfo).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion ?? "1.0.0");

    internal static string Version => PackageVersion.Value;
}