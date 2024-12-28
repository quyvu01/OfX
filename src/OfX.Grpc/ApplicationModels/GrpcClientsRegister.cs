using OfX.Grpc.Exceptions;

namespace OfX.Grpc.ApplicationModels;

public class GrpcClientsRegister
{
    public Dictionary<string, List<Type>> HostMapAttributes { get; } = [];

    public GrpcClientsRegister AddGrpcHostWithOfXAttributes(string grpcHost, IEnumerable<Type> attributeTypes)
    {
        if (grpcHost is null)
            throw new OfXGrpcExceptions.GrpcHostMustNotBeNull();
        if (attributeTypes is null)
            throw new OfXGrpcExceptions.AttributeTypesCannotBeNull();
        var typesAdding = attributeTypes.ToList();
        if (HostMapAttributes.TryGetValue(grpcHost, out _))
            throw new OfXGrpcExceptions.GrpcHostHasBeenRegistered(grpcHost);
        var addTypes = HostMapAttributes.Values.SelectMany(a => a);
        if (typesAdding.Intersect(addTypes).Any())
            throw new OfXGrpcExceptions.SomeAttributesHasBeenRegisteredWithOtherHost();
        HostMapAttributes.Add(grpcHost, typesAdding);
        return this;
    }
}