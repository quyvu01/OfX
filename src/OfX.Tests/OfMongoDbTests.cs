using MongoDB.Driver;
using OfX.Extensions;
using OfX.MongoDb.Extensions;
using OfX.Nats.Extensions;
using OfX.Tests.Models;

namespace OfX.Tests;

public class OfMongoDbTests : ServicesBuilding
{
    public OfMongoDbTests()
    {
        var client = new MongoClient("mongodb://localhost:27017");
        var database = client.GetDatabase("OfMongoDbTests");
        var memberAddressCollection = database.GetCollection<MemberAddress>("MemberAddresses");
        var otherMemberAddressCollection = database.GetCollection<OtherMemberAddressMongo>("OtherMemberAddressMongos");
        var assembly = typeof(ITestAssemblyMarker).Assembly;
        InstallService((serviceCollection, _) => serviceCollection.AddOfX(options =>
                {
                    options.AddAttributesContainNamespaces(assembly);
                    options.AddHandlersFromNamespaceContaining<ITestAssemblyMarker>();
                    options.AddNats(config => config.Url("nats://localhost:4222"));
                    options.AddModelConfigurationsFromNamespaceContaining<ITestAssemblyMarker>();
                })
                .AddMongoDb(c => c
                    .AddCollection(memberAddressCollection)
                    .AddCollection(otherMemberAddressCollection)
                ))
            .InstallAllServices();
    }
}