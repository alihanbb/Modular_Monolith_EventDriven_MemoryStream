using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PaymentService.Payment;
using PaymentService.Payment.Featrue;

namespace PaymentService.Extentions
{
    public static class PaymentExtention
    {
        public static IServiceCollection AddServiceExtention(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<PaymentDbContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
            });

            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

            services.AddValidatorsFromAssemblyContaining<ProcessPaymentRequestValidator>();
            return services;
        }
    }
}
