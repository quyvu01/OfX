using System.Reflection;
using HotChocolate.Resolvers;
using OfX.HotChocolate.Statics;

namespace OfX.HotChocolate.Middlewares;

internal class DependencyAwareMiddleware(FieldDelegate next)
{
    public async Task InvokeAsync(IMiddlewareContext context)
    {
        var parent = context.Parent<object>();
        if (parent == null)
        {
            await next(context);
            return;
        }

        var parentType = parent.GetType();
        if (!OfXHotChocolateStatics.DependencyGraphs.TryGetValue(parentType, out var dependencyGraph))
        {
            await next(context);
            return;
        }

        // Get the field being resolved
        var fieldName = context.Selection.SyntaxNode.Name.Value;
        var fieldProperty = parentType.GetProperty(fieldName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

        if (fieldProperty != null && dependencyGraph.TryGetValue(fieldProperty, out var dependencies))
        {
            // Get the dependencies for the field

            // Ensure all dependencies are resolved
            foreach (var dependency in dependencies)
            {
                var dependencyFieldName = dependency.Name;
                var dependencyField = context.ObjectType.Fields
                    .FirstOrDefault(f => f.Name.Equals(dependencyFieldName, StringComparison.OrdinalIgnoreCase));

                if (dependencyField != null)
                {
                    // Resolve the dependency field
                }
            }
        }

        // Continue to the next middleware or resolver
        await next(context);
    }
}