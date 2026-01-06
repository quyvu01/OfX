namespace OfX.Grpc.Exceptions;

/// <summary>
/// Contains exception types specific to the OfX gRPC transport.
/// </summary>
public static class OfXGrpcExceptions
{
    /// <summary>
    /// Thrown when the server receives a request for an attribute type that is not registered in this application.
    /// </summary>
    /// <param name="type">The assembly-qualified type name that could not be deserialized.</param>
    public sealed class CannotDeserializeOfXAttributeType(string type)
        : Exception($"The OfX Attribute seems not a part of this application: {type}!");
}