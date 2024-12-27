using Microsoft.Extensions.DependencyInjection;

namespace OfX.Registries;

public sealed record OfXServiceInjector(IServiceCollection Collection, OfXRegister OfXRegister);