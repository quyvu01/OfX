namespace OfX.Abstractions;

public interface IIdConverter
{
    object ConvertIds(List<string> selectorIds);
}

public interface IIdConverter<TId> : IIdConverter;