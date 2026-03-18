using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularMonolith.Modules.Loyalty.API.Endpoints;
using ModularMonolith.Modules.Loyalty.Application.EventHandlers;
using ModularMonolith.Modules.Loyalty.Infrastructure.Persistence;
using ModularMonolith.Shared.EventBus;
using ModularMonolith.Shared.Events;
using ModularMonolith.Shared.Infrastructure.EventBus;
using ModularMonolith.Shared.Infrastructure.Migrations;
using ModularMonolith.Shared.Migrations;

namespace ModularMonolith.Modules.Loyalty;

public static class LoyaltyModule
{
    public static IServiceCollection AddLoyaltyModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<LoyaltyDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddEventHandler<PaymentSucceededEvent, PaymentSucceededEventHandler>();

        services.AddOutbox<LoyaltyDbContext>();

        services.AddSingleton<IMigratable>(new DbContextMigratable<LoyaltyDbContext>());

        return services;
    }

    public static IEndpointRouteBuilder MapLoyaltyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        GetUserPointsEndpoint.MapEndpoint(endpoints);
        return endpoints;
    }
}
