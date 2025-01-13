namespace Kernel.ModelIds;

public record StronglyTypedId<TValue>(TValue Value) where TValue : notnull
{
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();
}