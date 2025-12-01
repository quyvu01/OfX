using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OfX.Cached;
using OfX.Extensions;
using OfX.Helpers;
using OfX.HotChocolate.Extensions;
using OfX.HotChocolate.Implementations;
using OfX.HotChocolate.Resolvers;

namespace OfX.HotChocolate.ApplicationModels;

public sealed class OfXHotChocolateRegister
{
    public void AddRequestExecutorBuilder(IRequestExecutorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder
            .AddDataLoader<DataMappingLoader>()
            .UseInternalParametersMiddleware();
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
            var objectCache = OfXModelCache.GetModel(objectType);
            if (objectCache.DependencyGraphs is not { Count: > 0 }) return;
            builder
                .AddType(typeof(OfXObjectTypeExtension<>).MakeGenericType(objectType))
                .AddResolver(typeof(DataResolvers<>).MakeGenericType(objectType));
        });
    }
}