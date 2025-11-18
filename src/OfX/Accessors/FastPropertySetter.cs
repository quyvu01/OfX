using System.Reflection;
using System.Reflection.Emit;

namespace OfX.Accessors;

public static class FastPropertySetter
{
    public static Action<object, object> CreateSetter(PropertyInfo property)
    {
        var method = new DynamicMethod(
            name: $"_set_{property.Name}",
            returnType: null,
            parameterTypes: [typeof(object), typeof(object)],
            m: property.DeclaringType!.Module,
            skipVisibility: true);

        var il = method.GetILGenerator();

        // Load instance (arg0)
        il.Emit(OpCodes.Ldarg_0);

        // Cast instance to declaring type
        il.Emit(OpCodes.Castclass, property.DeclaringType!);

        // Load value (arg1)
        il.Emit(OpCodes.Ldarg_1);

        // Convert object -> propertyType
        il.Emit(property.PropertyType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, property.PropertyType);

        // Call setter
        il.Emit(OpCodes.Callvirt, property.SetMethod!);

        il.Emit(OpCodes.Ret);

        return (Action<object, object>)method.CreateDelegate(typeof(Action<object, object>));
    }
}