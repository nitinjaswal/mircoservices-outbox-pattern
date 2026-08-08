using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(builder.Configuration
        .GetConnectionString("DefaultConnection")));

//
builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<OrderDbContext>(o =>
    {
        o.UseSqlServer();
        o.UseBusOutbox();

        //we can customize UseBusOutbox
        //o.UseBusOutbox(bo =>
        //{
        //    bo.MessageDeliveryLimit = 10;        // process 10 messages per cycle
        //    bo.MessageDeliveryTimeout = TimeSpan.FromSeconds(10); // delivery timeout
        //});
        //o.QueryDelay = TimeSpan.FromSeconds(1);  // polling interval ← default 1s
        //o.QueryMessageLimit = 100;               // max messages per query
        //o.QueryTimeout = TimeSpan.FromSeconds(30); // DB query timeout
        //This is the background service MassTransit registers via UseBusOutbox().
        //Poll OutboxState and publish messages to the broker in a single transaction.
    });

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });

        cfg.ConfigureEndpoints(ctx);
    });
});

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    for (int i = 0; i < 10; i++)
    {
        try { db.Database.Migrate(); logger.LogInformation("DB migrated"); break; }
        catch (Exception ex) { logger.LogWarning("DB not ready ({N}/10): {Msg}", i + 1, ex.Message); Thread.Sleep(3000); }
    }
}

app.Run();