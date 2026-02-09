using System.Reflection;
using Amazon;
using Microsoft.EntityFrameworkCore;
using OfX.Aws.Sqs.Extensions;
using OfX.EntityFrameworkCore.Extensions;
using OfX.Extensions;
using OfX.Grpc.Extensions;
using OfX.Nats.Extensions;
using OfX.RabbitMq.Extensions;
using OfX.Supervision;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Serilog;
using Service3Api;
using Service3Api.Contexts;
using Shared;
using Shared.RunSqlMigration;

var builder = WebApplication.CreateBuilder(args);
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

// Configure OpenTelemetry for distributed tracing and metrics
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("Service3", serviceVersion: "1.0.0")
        .AddAttributes(new Dictionary<string, object>
        {
            ["environment"] = builder.Environment.EnvironmentName,
            ["host.name"] = Environment.MachineName
        }))
    .WithTracing(tracing => tracing
        .AddSource("OfX") // Subscribe to OfX traces
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317")))
    .WithMetrics(metrics => metrics
        .AddMeter("OfX") // Subscribe to OfX metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation());

builder.Services.AddOfX(cfg =>
    {
        cfg.AddAttributesContainNamespaces(typeof(IKernelAssemblyMarker).Assembly);
        cfg.AddModelConfigurationsFromNamespaceContaining<IAssemblyMarker>();
        cfg.ConfigureSupervisor(opts =>
        {
            opts.Strategy = SupervisionStrategy.OneForOne;
            opts.MaxRestarts = 5;
            opts.EnableCircuitBreaker = true;
            opts.CircuitBreakerThreshold = 3;
        });
        // cfg.AddRabbitMq(c => c.Host("localhost", "/"));
        // cfg.AddKafka(c => c.Host("localhost:9092"));
        // cfg.AddNats(c => c.Url("nats://localhost:4222"));
        cfg.AddSqs(c =>
        {
            c.Region(RegionEndpoint.USEast1, credential =>
            {
                credential.ServiceUrl("http://localhost:4566");
                credential.AccessKeyId("test");
                credential.SecretAccessKey("test");
            });
        });
        cfg.ThrowIfException();
    })
    .AddOfXEFCore(cfg => cfg.AddDbContexts(typeof(Service3Context)));
builder.Services.AddGrpc();

#region Setting Database and Seeding data

builder.Services.AddDbContextPool<Service3Context>(options =>
{
    options.UseNpgsql("Host=localhost;Username=postgres;Password=Abcd@2021;Database=OfXTestService3", b =>
    {
        b.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
        b.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
    });
}, 128);

#endregion

var app = builder.Build();

using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<Service3Context>();
await Service3Api.Data.Service3DataSeeder.SeedAsync(dbContext);
// app.MapOfXGrpcService();
app.Run();