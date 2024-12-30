namespace OfX.Abstractions;

public interface IOfXConfigAttribute
{
    public string IdProperty { get; }
    public string DefaultProperty { get; }
}