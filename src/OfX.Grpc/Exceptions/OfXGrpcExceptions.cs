namespace OfX.Grpc.Exceptions;

public static class OfXGrpcExceptions
{
    public class GrpcHostMustNotBeNull()
        : Exception("Grpc host must not be null!");

    public class GrpcHostHasBeenRegistered(string host)
        : Exception($"Grpc host: {host} has been registered!");

    public sealed class SomeAttributesHasBeenRegisteredWithOtherHost()
        : Exception("Some attributes has been registered with other host. Please check again!");

    public sealed class AttributeTypesCannotBeNull()
        : Exception("Cannot register nullable attributes with a host!");

    public sealed class CannotDeserializeOfXAttributeType(string type)
        : Exception($"The OfX Attribute seems not a part of this application: {type}!");
    
    public sealed class CannotFindHandlerForOfAttribute(Type type)
        : Exception($"Cannot find handler for OfXAttribute type: {type.Name}!");
}