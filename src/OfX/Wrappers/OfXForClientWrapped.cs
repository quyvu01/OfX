using OfX.Registries;

namespace OfX.Wrappers;

public sealed record OfXForClientWrapped
{
    public OfXRegister OfXRegister { get; }

    private OfXForClientWrapped(OfXRegister OfXRegister) => this.OfXRegister = OfXRegister;

    public static OfXForClientWrapped Of(OfXRegister OfXRegister) => new(OfXRegister);
}