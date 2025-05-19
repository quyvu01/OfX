using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OfX.Extensions;
using OfX.Helpers;
using OfX.HotChocolate.Implementations;
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
            if (!objectType.IsClass || objectType.IsAbstract || GeneralHelpers.IsPrimitiveType(objectType)) return;
            var dependencyGraphs = DependencyGraphBuilder.BuildDependencyGraph(objectType);
            if (dependencyGraphs is { Count: > 0 })
                OfXHotChocolateStatics.DependencyGraphs.TryAdd(objectType, dependencyGraphs);
            builder
                .AddType(typeof(OfXObjectTypeExtension<>).MakeGenericType(objectType))
                .AddResolver(typeof(DataResolvers<>).MakeGenericType(objectType));
        });
    }
}