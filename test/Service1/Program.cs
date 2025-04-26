using System.Reflection;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using OfX.EntityFrameworkCore.Extensions;
using OfX.Extensions;
using OfX.HotChocolate.Extensions;
using OfX.MongoDb.Extensions;
using OfX.Nats.Extensions;
using Serilog;
using Service1;
using Service1.Contexts;
using Service1.GraphQls;
using Service1.Models;
using Shared;
using Shared.RunSqlMigration;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var client = new MongoClient("mongodb://localhost:27017");
var database = client.GetDatabase("Service1MongoDb");
var memberSocialCollection = database.GetCollection<MemberSocial>("MemberSocials");

// Seeding MemberSocials

foreach (var memberSocialId in Enumerable.Range(1, 3))
{
    var existed = await memberSocialCollection
        .Find(m => m.Id.Equals(memberSocialId))
        .FirstOrDefaultAsync();
    if (existed is null)
        await memberSocialCollection.InsertOneAsync(new MemberSocial
            { Id = memberSocialId, Name = $"Social name: {memberSocialId}" });
}


var registerBuilder = builder.Services.AddGraphQLServer()
    .AddQueryType<Query>()
    .AddType<MembersType>();

builder.Services.AddOfX(cfg =>
    {
        cfg.AddAttributesContainNamespaces(typeof(IKernelAssemblyMarker).Assembly);
        cfg.AddModelConfigurationsFromNamespaceContaining<IAssemblyMarker>();
        cfg.AddNats(config => config.Url("nats://localhost:4222"));
        // cfg.ThrowIfException();
        cfg.SetMaxObjectSpawnTimes(16);
        cfg.SetRequestTimeOut(TimeSpan.FromSeconds(10));
    })
    .AddOfXEFCore(cfg => cfg.AddDbContexts(typeof(Service1Context), typeof(OtherService1Context)))
    .AddMongoDb(cfg => cfg.AddCollection(memberSocialCollection))
    .AddHotChocolate(cfg => cfg.AddRequestExecutorBuilder(registerBuilder));

List<string> provinceIds =
[
    "01962f9a-f7f8-7f61-941c-6a086fe96cd2", "01962f9a-f7f8-7b4c-9b4d-eae8ea6e5fc7",
    "01962f9a-f7f8-7e54-a79d-575a8e882eb8"
];
builder.Services.AddDbContextPool<Service1Context>(options =>
{
    options.UseNpgsql("Host=localhost;Username=postgres;Password=Abcd@2021;Database=OfXTestService1", b =>
    {
        b.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
        b.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
    }).UseAsyncSeeding(async (context, _, cancellationToken) =>
    {
        var memberAdditionalDataSet = context.Set<MemberAdditionalData>();
        foreach (var id in Enumerable.Range(1, 3))
        {
            var existedMemberAdditionalData = await memberAdditionalDataSet
                .FirstOrDefaultAsync(a => a.Id == id.ToString(), cancellationToken);
            if (existedMemberAdditionalData == null)
                memberAdditionalDataSet.Add(new MemberAdditionalData { Id = id.ToString(), Name = $"Joie: {id}" });
        }

        await context.SaveChangesAsync(cancellationToken);
    });
}, 128);

builder.Services.AddDbContextPool<OtherService1Context>(options =>
{
    options.UseNpgsql("Host=localhost;Username=postgres;Password=Abcd@2021;Database=OfXTestOtherService1", b =>
        {
            b.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
            b.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
        })
        .UseAsyncSeeding(async (context, _, cancellationToken) =>
        {
            var memberAddressSet = context.Set<MemberAddress>();
            foreach (var id in Enumerable.Range(1, 3))
            {
                var existedMemberAddress = await memberAddressSet
                    .FirstOrDefaultAsync(a => a.Id == id.ToString(), cancellationToken);
                if (existedMemberAddress is null)
                    memberAddressSet.Add(new MemberAddress
                        { Id = id.ToString(), ProvinceId = provinceIds.ElementAtOrDefault(id - 1) });
            }

            await context.SaveChangesAsync(cancellationToken);
        });
}, 128);


builder.Services.AddControllers();

var app = builder.Build();

await MigrationDatabase.MigrationDatabaseAsync<Service1Context>(app);
await MigrationDatabase.MigrationDatabaseAsync<OtherService1Context>(app);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.MapGraphQL();

app.Run();