using System.Reflection;
using HotChocolate.Resolvers;
using OfX.Attributes;
using OfX.Extensions;
using OfX.HotChocolate.Abstractions;
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
                if (!OfXHotChocolateStatics.DependencyGraphs.TryGetValue(typeof(T), out var dependencyGraphs))
                {
                    await next(context);
                    return;
                }

                var args = context.Selection
                    .DeclaringOperation
                    .RootSelectionSet
                    .Selections
                    .Select(a => a.Arguments)
                    .OfType<IReadOnlyCollection<KeyValuePair<string, ArgumentValue>>>()
                    .SelectMany(a => a);

                var parameters = args
                    .Select(a => a.Value)
                    .Select(a => (a.RuntimeType, a.ValueLiteral))
                    .ToList();
                
                // Yup, I need to get the MethodInfo to get the parameters are marked by [ParametersAttribute]...

                var currentContext = context.Service<ICurrentContextProvider>();
                var ctx = currentContext.CreateContext();
                var attribute = data.Attribute;
                ctx.TargetPropertyInfo = data.TargetPropertyInfo;
                ctx.Expression = attribute.Expression;
                ctx.RuntimeAttributeType = attribute.GetType();
                ctx.SelectorPropertyName = attribute.PropertyName;
                ctx.RequiredPropertyInfo = data.RequiredPropertyInfo;
                ctx.Order = dependencyGraphs.GetPropertyOrder(data.TargetPropertyInfo);
                await next(context);
            })
            .ResolveWith<DataResolvers<T>>(x => x.GetDataAsync(null!, null!))
            .Type(data.TargetPropertyInfo.PropertyType));
}