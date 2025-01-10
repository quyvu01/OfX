namespace OfX.Abstractions;

public interface IStronglyTypeConverter;

public interface IStronglyTypeConverter<out TId> : IStronglyTypeConverter
{
    TId Convert(string input);
    bool CanConvert(string input);
}