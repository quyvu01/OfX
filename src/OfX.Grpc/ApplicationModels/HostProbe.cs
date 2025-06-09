namespace OfX.Grpc.ApplicationModels;

public sealed record HostProbe(string ServiceHost, bool IsProbed);