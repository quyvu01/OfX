using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OfX.Extensions;
using OfX.Helpers;
using OfX.HotChocolate.Helpers;
using OfX.HotChocolate.Implementations;
using OfX.HotChocolate.Middlewares;
using OfX.HotChocolate.Resolvers;
using OfX.HotChocolate.Statics;

namespace OfX.HotChocolate.ApplicationModels;

public sealed class OfXHotChocolateRegister
{
    public void AddRequestExecutorBuilder(IRequestExecutorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddDataLoader<DataMappingLoader>();
        var schema = builder.BuildSchemaAsync().Result;
        var types = schema.Types;
        types.ForEach(a =>
        {
            var dataType = a.GetType();
            if (!dataType.IsGenericType) return;
            var genericType = dataType.GetGenericTypeDefinition();
            if (genericType != typeof(ObjectType<>)) return;
            var objectType = dataType.GetGenericArguments().FirstOrDefault();
            if (objectType is null) return;
            if (objectType.IsClass && !objectType.IsAbstract && !GeneralHelpers.IsPrimitiveType(objectType))
            {
                var dependencyGraphs = DependencyGraphBuilder
                    .BuildDependencyGraph(objectType);
                if (dependencyGraphs is { Count: > 0 })
                    OfXHotChocolateStatics.DependencyGraphs.Add(objectType, dependencyGraphs);
                builder
                    .AddType(typeof(OfXObjectType<>).MakeGenericType(objectType))
                    .AddResolver(typeof(DataResolvers<>).MakeGenericType(objectType));
            }
        });
        builder
            .UseRequest(next => async context =>
            {
                Console.WriteLine($"Before next: {context.Document}");
                await next.Invoke(context);
                Console.WriteLine($"After next: {context.Document}");
            });
        // builder.UseRequest<DependencyAwareRequestMiddleware>();
        // builder.UseField<DependencyAwareMiddleware>();
    }
}