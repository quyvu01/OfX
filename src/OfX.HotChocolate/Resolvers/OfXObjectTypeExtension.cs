using System.Reflection;
using OfX.Attributes;
using OfX.Extensions;
using OfX.HotChocolate.Abstractions;

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
                var currentContext = context.Service<ICurrentContextProvider>();
                var ctx = currentContext.CreateContext();
                ctx.TargetPropertyInfo = data.TargetPropertyInfo;
                var attribute = data.Attribute;
                ctx.Expression = attribute.Expression;
                ctx.RuntimeAttributeType = attribute.GetType();
                ctx.SelectorPropertyName = attribute.PropertyName;
                ctx.RequiredPropertyInfo = data.RequiredPropertyInfo;
                ctx.Order = attribute.Order;
                await next(context);
            })
            .ResolveWith<DataResolvers<T>>(x =>
                x.GetDataAsync(null!, null!))
            .Type(data.TargetPropertyInfo.PropertyType)
        );
}