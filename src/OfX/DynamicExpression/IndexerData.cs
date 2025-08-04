using System.Reflection;

namespace OfX.DynamicExpression;

internal class IndexerData : MethodData
{
    public readonly PropertyInfo Indexer;

    public IndexerData(PropertyInfo indexer)
    {
        Indexer = indexer;

        var method = indexer.GetGetMethod();
        if (method != null)
        {
            Parameters = method.GetParameters();
            return;
        }

        method = indexer.GetSetMethod()!;
        Parameters = RemoveLast(method.GetParameters());
    }

    private static T[] RemoveLast<T>(T[] array)
    {
        var result = new T[array.Length - 1];
        Array.Copy(array, 0, result, 0, result.Length);
        return result;
    }
}