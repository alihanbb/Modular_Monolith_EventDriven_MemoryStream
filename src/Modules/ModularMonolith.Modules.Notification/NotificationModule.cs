#region usings
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModularMonolith.Modules.Notification.Application.EventHandlers;
using ModularMonolith.Modules.Notification.Domain.Configuration;
using ModularMonolith.Modules.Notification.Domain.Services;
using ModularMonolith.Modules.Notification.Infrastructure.Services;
using ModularMonolith.Shared.Events;
using ModularMonolith.Shared.Infrastructure.EventBus;
#endregion

namespace ModularMonolith.Modules.Notification;

public static class NotificationModule
{
    public static IServiceCollection AddNotificationModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var emailSettings = configuration.GetSection("Email").Get<EmailSettings>()
            ?? new EmailSettings();
        services.AddSingleton(emailSettings);

        if (environment.IsDevelopment())
            services.AddScoped<IEmailService, DevEmailService>();
        else
            services.AddScoped<IEmailService, EmailService>();

        services.AddEventHandler<PaymentSucceededEvent, PaymentSucceededEventHandler>();
        services.AddEventHandler<PointsAddedEvent, PointsAddedEventHandler>();
        services.AddEventHandler<EmailSentEvent, EmailSentEventHandler>();

        return services;
    }
}
