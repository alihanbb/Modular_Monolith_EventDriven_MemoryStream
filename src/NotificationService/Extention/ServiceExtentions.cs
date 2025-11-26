using NotificationService.Email.Configuration;
using NotificationService.Email.Services;

namespace NotificationService.Extention
{
    public static class ServiceExtentions
    {
        public static IServiceCollection AddServiceExtentions(this IServiceCollection services, IConfiguration configuration)
        {
            var emailSettings = configuration.GetSection("EmailSettings").Get<EmailSettings>()
                ?? throw new InvalidOperationException("EmailSettings configuration is missing");

            services.AddSingleton(emailSettings);
            services.AddScoped<IEmailService, EmailService>();

            return services;
        }
    }
}
