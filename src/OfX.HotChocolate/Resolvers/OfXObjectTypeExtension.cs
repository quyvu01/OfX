using System.Reflection;
using OfX.Attributes;
using OfX.Extensions;
using OfX.HotChocolate.Constants;
using OfX.HotChocolate.GraphQlContext;
using OfX.HotChocolate.Statics;

namespace OfX.HotChocolate.Resolvers;

internal class OfXObjectTypeExtension<T> : ObjectTypeExtension<T> where T : class
{
    protected override void Configure(IObjectTypeDescriptor<T> descriptor) => typeof(T)
        .GetProperties()
        .Where(p => p.GetCustomAttributes(true)
            .Any(a => typeof(OfXAttribute).IsAssignableFrom(a.GetType())))
        .Select(x =>
        {
            var attr = x.GetCustomAttribute<OfXAttribute>()!;
            return new
            {
                TargetPropertyInfo = x, Attribute = attr,
                RequiredPropertyInfo = typeof(T).GetProperty(attr.PropertyName)
            };
        })
        .ForEach(data => descriptor.Field(data.TargetPropertyInfo)
            .Use(next => async context =>
            {
                var methodPath = context.Path.ToList().FirstOrDefault()?.ToString();
                if (!OfXHotChocolateStatics.DependencyGraphs.TryGetValue(typeof(T), out var dependencyGraphs))
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