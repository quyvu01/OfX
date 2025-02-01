using System.Reflection;

namespace OfX.EntityFrameworkCore.Statics;

internal static class EntityFrameworkCoreStatics
{
    internal static List<Type> DbContextTypes { get; set; } = [];
    internal static Assembly ModelConfigurationAssembly { get; set; }
}