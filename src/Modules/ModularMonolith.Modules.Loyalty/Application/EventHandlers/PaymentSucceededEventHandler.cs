#region usings
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModularMonolith.Modules.Loyalty.Domain.Entities;
using ModularMonolith.Modules.Loyalty.Infrastructure.Persistence;
using ModularMonolith.Shared.EventBus;
using ModularMonolith.Shared.Events;
#endregion

namespace ModularMonolith.Modules.Loyalty.Application.EventHandlers;

internal sealed class PaymentSucceededEventHandler(
    LoyaltyDbContext dbContext,
    [FromKeyedServices(nameof(LoyaltyDbContext))] IOutboxPublisher outboxPublisher,
    ILogger<PaymentSucceededEventHandler> logger) : IEventHandler<PaymentSucceededEvent>
{
    public async Task HandleAsync(PaymentSucceededEvent @event, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Ödeme başarı eventi işleniyor. UserId: {UserId}, OrderId: {OrderId}, Amount: {Amount}",
            @event.UserId, @event.OrderId, @event.Amount);

        try
        {
            var pointsToAdd = CalculatePoints(@event.Amount);

            var userPoints = await dbContext.UserPoints
                .FirstOrDefaultAsync(up => up.UserId == @event.UserId, cancellationToken);

            if (userPoints is null)
            {
                userPoints = new UserPoints
                {
                    Id = Guid.NewGuid(),
                    UserId = @event.UserId,
                    TotalPoints = pointsToAdd,
                    AvailablePoints = pointsToAdd,
                    UsedPoints = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                dbContext.UserPoints.Add(userPoints);
                logger.LogInformation("Yeni UserPoints kaydı oluşturuldu. UserId: {UserId}", @event.UserId);
            }
            else
            {
                userPoints.TotalPoints += pointsToAdd;
                userPoints.AvailablePoints += pointsToAdd;
                userPoints.UpdatedAt = DateTime.UtcNow;
                logger.LogInformation(
                    "UserPoints güncellendi. UserId: {UserId}, Yeni Toplam: {Total}",
                    @event.UserId, userPoints.TotalPoints);
            }

            await outboxPublisher.PublishAsync(new PointsAddedEvent
            {
                UserId = @event.UserId,
                UserEmail = @event.UserEmail,
                OrderId = @event.OrderId,
                PointsAdded = pointsToAdd,
                TotalPoints = userPoints.TotalPoints,
                AvailablePoints = userPoints.AvailablePoints
            }, cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Puan güncellendi ve PointsAddedEvent outbox'a alındı. UserId: {UserId}, Eklenen: {Points} puan",
                @event.UserId, pointsToAdd);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Puan ekleme sırasında hata. UserId: {UserId}, OrderId: {OrderId}",
                @event.UserId, @event.OrderId);
            throw;
        }
    }

    private static int CalculatePoints(decimal amount) =>
        (int)Math.Round(amount * 0.10m, MidpointRounding.AwayFromZero);
}
