using System.Reflection;
using Kernel;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using OfX.EntityFrameworkCore.Extensions;
using OfX.Extensions;
using OfX.Nats.Extensions;
using WorkerService1;
using WorkerService1.Contexts;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOfX(cfg =>
    {
        cfg.AddAttributesContainNamespaces(typeof(IKernelAssemblyMarker).Assembly);
        // cfg.AddRabbitMq(config => config.Host("localhost", "/"));
        cfg.AddNats(config => config.Url("nats://localhost:4222"));
        cfg.AddModelConfigurationsFromNamespaceContaining<IAssemblyMarker>();
    })
    .AddOfXEFCore(cfg =>
    {
        cfg.AddDbContexts(typeof(Service2Context));
    });

builder.Services.AddDbContextPool<Service2Context>(options =>
{
    options.UseNpgsql("Host=localhost;Username=postgres;Password=Abcd@2021;Database=OfXTestService2", b =>
    {
        b.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
        b.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
    });
}, 128);

var app = builder.Build();

app.Run();