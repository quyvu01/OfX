using System.Reflection;
using Kernel;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using OfX.EntityFrameworkCore.Extensions;
using OfX.Extensions;
using OfX.Kafka.Extensions;
using WorkerService1;
using WorkerService1.Contexts;
using WorkerService1.StronglyTypeIdsRegisters;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOfX(cfg =>
    {
        cfg.AddAttributesContainNamespaces(typeof(IKernelAssemblyMarker).Assembly);
        // cfg.AddRabbitMq(config => config.Host("localhost", "/"));
        cfg.AddStronglyTypeIdConverter(a => a.OfType<StronglyTypeIdRegisters>());
        cfg.AddKafka(c => c.Host("localhost:9092"));
    })
    .AddOfXEFCore(cfg =>
    {
        cfg.AddDbContexts(typeof(Service2Context));
        cfg.AddModelConfigurationsFromNamespaceContaining<IAssemblyMarker>();
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
app.StartKafkaMqListeningAsync();
app.Run();