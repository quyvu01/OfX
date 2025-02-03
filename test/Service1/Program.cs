using Kernel;
using Kernel.Attributes;
using OfX.Extensions;
using OfX.Grpc.Extensions;
using OfX.Nats.Extensions;
using OfX.RabbitMq.Extensions;
using Service1.Pipelines;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddOfX(cfg =>
{
    cfg.AddAttributesContainNamespaces(typeof(IKernelAssemblyMarker).Assembly);
    cfg.AddRabbitMq(config => config.Host("localhost", "/"));

    cfg.AddGrpcClients(config => config
        .AddGrpcHostWithOfXAttributes("http://localhost:5001", [typeof(UserOfAttribute)])
        .AddGrpcHostWithOfXAttributes("http://localhost:5013", [typeof(ProvinceOfAttribute), typeof(CountryOfAttribute)])
    );
    cfg.AddSendPipelines(c => c.OfType(typeof(TestSendPipeline<>)));
});

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();