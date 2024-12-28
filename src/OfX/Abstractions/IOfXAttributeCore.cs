namespace OfX.Abstractions;

public interface IOfXAttributeCore
{
    string PropertyName { get; }
    string Expression { get; set; }
    int Order { get; set; }
}

public interface IDataMappableCore : IOfXAttributeCore;