using System.Reflection;

namespace OfX.DynamicExpression;

internal static class ReflectionExtensions
{
    public static readonly MethodInfo StringConcatMethod = GetStringConcatMethod();
    public static readonly MethodInfo ObjectToStringMethod = GetObjectToStringMethod();

    public static DelegateInfo GetDelegateInfo(Type delegateType, params string[] parametersNames)
    {
        var method = delegateType.GetMethod("Invoke");
        if (method == null) throw new ArgumentException("The specified type is not a delegate");

        var delegateParameters = method.GetParameters();
        var parameters = new Parameter[delegateParameters.Length];

        var useCustomNames = parametersNames is { Length: > 0 };

        if (useCustomNames && parametersNames.Length != parameters.Length)
            throw new ArgumentException(
                $"Provided parameters names doesn't match delegate parameters, {parameters.Length} parameters expected.");

        for (var i = 0; i < parameters.Length; i++)
        {
            var paramName = useCustomNames ? parametersNames[i] : delegateParameters[i].Name;
            var paramType = delegateParameters[i].ParameterType;

            parameters[i] = new Parameter(paramName, paramType);
        }

        return new DelegateInfo(method.ReturnType, parameters);
    }

    public static IEnumerable<MethodInfo> GetExtensionMethods(Type type)
    {
        if (!type.IsSealed || !type.IsAbstract || type.IsGenericType || type.IsNested) return [];
        var query = from method in type.GetMethods(BindingFlags.Static | BindingFlags.Public |
                                                   BindingFlags.NonPublic)
            where method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
            select method;
        return query;

    }

    public class DelegateInfo(Type returnType, Parameter[] parameters)
    {
        public Type ReturnType { get; private set; } = returnType;
        public Parameter[] Parameters { get; private set; } = parameters;
    }

    private static MethodInfo GetStringConcatMethod()
    {
        var methodInfo = typeof(string).GetMethod("Concat", [typeof(string), typeof(string)]);
        if (methodInfo == null) throw new Exception("String concat method not found");
        return methodInfo;
    }

    private static MethodInfo GetObjectToStringMethod()
    {
        var toStringMethod = typeof(object).GetMethod("ToString", Type.EmptyTypes);
        if (toStringMethod == null) throw new Exception("ToString method not found");
        return toStringMethod;
    }

    // +1 for the return type
    public static Type GetFuncType(int parameterCount)
        => typeof(Func<>).Assembly.GetType($"System.Func`{parameterCount + 1}");

    public static Type GetActionType(int parameterCount)
        => typeof(Action<>).Assembly.GetType($"System.Action`{parameterCount}");

    public static bool HasParamsArrayType(ParameterInfo parameterInfo)
        => parameterInfo.IsDefined(typeof(ParamArrayAttribute), false);

    public static Type GetParameterType(ParameterInfo parameterInfo)
    {
        var isParamsArray = HasParamsArrayType(parameterInfo);
        var type = isParamsArray
            ? parameterInfo.ParameterType.GetElementType()
            : parameterInfo.ParameterType;
        return type;
    }
}