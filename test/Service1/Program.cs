using Kernel;
using OfX.Extensions;
using OfX.Nats.Extensions;
using Service1.Pipelines;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddOfX(cfg =>
{
    cfg.AddAttributesContainNamespaces(typeof(IKernelAssemblyMarker).Assembly);
    cfg.AddNats(options =>
    {
        options.Url("nats://localhost:4222");
        options.TopicPrefix("Staging");
    });
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