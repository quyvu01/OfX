using System.Reflection;
using OfX.Attributes;
using OfX.Cached;
using OfX.Extensions;
using OfX.HotChocolate.Constants;
using OfX.HotChocolate.GraphQlContext;

namespace OfX.HotChocolate.Resolvers;

/// <summary>
/// HotChocolate type extension that configures OfX-decorated fields for automatic resolution.
/// </summary>
/// <typeparam name="T">The GraphQL object type being extended.</typeparam>
/// <remarks>
/// This extension automatically:
/// <list type="bullet">
///   <item><description>Discovers properties with OfX attributes</description></item>
///   <item><description>Adds middleware to extract expression parameters</description></item>
///   <item><description>Configures resolvers to use the <see cref="DataResolvers{TResponse}"/></description></item>
/// </list>
/// </remarks>
internal class OfXObjectTypeExtension<T> : ObjectTypeExtension<T> where T : class
{
    protected override void Configure(IObjectTypeDescriptor<T> descriptor) => typeof(T)
        .GetProperties()
        .Where(p => p.GetCustomAttributes(true)
            .Any(a => typeof(OfXAttribute).IsAssignableFrom(a.GetType())))
        .Select(x =>
        {
            var attr = x.GetCustomAttribute<OfXAttribute>();
            if (attr is null) return null;
            var requiredProperty = typeof(T).GetProperty(attr.PropertyName);
            if (requiredProperty is null)
                throw new InvalidOperationException(
                    $"Property '{attr.PropertyName}' not found on type '{typeof(T).FullName}'. " +
                    $"Required by OfX attribute on property '{x.Name}'.");
            return new
            {
                TargetPropertyInfo = x, Attribute = attr,
                RequiredPropertyInfo = requiredProperty
            };
        })
        .Where(data => data is not null)
        .ForEach(data => descriptor.Field(data!.TargetPropertyInfo)
            .Use(next => async context =>
            {
                var methodPath = context.Path.ToList().FirstOrDefault()?.ToString();
                var modelCached = OfXModelCache
                    .ContainsModel(typeof(T));
                if (!modelCached)
                {
                    await next(context);
                    return;
                }

                var expressionParameters = context.ContextData
                        .TryGetValue(GraphQlConstants.GetContextDataParametersHeader(methodPath),
                            out var value) switch
                    {
                        true => value switch
                        {
                            Dictionary<string, string> parameters => parameters,
                            _ => null
                        },
                        _ => null
                    };

                var groupId = context.ContextData
                        .TryGetValue(GraphQlConstants.GetContextDataGroupIdHeader(methodPath),
                            out var groupIdFromHeader) switch
                    {
                        true => groupIdFromHeader?.ToString(),
                        _ => null
                    };

                var attribute = data.Attribute;
                
                var dependencyGraphs = OfXModelCache
                    .GetModelAccessor(typeof(T))
                    .DependencyGraphs;

                var ctx = new FieldContext
                {
                    TargetPropertyInfo = data.TargetPropertyInfo,
                    Expression = attribute.Expression,
                    RuntimeAttributeType = attribute.GetType(),
                    SelectorPropertyName = attribute.PropertyName,
                    RequiredPropertyInfo = data.RequiredPropertyInfo,
                    Order = dependencyGraphs.GetPropertyOrder(data.TargetPropertyInfo),
                    ExpressionParameters = expressionParameters,
                    GroupId = groupId
                };

                context.ContextData[GraphQlConstants.GetContextFieldContextHeader(methodPath)] = ctx;

                await next(context);
            })
            .ResolveWith<DataResolvers<T>>(x => x.GetDataAsync(null!, null!))
            .Type(data.TargetPropertyInfo.PropertyType));
}