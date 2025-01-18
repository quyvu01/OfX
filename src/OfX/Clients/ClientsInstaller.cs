using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OfX.Abstractions;

namespace OfX.Clients;

public static class ClientsInstaller
{
    /// <summary>
    /// ServiceSubstituteType must be implemented from IMappableRequestHandler and have only IServiceProvider!
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="serviceSubstituteType"></param>
    /// <param name="attributeTypes"></param>
    public static void InstallMappableRequestHandlers(IServiceCollection serviceCollection, Type serviceSubstituteType,
        params Type[] attributeTypes)
    {
        var attributesBuilding = attributeTypes
            .Select(a => (AttributeType: a, InterfaceType: serviceSubstituteType.MakeGenericType(a),
                ServiceType: typeof(IMappableRequestHandler<>).MakeGenericType(a)))
            .ToList();

        var assemblyName = new AssemblyName { Name = "DynamicInstanceAssemblyHandlers" };
        var newAssembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var newModule = newAssembly.DefineDynamicModule("DynamicInstanceModule");
        var typeBuilder = newModule.DefineType("DynamicOfXClientHandlers", TypeAttributes.Public, null,
            attributesBuilding.Select(a => a.InterfaceType).ToArray());

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
        attributesBuilding.ForEach(c =>
        {
            var existedService = serviceCollection.FirstOrDefault(a => a.ServiceType == c.ServiceType);
            if (existedService is not null)
            {
                if (existedService.ImplementationType !=
                    typeof(DefaultMappableRequestHandler<>).MakeGenericType(c.AttributeType)) return;
                serviceCollection.Replace(new ServiceDescriptor(c.ServiceType, handlersType, ServiceLifetime.Scoped));
                return;
            }

            serviceCollection.AddScoped(c.ServiceType, handlersType);
        });
    }
}