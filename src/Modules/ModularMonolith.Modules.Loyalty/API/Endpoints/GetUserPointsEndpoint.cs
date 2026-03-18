using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using ModularMonolith.Modules.Loyalty.Infrastructure.Persistence;

namespace ModularMonolith.Modules.Loyalty.API.Endpoints;

internal static class GetUserPointsEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/loyalty/points/{userId}", async (
            string userId,
            LoyaltyDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var userPoints = await dbContext.UserPoints
                .AsNoTracking()
                .FirstOrDefaultAsync(up => up.UserId == userId, cancellationToken);

            if (userPoints is null)
                return Results.NotFound(new { Detail = $"'{userId}' kullanıcısına ait puan kaydı bulunamadı." });

            return Results.Ok(new
            {
                userPoints.UserId,
                userPoints.TotalPoints,
                userPoints.AvailablePoints,
                userPoints.UsedPoints
            });
        })
        .WithName("GetUserPoints")
        .WithTags("Loyalty")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }
}
