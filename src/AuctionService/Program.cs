using AuctionService.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<AuctionDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAutoMapper(cfg => { cfg.LicenseKey = builder.Configuration.GetValue<string>("AutoMapper:LicenseKey"); }, typeof(Program));
builder.Services.AddMassTransit(config =>
{
    config.AddEntityFrameworkOutbox<AuctionDbContext>(o =>
    {
        o.QueryDelay = TimeSpan.FromSeconds(10);
        o.UsePostgres();
        o.UseBusOutbox();
    });

    //config.AddConsumersFromNamespaceContaining<AuctionConsumer>();
    config.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("auction", false));

    config.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration.GetValue<string>("RabbitMQ:Host"), h =>
        {
            h.Username(builder.Configuration.GetValue<string>("RabbitMQ:Username"));
            h.Password(builder.Configuration.GetValue<string>("RabbitMQ:Password"));
        });
        cfg.ConfigureEndpoints(ctx);
    });
});

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();

try
{
    DbInitializer.InitDb(app);
}
catch (System.Exception e)
{
    Console.WriteLine(e);
}

app.Run();

