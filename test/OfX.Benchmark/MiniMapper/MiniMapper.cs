using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

namespace OfX.Benchmark.MiniMapper;

public static class MiniMapper
{
    private static readonly ConcurrentDictionary<(Type, Type), Delegate> Cache 
        = new();

    public static TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
    {
        var key = (typeof(TSource), typeof(TDestination));

        var del = (Action<TSource, TDestination>)Cache.GetOrAdd(key, static _ => BuildIl<TSource, TDestination>());

        del(source, destination);
        return destination;
    }

    private static Action<TSource, TDestination> BuildIl<TSource, TDestination>()
    {
        var method = new DynamicMethod(
            name: "MiniMapperInvoker",
            returnType: null,
            parameterTypes: [typeof(TSource), typeof(TDestination)],
            m: typeof(MiniMapper).Module,
            skipVisibility: true
        );

        var il = method.GetILGenerator();

        var srcProps = typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToDictionary(p => p.Name);

        var destProps = typeof(TDestination).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite);

        foreach (var dp in destProps)
        {
            if (!srcProps.TryGetValue(dp.Name, out var sp)) continue;
            if (dp.PropertyType != sp.PropertyType) continue;

            // dest.<prop> = src.<prop>
            il.Emit(OpCodes.Ldarg_1);               // dest
            il.Emit(OpCodes.Ldarg_0);               // src
            il.Emit(OpCodes.Callvirt, sp.GetMethod!);  // src.get_Prop
            il.Emit(OpCodes.Callvirt, dp.SetMethod!);  // dest.set_Prop
        }

        il.Emit(OpCodes.Ret);

        return (Action<TSource, TDestination>)method.CreateDelegate(typeof(Action<TSource, TDestination>));
    }
}