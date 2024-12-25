using Microsoft.Extensions.DependencyInjection;

namespace OfX.EntityFrameworkCore.ApplicationModels;

public sealed record OfXEfCoreServiceInjector(IServiceCollection Collection);