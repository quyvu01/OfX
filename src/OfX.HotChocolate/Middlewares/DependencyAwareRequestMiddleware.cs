using System.Reflection;
using HotChocolate.Execution;
using HotChocolate.Language;
using OfX.HotChocolate.Statics;

namespace OfX.HotChocolate.Middlewares;

public class DependencyAwareRequestMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(IRequestContext context)
    {
        Console.WriteLine("[IRequestContext]");
        await next(context);
        return;
        // Get the query document
        var queryDocument = context.Document;
        if (queryDocument == null)
        {
            await next(context);
            return;
        }

        // Get the operation definition
        var operationDefinition = queryDocument.Definitions.OfType<OperationDefinitionNode>().FirstOrDefault();
        if (operationDefinition == null)
        {
            await next(context);
            return;
        }

        // Get the selection set
        var selectionSet = operationDefinition.SelectionSet;
        if (selectionSet == null)
        {
            await next(context);
            return;
        }

        // Modify the selection set to include required fields
        var modifiedSelectionSet = ModifySelectionSet(selectionSet, context.Schema);

        // Update the query document with the modified selection set
        var modifiedQueryDocument = queryDocument.WithDefinitions([
            operationDefinition.WithSelectionSet(modifiedSelectionSet)
        ]);

        // Update the request context with the modified query document
        context.Document = modifiedQueryDocument;

        // Continue to the next middleware
        await next(context);
    }

    private SelectionSetNode ModifySelectionSet(SelectionSetNode selectionSet, ISchema schema)
    {
        var selections = selectionSet.Selections.ToList();

        foreach (var selection in selections)
        {
            if (selection is FieldNode fieldNode)
            {
                // Get the field name and type
                var fieldName = fieldNode.Name.Value;
                var fieldType = schema.Types.OfType<ObjectType>().FirstOrDefault(t =>
                    t.Fields.Any(f => f.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase)));

                if (fieldType != null)
                {
                    // Get the dependency graph for the field's type
                    if (OfXHotChocolateStatics.DependencyGraphs.TryGetValue(fieldType.RuntimeType, out var dependencyGraph))
                    {
                        // Get the field's property
                        var fieldProperty = fieldType.RuntimeType.GetProperty(fieldName,
                            BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                        if (fieldProperty != null && dependencyGraph.ContainsKey(fieldProperty))
                        {
                            // Get the dependencies for the field
                            var dependencies = dependencyGraph[fieldProperty];

                            // Add the dependencies to the selection set
                            foreach (var dependency in dependencies)
                            {
                                var dependencyFieldName = dependency.Name;
                                if (!selections.Any(s =>
                                        s is FieldNode f && f.Name.Value.Equals(dependencyFieldName,
                                            StringComparison.OrdinalIgnoreCase)))
                                {
                                    selections.Add(new FieldNode(null, new NameNode(dependencyFieldName), null,
                                        Array.Empty<DirectiveNode>(), Array.Empty<ArgumentNode>(), null));
                                }
                            }
                        }
                    }
                }
            }
        }

        // Return the modified selection set
        return new SelectionSetNode(null, selections);
    }
}