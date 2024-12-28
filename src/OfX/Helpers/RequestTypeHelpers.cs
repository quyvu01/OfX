using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using OfX.Abstractions;

namespace OfX.Helpers;

public static class RequestTypeHelpers
{
    private static readonly List<Type> RequestArgumentsType = [typeof(List<string>), typeof(string)];
    private static readonly Lazy<ConcurrentDictionary<Type, Type>> AttributeTypeLookup = new(() => []);

    public static Type GetRequestTypeByAttribute(Type attributeType) =>
        AttributeTypeLookup.Value.GetOrAdd(attributeType, CreateRequestTypeByAttribute);

    private static Type CreateRequestTypeByAttribute(Type attributeType)
    {
        //Todo: I have to validate the attribute type is OfXAttribute or not!
        var attributeAssemblyName = attributeType.Namespace;
        var assemblyName = new AssemblyName { Name = attributeAssemblyName };
        var newAssembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var newModule = newAssembly.DefineDynamicModule("DynamicRequestModule");
        var requestType = typeof(DataMappableOf<>).MakeGenericType(attributeType);
        var typeBuilder = newModule.DefineType($"DynamicRequestType{attributeType.Name}", TypeAttributes.Public,
            typeof(DataMappableOf<>).MakeGenericType(attributeType));
        var ctorTypes = requestType.GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance, [..RequestArgumentsType])!;
        // Define the constructor for the dynamic class
        var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public,
            CallingConventions.Standard, [..RequestArgumentsType]);
        // Generate the constructor IL code
        var ilGenerator = constructorBuilder.GetILGenerator();
        ilGenerator.Emit(OpCodes.Ldarg_0); // Load "this" onto the stack
        ilGenerator.Emit(OpCodes.Ldarg_1); // Load the List<string> argument onto the stack
        ilGenerator.Emit(OpCodes.Ldarg_2); // Load the string argument onto the stack
        // Call the base constructor with the List<string> and string arguments
        ilGenerator.Emit(OpCodes.Call, ctorTypes);
        ilGenerator.Emit(OpCodes.Ret); // Return from the constructor
        return typeBuilder.CreateType();
    }
}