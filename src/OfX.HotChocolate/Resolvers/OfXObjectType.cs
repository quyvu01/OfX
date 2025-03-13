using System.Reflection;
using OfX.Attributes;
using OfX.Extensions;
using OfX.HotChocolate.Abstractions;
using OfX.HotChocolate.Helpers;
using OfX.HotChocolate.Statics;

namespace OfX.HotChocolate.Resolvers;

internal class OfXObjectType<T> : ObjectType<T>
{
    protected override void Configure(IObjectTypeDescriptor<T> descriptor)
    {
        var dependencyGraphs = DependencyGraphBuilder.BuildDependencyGraph(typeof(T));
        if (dependencyGraphs is { Count: > 0 })
            OfXHotChocolateStatics.DependencyGraphs.Add(typeof(T), dependencyGraphs);
        var props = typeof(T)
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
            });
        props.ForEach(data => descriptor.Field(data.TargetPropertyInfo)
            .Use(next => async context =>
            {
                var currentContext = context.Service<ICurrentContextProvider>();
                var ctx = currentContext.CreateContext();
                // Temp for test
                ctx.TargetPropertyInfo = data.TargetPropertyInfo;
                var attribute = data.Attribute;
                ctx.Expression = attribute.Expression;
                ctx.RuntimeAttributeType = attribute.GetType();
                ctx.SelectorPropertyName = attribute.PropertyName;
                ctx.RequiredPropertyInfo = data.RequiredPropertyInfo;
                ctx.Order = attribute.Order;
                await next(context);
            })
            .ResolveWith<ResponseResolvers<T>>(x =>
                x.GetDataAsync(default!, null!, CancellationToken.None!)).Type(data.TargetPropertyInfo.PropertyType));
    }
}