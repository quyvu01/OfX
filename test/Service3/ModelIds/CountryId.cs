using Kernel.ModelIds;

namespace Service3Api.ModelIds;

public sealed record CountryId(string Value) : StronglyTypedId<string>(Value)
{
    public override string ToString() => base.ToString();
}