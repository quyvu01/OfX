using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace OfX.EntityFrameworkCore.Statics;

internal static class EntityFrameworkCoreStatics
{
    internal static List<Type> DbContextTypes { get; set; } = [];

    internal static readonly Lazy<ConcurrentDictionary<Type, MethodCallExpression>> IdMethodCallExpressions =
        new(() => []);
}