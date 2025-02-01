using OfX.Grpc.Exceptions;
using OfX.Grpc.Statics;

namespace OfX.Grpc.ApplicationModels;

public class GrpcClientsRegister
{
    public GrpcClientsRegister AddGrpcHostWithOfXAttributes(string grpcHost, IEnumerable<Type> attributeTypes)
    {
        if (grpcHost is null)
            throw new OfXGrpcExceptions.GrpcHostMustNotBeNull();
        if (attributeTypes is null)
            throw new OfXGrpcExceptions.AttributeTypesCannotBeNull();
        var typesAdding = attributeTypes.ToList();
        if (GrpcStatics.HostMapAttributes.TryGetValue(grpcHost, out _))
            throw new OfXGrpcExceptions.GrpcHostHasBeenRegistered(grpcHost);
        var addTypes = GrpcStatics.HostMapAttributes.Values.SelectMany(a => a);
        if (typesAdding.Intersect(addTypes).Any())
            throw new OfXGrpcExceptions.SomeAttributesHasBeenRegisteredWithOtherHost();
        GrpcStatics.HostMapAttributes.Add(grpcHost, typesAdding);
        return this;
    }
}