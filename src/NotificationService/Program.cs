using NotificationService.Extention;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServiceExtentions(builder.Configuration);
builder.Services.AddMassTransitConfiguration();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
