using System.Reflection;
using Microsoft.EntityFrameworkCore;
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
using Service3Api.Models;
using Shared;

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
        // cfg.AddNats(c => c.Url("nats://localhost:4222")); // Use NATS for telemetry demo
    })
    .AddOfXEFCore(cfg => cfg.AddDbContexts(typeof(Service3Context)));
builder.Services.AddGrpc();

#region Setting Database and Seeding data

Dictionary<string, List<string>> countryMapProvinces = new()
{
    { "abc", ["01962f9a-f7f8-7f61-941c-6a086fe96cd2", "01962f9a-f7f8-7b4c-9b4d-eae8ea6e5fc7"] },
    { "xyz", ["01962f9a-f7f8-7e54-a79d-575a8e882eb8"] },
};
builder.Services.AddDbContextPool<Service3Context>(options =>
{
    options.UseNpgsql("Host=localhost;Username=postgres;Password=Abcd@2021;Database=OfXTestService3", b =>
    {
        b.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
        b.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
    }).UseAsyncSeeding(async (context, _, cancellationToken) =>
    {
        var countrySet = context.Set<Country>();
        foreach (var kpv in countryMapProvinces)
        {
            var existedCountry = await countrySet
                .FirstOrDefaultAsync(x => x.Id == kpv.Key, cancellationToken);
            if (existedCountry == null)
                countrySet.Add(new Country
                {
                    Id = kpv.Key, Name = $"Country-Id: {kpv.Key}", Provinces =
                    [
                        ..kpv.Value.Select((a, index) => new Province
                        {
                            Id = Guid.Parse(a), Name = $"Province Of country: {kpv.Key} with index: {index}"
                        })
                    ]
                });
        }

        await context.SaveChangesAsync(cancellationToken);
    });
}, 128);

#endregion

var app = builder.Build();

// await MigrationDatabase.MigrationDatabaseAsync<Service3Context>(app);

app.MapOfXGrpcService();
app.Run();