using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Grpc.Abstractions;
using OfX.Queries.CrossCuttingQueries;

namespace OfX.Grpc.HandlersInstaller;

internal static class DefaultHandlersInstaller
{
    public static void InstallerServices(IServiceCollection services, Assembly handlerAssembly,
        params Assembly[] contractAssemblies)
    {
        var ignoreClasses = handlerAssembly.ExportedTypes
            .Where(x => typeof(IMappableRequestHandler).IsAssignableFrom(x) &&
                        x is { IsInterface: false, IsAbstract: false })
            .Distinct()
            .ToList();

        var ignoreRequestTypes = ignoreClasses.SelectMany(a => a.GetInterfaces())
            .Where(a =>
            {
                if (!a.IsGenericType) return false;
                var arguments = a.GetGenericArguments();
                return arguments.Length == 2;
            }).Select(a => a.GetGenericArguments()[0]);

        var requestTypes = contractAssemblies.SelectMany(x => x.ExportedTypes)
            .Where(x => typeof(GetDataMappableQuery).IsAssignableFrom(x) &&
                        x is { IsInterface: false, IsAbstract: false });

        var requestsCreatedImplicit = requestTypes.Except(ignoreRequestTypes).ToList();
        if (requestsCreatedImplicit is not { Count: > 0 }) return;
        var queryWithAttributes = requestsCreatedImplicit
            .Select(a =>
            {
                var basedType = a.BaseType;
                if (basedType is null || !basedType.IsGenericType) return (null, null);
                var arguments = basedType.GetGenericArguments();
                return (QueryType: a, AttributeType: arguments[0]);
            })
            .Where(a => a is { QueryType: not null, AttributeType: not null })
            .Select(a => (InterfaceType: typeof(IOfXGrpcRequestClient<,>).MakeGenericType(a.QueryType, a.AttributeType),
                ServiceType: typeof(IMappableRequestHandler<,>).MakeGenericType(a.QueryType, a.AttributeType)))
            .ToList();

        var assemblyName = new AssemblyName { Name = "DynamicInstanceAssemblyHandlers" };
        var newAssembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var newModule = newAssembly.DefineDynamicModule("DynamicInstanceModule");
        var typeBuilder = newModule.DefineType("DefaultOfXHandlers", TypeAttributes.Public, null,
            queryWithAttributes.Select(a => a.InterfaceType).ToArray());

        // Add the constructor
        var ctorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard,
            [typeof(IServiceProvider)]);

        var ctorGenerator = ctorBuilder.GetILGenerator();
        ctorGenerator.Emit(OpCodes.Ldarg_0); // Load "this" onto the stack
        ctorGenerator.Emit(OpCodes.Ldarg_1); // Load "IServiceProvider" onto the stack
        var repository = typeBuilder.DefineField("serviceProvider", typeof(IServiceProvider), FieldAttributes.Private);
        ctorGenerator.Emit(OpCodes.Stfld,
            repository); // Assign "serviceProvider" to the private field "IServiceProvider"
        ctorGenerator.Emit(OpCodes.Ret); // Return from the constructor

        // Add the IRequestClientRepository property
        var requestClientRepository = typeBuilder.DefineProperty("ServiceProvider", PropertyAttributes.None,
            typeof(IServiceProvider), null);
        var requestClientRepositoryGetMethod = typeBuilder.DefineMethod("get_ServiceProvider",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig |
            MethodAttributes.Virtual, typeof(IServiceProvider), Type.EmptyTypes);
        var methodGenerator = requestClientRepositoryGetMethod.GetILGenerator();
        methodGenerator.Emit(OpCodes.Ldarg_0); // Load "this" onto the stack
        methodGenerator.Emit(OpCodes.Ldfld,
            repository); // Load the value of the field "IRequestClientRepository" onto the stack`
        methodGenerator.Emit(OpCodes.Ret); // Return from the getter method
        requestClientRepository.SetGetMethod(requestClientRepositoryGetMethod);
        var handlersType = typeBuilder.CreateType();
        queryWithAttributes.ForEach(c => services.AddScoped(c.ServiceType, handlersType));
    }
}