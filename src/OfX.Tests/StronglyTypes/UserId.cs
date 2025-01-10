namespace OfX.Tests.StronglyTypes;

public record UserId(Guid Value) : StronglyTypedId<Guid>(Value)
{
    public override string ToString() => base.ToString();
}