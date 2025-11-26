using MassTransit;
using SearchService.Consumers;
using SearchService.Data;
using SearchService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient<AuctionServiceHttpClient>();
builder.Services.AddAutoMapper(cfg => { cfg.LicenseKey = builder.Configuration.GetValue<string>("AutoMapper:LicenseKey"); }, typeof(Program));
builder.Services.AddMassTransit(config =>
{
    config.AddConsumersFromNamespaceContaining<AuctionCreatedConsumer>();
    config.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("search", false));
    config.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration.GetValue<string>("RabbitMQ:Host"), h =>
        {
            h.Username(builder.Configuration.GetValue<string>("RabbitMQ:Username"));
            h.Password(builder.Configuration.GetValue<string>("RabbitMQ:Password"));
        });
        cfg.ReceiveEndpoint("search-auction-created", e =>
        {
            e.UseMessageRetry(r => r.Interval(5, 5));
            e.ConfigureConsumer<AuctionCreatedConsumer>(ctx);
        });
        cfg.ReceiveEndpoint("search-auction-updated", e =>
        {
            e.UseMessageRetry(r => r.Interval(5, 5));
            e.ConfigureConsumer<AuctionUpdatedConsumer>(ctx);
        });
        cfg.ReceiveEndpoint("search-auction-deleted", e =>
        {
            e.UseMessageRetry(r => r.Interval(5, 5));
            e.ConfigureConsumer<AuctionDeletedConsumer>(ctx);
        });
        cfg.ConfigureEndpoints(ctx);
    });
});

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();

try
{
    await DbInitializer.InitDb(app);
}
catch (Exception ex)
{
    Console.WriteLine(ex);
}

app.Run();
