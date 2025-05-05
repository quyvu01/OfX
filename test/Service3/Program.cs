using System.Reflection;
using Microsoft.EntityFrameworkCore;
using OfX.EntityFrameworkCore.Extensions;
using OfX.Extensions;
using OfX.Nats.Extensions;
using Service3Api;
using Service3Api.Contexts;
using Service3Api.Models;
using Service3Api.Pipelines;
using Shared;
using Shared.RunSqlMigration;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOfX(cfg =>
    {
        cfg.AddAttributesContainNamespaces(typeof(IKernelAssemblyMarker).Assembly);
        cfg.AddNats(config => config.Url("nats://localhost:4222"));
        cfg.AddModelConfigurationsFromNamespaceContaining<IAssemblyMarker>();
        cfg.AddReceivedPipelines(c => c.OfType(typeof(TestReceivedPipeline<>)));
    })
    .AddOfXEFCore(cfg => cfg.AddDbContexts(typeof(Service3Context)));

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

var app = builder.Build();

await MigrationDatabase.MigrationDatabaseAsync<Service3Context>(app);


app.Run();