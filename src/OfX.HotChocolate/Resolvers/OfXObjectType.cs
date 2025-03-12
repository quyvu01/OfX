using System.Reflection;
using OfX.Attributes;
using OfX.Extensions;
using OfX.HotChocolate.Abstractions;

namespace OfX.HotChocolate.Resolvers;

internal class OfXObjectType<TResponse> : ObjectType<TResponse>
{
    protected override void Configure(IObjectTypeDescriptor<TResponse> descriptor)
    {
        var props = typeof(TResponse)
            .GetProperties()
            .Where(p => p.GetCustomAttributes(true)
                .Any(a => typeof(OfXAttribute).IsAssignableFrom(a.GetType())))
            .Select(x =>
            {
                var attr = x.GetCustomAttribute<OfXAttribute>()!;
                return new
                {
                    TargetPropertyInfo = x, Attribute = attr,
                    RequiredPropertyInfo = typeof(TResponse).GetProperty(attr.PropertyName)
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
            .ResolveWith<ResponseResolvers<TResponse>>(x =>
                x.GetDataAsync(default!, null!, CancellationToken.None!)).Type(data.TargetPropertyInfo.PropertyType));
    }
}