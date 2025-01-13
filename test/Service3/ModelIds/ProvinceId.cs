using Kernel.ModelIds;

namespace Service3Api.ModelIds;

public sealed record ProvinceId(Guid Value) : StronglyTypedId<Guid>(Value)
{
    public override string ToString() => base.ToString();
}