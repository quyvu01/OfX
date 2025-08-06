using OfX.Extensions;
using OfX.Grpc.Extensions;
using Service4.Integration;
using Service4.Integration.Pipelines;
using Shared;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddOfX(cfg =>
    {
        cfg.AddAttributesContainNamespaces(typeof(IKernelAssemblyMarker).Assembly);
        // cfg.AddNats(config => config.Url("nats://localhost:4222"));
        cfg.AddDefaultReceiversFromNamespaceContaining<IAssemblyMarker>();
        cfg.AddCustomExpressionPipelines(a => a
            .OfType<TestReceivedPipeline>()
            .OfType<OtherTestReceivedPipeline>()
        );
    });
builder.Services.AddGrpc();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapOfXGrpcService();

app.Run();