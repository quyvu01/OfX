// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.DependencyInjection;
// using OfX.Abstractions;
// using OfX.EntityFrameworkCore.Extensions;
// using OfX.Extensions;
// using OfX.Nats.Extensions;
// using OfX.Tests.Contexts;
// using OfX.Tests.Models;
// using Xunit;
//
// namespace OfX.Tests;
//
// public sealed class OfXNatsTest : ServicesBuilding
// {
//     public OfXNatsTest()
//     {
//         var assembly = typeof(ITestAssemblyMarker).Assembly;
//         InstallService((serviceCollection, _) => serviceCollection
//                 .AddDbContext<TestDbContext>(opts => opts
//                     .UseInMemoryDatabase($"Test_{Guid.NewGuid()}")))
//             .InstallService((serviceCollection, _) =>
//             {
//                 serviceCollection.AddOfX(options =>
//                     {
//                         options.AddAttributesContainNamespaces(assembly);
//                         options.AddNats(config => config.Url("nats://localhost:4222"));
//                     })
//                     .AddOfXEFCore(options =>
//                     {
//                         options.AddDbContexts(typeof(TestDbContext));
//                         options.AddModelConfigurationsFromNamespaceContaining<ITestAssemblyMarker>();
//                     });
//             })
//             .InstallAllServices();
//         var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
//         dbContext.Users.AddRange(StaticData.StaticDataTest.Users);
//         dbContext.SaveChanges();
//     }
//
//     [Fact]
//     public async Task Test()
//     {
//         await Task.Yield();
//         var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
//         var member = new Member { UserId = "1" };
//         var user = await dbContext.Users.FirstOrDefaultAsync(a => a.Id == "1");
//         var dataMappableService = ServiceProvider.GetRequiredService<IDataMappableService>();
//         await dataMappableService.MapDataAsync(member);
//         Assert.Equal(user?.Name, member.UserName);
//         Assert.Equal(user?.Email, member.UserEmail);
//     }
// }