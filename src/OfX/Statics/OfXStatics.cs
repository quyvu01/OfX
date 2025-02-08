using System.Reflection;
using OfX.Responses;

namespace OfX.Statics;

public static class OfXStatics
{
    internal static List<Assembly> AttributesRegister { get; set; } = [];
    internal static Assembly HandlersRegister { get; set; }
    internal static List<Type> StronglyTypeConfigurations { get; } = [];

    public static readonly Type OfXValueType = typeof(OfXValueResponse);

    public static readonly PropertyInfo ValueExpressionTypeProp =
        OfXValueType.GetProperty(nameof(OfXValueResponse.Expression))!;

    public static readonly PropertyInfo ValueValueTypeProp =
        OfXValueType.GetProperty(nameof(OfXValueResponse.Value))!;
    
    public static readonly PropertyInfo OfXIdProp =
        typeof(OfXDataResponse).GetProperty(nameof(OfXDataResponse.Id))!;

    public static readonly PropertyInfo OfXValuesProp =
        typeof(OfXDataResponse).GetProperty(nameof(OfXDataResponse.OfXValues))!;
}