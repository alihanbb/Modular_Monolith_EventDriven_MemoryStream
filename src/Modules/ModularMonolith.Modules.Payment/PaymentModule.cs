#region usings
using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularMonolith.Modules.Payment.API.Endpoints;
using ModularMonolith.Modules.Payment.Application.Commands;
using ModularMonolith.Modules.Payment.Infrastructure.Persistence;
using ModularMonolith.Shared.Infrastructure.EventBus;
using ModularMonolith.Shared.Infrastructure.Migrations;
using ModularMonolith.Shared.Migrations;
#endregion

namespace ModularMonolith.Modules.Payment;

public static class PaymentModule
{
    public static IServiceCollection AddPaymentModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PaymentDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<ProcessPaymentHandler>());

        services.AddValidatorsFromAssembly(typeof(ProcessPaymentRequestValidator).Assembly, includeInternalTypes: true);
      
        services.AddOutbox<PaymentDbContext>();

        services.AddSingleton<IMigratable>(new DbContextMigratable<PaymentDbContext>());

        return services;
    }

    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ProcessPaymentEndpoint.MapEndpoint(endpoints);
        return endpoints;
    }
}
