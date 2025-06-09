namespace OfX.Grpc.Exceptions;

public static class OfXGrpcExceptions
{
    public sealed class CannotDeserializeOfXAttributeType(string type)
        : Exception($"The OfX Attribute seems not a part of this application: {type}!");
}