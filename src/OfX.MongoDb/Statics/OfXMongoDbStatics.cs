using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace OfX.MongoDb.Statics;

public static class OfXMongoDbStatics
{
    internal static readonly List<Type> ModelTypes = [];

    internal static readonly Lazy<ConcurrentDictionary<Type, MethodCallExpression>> IdMethodCallExpressions =
        new(() => []);
}