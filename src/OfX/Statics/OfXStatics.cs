using System.Reflection;

namespace OfX.Statics;

internal static class OfXStatics
{
    internal static List<Assembly> AttributesRegister { get; set; } = [];
    internal static Assembly HandlersRegister { get; set; }
}