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

/// <summary>
/// Configuration class for registering HotChocolate GraphQL integration with the OfX framework.
/// </summary>
/// <remarks>
/// This registrar automatically discovers types with OfX attributes and creates
/// GraphQL resolvers and type extensions for them.
/// </remarks>
public sealed class OfXHotChocolateRegister
{
    /// <summary>
    /// Configures the HotChocolate request executor builder with OfX integration.
    /// </summary>
    /// <param name="builder">The HotChocolate request executor builder.</param>
    /// <remarks>
    /// This method:
    /// <list type="bullet">
    ///   <item><description>Registers the <see cref="DataMappingLoader"/> DataLoader for batched data fetching</description></item>
    ///   <item><description>Adds parameter middleware for expression parameter resolution</description></item>
    ///   <item><description>Automatically creates type extensions for types with OfX attributes</description></item>
    /// </list>
    /// </remarks>
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