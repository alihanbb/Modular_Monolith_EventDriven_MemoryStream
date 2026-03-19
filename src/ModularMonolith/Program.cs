using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using ModularMonolith;
using ModularMonolith.Modules.Loyalty;
using ModularMonolith.Modules.Notification;
using ModularMonolith.Modules.Payment;
using ModularMonolith.Shared.Infrastructure.EventBus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInMemoryEventBus();
builder.Services.AddTelemetry(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Version = "v1",
        Title = "Modular Monolith API",
        Description = "Modular monolith architecture and Inmemory outbox pattern implemantation",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Alihan Berat Çelik",
            Email = "alihancelik@gmail.com"
        }

    });
});

builder.Services.AddPaymentModule(builder.Configuration);
builder.Services.AddLoyaltyModule(builder.Configuration);
builder.Services.AddNotificationModule(
    builder.Configuration,
    builder.Environment
);

builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        tags: new[] { "ready" }
    );


var app = builder.Build();
await app.MigrateAllDatabasesAsync();

app.MapHealthChecks("/healthz/live", new HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/healthz/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Modular Monolith API v1");
        options.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();


app.MapPaymentEndpoints();
app.MapLoyaltyEndpoints();

app.Run();
