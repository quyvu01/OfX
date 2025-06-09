using OfX.Extensions;
using OfX.Grpc.Statics;

namespace OfX.Grpc.ApplicationModels;

public class GrpcClientsRegister
{
    public void AddGrpcHosts(params string[] serviceHosts)
    {
        serviceHosts?.ForEach(a =>
        {
            if (!GrpcStatics.ServiceHosts.Contains(a)) GrpcStatics.ServiceHosts.Add(a);
        });
    }

    public void AddGrpcHosts(string serviceHost)
    {
        ArgumentNullException.ThrowIfNull(serviceHost);
        if (!GrpcStatics.ServiceHosts.Contains(serviceHost)) GrpcStatics.ServiceHosts.Add(serviceHost);
    }
}