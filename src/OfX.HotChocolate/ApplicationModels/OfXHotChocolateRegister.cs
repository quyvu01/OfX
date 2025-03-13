using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OfX.Extensions;
using OfX.Helpers;
using OfX.HotChocolate.Implementations;
using OfX.HotChocolate.Middlewares;
using OfX.HotChocolate.Resolvers;

namespace OfX.HotChocolate.ApplicationModels;

public sealed class OfXHotChocolateRegister
{
    public void AddRequestExecutorBuilder(IRequestExecutorBuilder builder)
    {
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
            if (objectType is not null && objectType.IsClass && !objectType.IsAbstract &&
                !GeneralHelpers.IsPrimitiveType(objectType))
            {
                builder
                    .AddType(typeof(OfXObjectType<>).MakeGenericType(objectType))
                    .AddResolver(typeof(ResponseResolvers<>).MakeGenericType(objectType));
            }
        });
        builder.UseField<DependencyAwareMiddleware>();
    }
}