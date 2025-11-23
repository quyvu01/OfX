namespace OfX.Tests.StronglyTypes;

public record ProvinceId(Guid Value) : StronglyTypedId<Guid>(Value)
{
    public override string ToString() => base.ToString();
}