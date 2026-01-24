using System.Reflection;
using Microsoft.AspNetCore.Builder;
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
using Service2;
using Service2.Contexts;
using Service2.Models;
using Shared;
using Shared.RunSqlMigration;

var builder = WebApplication.CreateBuilder(args);

// Configure OpenTelemetry for distributed tracing and metrics
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("Service2", serviceVersion: "1.0.0")
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
        // cfg.AddK  activity.SetOfXTags(expression: requestContext.Query.Expression,
        //                       selectorIds: requestContext.Query.SelectorIds);afka(c => c.Host("localhost:9092"));
        // cfg.AddNats(c => c.Url("nats://localhost:4222")); // Use NATS for telemetry demo
    })
    .AddOfXEFCore(cfg => cfg.AddDbContexts(typeof(Service2Context)));

#region Setting Database and Seeding data

List<string> provinceIds =
[
    "01962f9a-f7f8-7f61-941c-6a086fe96cd2", "01962f9a-f7f8-7b4c-9b4d-eae8ea6e5fc7",
    "01962f9a-f7f8-7e54-a79d-575a8e882eb8"
];

builder.Services.AddDbContextPool<Service2Context>(options =>
{
    options.UseNpgsql("Host=localhost;Username=postgres;Password=Abcd@2021;Database=OfXTestService2", b =>
        {
            b.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
            b.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
        })
        .UseAsyncSeeding(async (context, _, cancellationToken) =>
        {
            var userSet = context.Set<User>();
            foreach (var id in Enumerable.Range(1, 3))
            {
                var existedUser = await userSet
                    .FirstOrDefaultAsync(a => a.Id == id.ToString(), cancellationToken);
                if (existedUser is null)
                    userSet.Add(new User
                    {
                        Id = id.ToString(), ProvinceId = provinceIds.ElementAtOrDefault(id - 1),
                        Name = $"Some user: {id}", Email = $"usr_{id}@gg.coo"
                    });
            }

            await context.SaveChangesAsync(cancellationToken);
        });
}, 128);

#endregion

builder.Services.AddGrpc();

var app = builder.Build();

await MigrationDatabase.MigrationDatabaseAsync<Service2Context>(app);

app.MapOfXGrpcService();
app.Run();