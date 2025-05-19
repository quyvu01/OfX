using System.Collections.Concurrent;
using System.Reflection;
using OfX.Attributes;
using OfX.Extensions;
using OfX.Helpers;
using OfX.HotChocolate.Abstractions;
using OfX.ObjectContexts;

namespace OfX.HotChocolate.Resolvers;

internal class OfXObjectTypeExtension<T> : ObjectTypeExtension<T> where T : class
{
    internal static readonly ConcurrentDictionary<Type, Dictionary<PropertyInfo, PropertyContext[]>>
        Graphs = []; // Todo: Remove on next version!

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
                var graph = Graphs.GetOrAdd(typeof(T), _ => DependencyGraphBuilder.BuildDependencyGraph(typeof(T)));
                var order = graph.TryGetValue(data.TargetPropertyInfo, out var dependencies) switch
                {
                    true => dependencies.Length - 1,
                    _ => 0
                };
                var currentContext = context.Service<ICurrentContextProvider>();
                var ctx = currentContext.CreateContext();
                var attribute = data.Attribute;
                ctx.TargetPropertyInfo = data.TargetPropertyInfo;
                ctx.Expression = attribute.Expression;
                ctx.RuntimeAttributeType = attribute.GetType();
                ctx.SelectorPropertyName = attribute.PropertyName;
                ctx.RequiredPropertyInfo = data.RequiredPropertyInfo;
                ctx.Order = order;
                await next(context);
            })
            .ResolveWith<DataResolvers<T>>(x =>
                x.GetDataAsync(null!, null!))
            .Type(data.TargetPropertyInfo.PropertyType)
        );
}