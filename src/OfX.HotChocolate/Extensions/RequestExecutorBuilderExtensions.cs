using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using OfX.HotChocolate.Attributes;
using OfX.HotChocolate.Constants;

namespace OfX.HotChocolate.Extensions;

internal static class RequestExecutorBuilderExtensions
{
    internal static IRequestExecutorBuilder UseInternalParametersMiddleware(this IRequestExecutorBuilder builder)
    {
        ConcurrentDictionary<MethodInfo, ParameterInfo> methodInfoLookup = [];
        ConcurrentDictionary<ParameterInfo, Func<IResolverContext, object>> parameterInfoLookup = [];
        builder.UseField(next => async context =>
        {
            if (context.Selection.Field.ResolverMember is not MethodInfo method)
            {
                await next.Invoke(context);
                return;
            }

            var paramWithAttr = methodInfoLookup.GetOrAdd(method, mt => mt.GetParameters()
                .SingleOrDefault(p => p.GetCustomAttribute<ParametersAttribute>() != null));


            if (paramWithAttr == null)
            {
                await next.Invoke(context);
                return;
            }

            var argumentFunc = parameterInfoLookup.GetOrAdd(paramWithAttr, BuildArgumentGetter);

            var paramValue = argumentFunc?.Invoke(context);
            if (paramValue is null)
            {
                await next(context);
                return;
            }

            context.ContextData[OfXHotChocolateConstants.ContextDataParametersHeader] = ObjectToDictionary();

            await next(context);
            return;

            Dictionary<string, string> ObjectToDictionary() => paramValue switch
            {
                Dictionary<string, string> val => val,
                _ => paramValue
                    .GetType()
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .ToDictionary(p => p.Name, p => p.GetValue(paramValue)?.ToString())
            };
        });
        return builder;
    }

    private static Func<IResolverContext, object> BuildArgumentGetter(ParameterInfo paramInfo)
    {
        // Parameter: IResolverContext context
        var contextParam = Expression.Parameter(typeof(IResolverContext), "context");

        var paramName = paramInfo.Name;
        var paramType = paramInfo.ParameterType;

        // Get the ArgumentValue<T> method
        var argumentValueMethod = typeof(IResolverContext)
            .GetMethod(nameof(IResolverContext.ArgumentValue))
            !.MakeGenericMethod(paramType);

        // Call: context.ArgumentValue<paramType>(paramName)
        var methodCall = Expression.Call(contextParam, argumentValueMethod, Expression.Constant(paramName));

        // Convert to object for the return type
        var convertToObject = Expression.Convert(methodCall, typeof(object));

        // Compile: context => (object)context.ArgumentValue<paramType>(paramName)
        var lambda = Expression.Lambda<Func<IResolverContext, object>>(convertToObject, contextParam);

        return lambda.Compile();
    }
}