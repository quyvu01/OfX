using System.Reflection;
using OfX.Grpc.Exceptions;

namespace OfX.Grpc.ApplicationModels;

public class GrpcClientsRegister
{
    public Dictionary<Assembly, string> AssembliesHostLookup { get; private set; } = [];

    public GrpcClientsRegister RegisterForAssembly<TAssemblyMarker>(string serverHost)
    {
        var assembly = typeof(TAssemblyMarker).Assembly;
        if (!AssembliesHostLookup.TryAdd(assembly, serverHost))
            throw new OfXGrpcExceptions.GrpcClientAssemblyExisted(assembly);
        return this;
    }
}