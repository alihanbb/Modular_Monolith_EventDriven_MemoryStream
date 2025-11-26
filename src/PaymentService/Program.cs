using MassTransit;
using PaymentService.Payment.Featrue;
using PaymentService.Extentions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServiceExtention(builder.Configuration);

builder.Services.AddMassTransit(x =>
{
    x.UsingInMemory((context, cfg) =>
    {
        cfg.UseMessageRetry(r =>
              r.Incremental(
                  retryLimit: 5,
                  initialInterval: TimeSpan.FromSeconds(1),
                  intervalIncrement: TimeSpan.FromSeconds(2)
              )
        );

        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

// Auto Migration
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentService.Payment.PaymentDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
}
app.UseHttpsRedirection();
ProcessPaymentEndpoint.MapEndpoint(app);
app.Run();
