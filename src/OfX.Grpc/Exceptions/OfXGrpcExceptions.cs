using System.Reflection;

namespace OfX.Grpc.Exceptions;

public static class OfXGrpcExceptions
{
    public sealed class GrpcClientAssemblyExisted(Assembly assemblyType)
        : Exception($"Assembly {assemblyType.FullName} has been register!");

    public class GrpcClientQueryTypeNotRegistered(Type requestType)
        : Exception($"QueryType: {requestType.Name} is not registered!");

    public sealed class SomeGrpcClientAssemblyAreNotRegistered()
        : Exception("Some assemblies are not register on root. Please check again!");

    public sealed class CannotDeserializeContractType() : Exception(
        "Cannot deserialize contract type, may it is not a part of this application as well. Please check again!");
    
    public sealed class CannotFindHandlerForQueryContract(Type requestType) : Exception(
        $"Cannot find the handler for query: {requestType.Name}. Please check again!");
}