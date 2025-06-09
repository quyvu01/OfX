using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using OfX.EntityFrameworkCore.Extensions;
using OfX.Extensions;
using OfX.Grpc.Extensions;
using OfX.Nats.Extensions;
using Service2;
using Service2.Contexts;
using Service2.Models;
using Shared;
using Shared.RunSqlMigration;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOfX(cfg =>
    {
        cfg.AddAttributesContainNamespaces(typeof(IKernelAssemblyMarker).Assembly);
        // cfg.AddNats(config => config.Url("nats://localhost:4222"));
        cfg.AddModelConfigurationsFromNamespaceContaining<IAssemblyMarker>();
    })
    .AddOfXEFCore(cfg => cfg.AddDbContexts(typeof(Service2Context)));

List<string> provinceIds =
[
    "01962f9a-f7f8-7f61-941c-6a086fe96cd2", "01962f9a-f7f8-7b4c-9b4d-eae8ea6e5fc7",
    "01962f9a-f7f8-7e54-a79d-575a8e882eb8"
];

builder.Services.AddGrpc();

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

var app = builder.Build();

await MigrationDatabase.MigrationDatabaseAsync<Service2Context>(app);

app.MapOfXGrpcService();
app.Run();