namespace OfX.Agents
{
    public interface IOfXAgent
    {
        /// <summary>
        /// Completes when agent is ready to work
        /// </summary>
        Task Ready { get; }

        /// <summary>
        /// Completes when agent has fully stopped
        /// </summary>
        Task Completed { get; }

        /// <summary>
        /// Canceled when agent begins stopping
        /// </summary>
        CancellationToken Stopping { get; }

        /// <summary>
        /// Canceled when agent is fully stopped
        /// </summary>
        CancellationToken Stopped { get; }

        Task StopAsync(string reason, CancellationToken cancellationToken = default);
    }
}


// 10.Bus Control

// ================== IOfXBusControl.cs ==================
namespace OfX
{
    //     11.Usage Example
//
// // ================== Program.cs ==================
//     var builder = Host.CreateApplicationBuilder(args);
//
// // Configure OfX
//     builder.Services.AddSingleton<RabbitMqSettings>(_ => new RabbitMqSettings
//     {
//         Host = "localhost",
//         Username = "guest",
//         Password = "guest"
//     });
//
//     builder.Services.AddSingleton<IConnectionContextFactory, RabbitMqConnectionContextFactory>();
//     builder.Services.AddSingleton<IRetryPolicy>(_ => new ExponentialRetryPolicy(
//         nonRetryableExceptions: typeof(AuthenticationException)));
//
//     builder.Services.AddSingleton<ConnectionContextSupervisor>();
//     builder.Services.AddSingleton<IOfXBusControl>(sp =>
//     {
//         var bus = new OfXBus(sp.GetRequiredService<ConnectionContextSupervisor>());
//
//         // Add receive endpoints
//         bus.AddReceiveEndpoint("my-queue", async message =>
//         {
//             Console.WriteLine($"Received: {message}");
//             await Task.CompletedTask;
//         });
//
//         return bus;
//     });
//
//     builder.Services.AddHostedService<OfXHostedService>();
//     builder.Services.Configure<OfXHostOptions>(o => o.WaitUntilStarted = true);
//
//     var host = builder.Build();
//     await host.RunAsync();
}