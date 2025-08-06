using System.Linq.Expressions;
using System.Reflection;

namespace OfX.DynamicExpression;

public class Identifier
{
    public Expression Expression { get; }
    public string Name { get; }

    public Identifier(string name, Expression expression)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name));

        Expression = expression ?? throw new ArgumentNullException(nameof(expression));
        Name = name;
    }
}

internal class FunctionIdentifier : Identifier
{
    internal FunctionIdentifier(string name, Delegate value) : base(name, new MethodGroupExpression(value))
    {
    }

    internal void AddOverload(Delegate overload) => ((MethodGroupExpression)Expression).AddOverload(overload);
}

/// <summary>
/// Custom expression that simulates a method group (i.e., a group of methods with the same name).
/// It's used when custom functions are added to the interpreter via <see cref="Interpreter.SetFunction(string, Delegate)"/>.
/// </summary>
internal class MethodGroupExpression : Expression
{
    public class Overload(Delegate @delegate)
    {
        public Delegate Delegate { get; } = @delegate;
        private MethodBase _method;
        private MethodBase _invokeMethod;

        public MethodBase Method => _method ??= Delegate.Method;

        // we'll most likely never need this: it was necessary before https://github.com/dotnet/roslyn/pull/53402
        public MethodBase InvokeMethod => _invokeMethod ??= MemberFinder.FindInvokeMethod(Delegate.GetType());
    }

    private readonly List<Overload> _overloads = [];

    internal IReadOnlyCollection<Overload> Overloads => _overloads.AsReadOnly();

    internal MethodGroupExpression(Delegate overload) => AddOverload(overload);

    internal void AddOverload(Delegate overload)
    {
        // remove any existing delegate with the exact same signature
        RemoveDelegateSignature(overload);
        _overloads.Add(new Overload(overload));
    }

    private void RemoveDelegateSignature(Delegate overload) =>
        _overloads.RemoveAll(del => HasSameSignature(overload.Method, del.Delegate.Method));

    private static bool HasSameSignature(MethodInfo method, MethodInfo other)
    {
        if (method.ReturnType != other.ReturnType) return false;

        var param = method.GetParameters();
        var oParam = other.GetParameters();
        if (param.Length != oParam.Length) return false;

        for (var i = 0; i < param.Length; i++)
        {
            var p = param[i];
            var q = oParam[i];
            if (p.ParameterType != q.ParameterType || p.HasDefaultValue != q.HasDefaultValue) return false;
        }

        return true;
    }

    /// <summary>
    /// The resolution process will find the best overload for the given arguments,
    /// which we then need to match to the correct delegate.
    /// </summary>
    internal Delegate FindUsedOverload(bool usedInvokeMethod, MethodData methodData)
    {
        foreach (var overload in _overloads)
        {
            if (usedInvokeMethod)
            {
                if (methodData.MethodBase == overload.InvokeMethod) return overload.Delegate;
            }
            else
            {
                if (methodData.MethodBase == overload.Method) return overload.Delegate;
            }
        }

        // this should never happen
        throw new InvalidOperationException("No overload matches the method");
    }
}