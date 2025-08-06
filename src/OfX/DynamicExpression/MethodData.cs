using System.Linq.Expressions;
using System.Reflection;

namespace OfX.DynamicExpression;

internal class MethodData
{
    public MethodBase MethodBase;
    public IList<ParameterInfo> Parameters;
    public IList<Expression> PromotedParameters;
    public bool HasParamsArray;

    public static MethodData Gen(MethodBase method) => new()
    {
        MethodBase = method,
        Parameters = method.GetParameters()
    };

    public override string ToString() => MethodBase.ToString();
}