using System.Reflection;
using Kernel;
using Microsoft.EntityFrameworkCore;
using OfX.EntityFrameworkCore.Extensions;
using OfX.Extensions;
using OfX.RabbitMq.Extensions;
using Service3Api;
using Service3Api.Contexts;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOfX(cfg =>
    {
        cfg.AddAttributesContainNamespaces(typeof(IKernelAssemblyMarker).Assembly);
        cfg.AddRabbitMq(config => config.Host("localhost", "/"));
    })
    .AddOfXEFCore(cfg =>
    {
        cfg.AddDbContexts(typeof(Service3Context));
        cfg.AddModelConfigurationsFromNamespaceContaining<IAssemblyMarker>();
    });

builder.Services.AddDbContextPool<Service3Context>(options =>
{
    options.UseNpgsql("Host=localhost;Username=postgres;Password=Abcd@2021;Database=OfXTestService3", b =>
    {
        b.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
        b.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
    });
}, 128);

var app = builder.Build();
app.StartRabbitMqListeningAsync();
app.Run();