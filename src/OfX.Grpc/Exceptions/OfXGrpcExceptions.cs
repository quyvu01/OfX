using System.Reflection;

namespace OfX.Grpc.Exceptions;

public static class OfXGrpcExceptions
{
    public sealed class GrpcClientAssemblyExisted(Assembly assemblyType)
        : Exception($"Assembly {assemblyType.FullName} has been register!");

    public class GrpcClientQueryTypeNotRegistered(Type requestType)
        : Exception($"QueryType: {requestType.Name} is not registered!");
    
    public sealed class SomeGrpcClientAssemblyAreNotRegistered() : Exception("Some assemblies are not register on root, please check again!");
}