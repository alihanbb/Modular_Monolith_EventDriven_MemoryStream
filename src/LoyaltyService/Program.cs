using Loyalty;
using LoyaltyService.Consumer;
using LoyaltyService.Loyalty;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LoyaltyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PaymentSuccessConsumer>();

    x.UsingInMemory((context, cfg) =>
    {
        cfg.ReceiveEndpoint("loyalty-queue", e =>
        {
            e.UseMessageRetry(r =>
              r.Incremental(
                  retryLimit: 5,
                  initialInterval: TimeSpan.FromSeconds(1),
                  intervalIncrement: TimeSpan.FromSeconds(2)
              )
  );

            e.ConfigureConsumer<PaymentSuccessConsumer>(context);
        });
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

// Auto Migration
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LoyaltyDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

