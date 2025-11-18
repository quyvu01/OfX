using System.Reflection;

namespace OfX.Accessors;

public class OfXTypeModel
{
    public Type ClrType { get; }
    private IReadOnlyDictionary<string, IOfXPropertyAccessor> Properties { get; }

    public OfXTypeModel(Type clrType)
    {
        ClrType = clrType;
        Properties = clrType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(p => p.Name, p => (IOfXPropertyAccessor)Activator
                .CreateInstance(typeof(OfXPropertyAccessor<,>).MakeGenericType(clrType, p.PropertyType), p)!);
    }

    public IOfXPropertyAccessor GetProperty(string name) => Properties.GetValueOrDefault(name);
}